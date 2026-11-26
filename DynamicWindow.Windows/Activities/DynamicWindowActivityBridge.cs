using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicWindow.Core.Models;
using DynamicWindow.Core.Services;

namespace DynamicWindow.Windows.Activities;

/// <summary>
/// Per-user named-pipe bridge for apps that publish long-running work to DynamicWindow.
/// Messages are UTF-8 JSON lines. Permission-sensitive actions are returned to the
/// originating client only after the user selects them in the island.
/// </summary>
public sealed class DynamicWindowActivityBridge : IDisposable
{
    public const string PipeName = "DynamicWindow.Island.Activity.v1";

    private readonly ActivityManager _activityManager;
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<string, ActivityOwner> _activityOwners = new(StringComparer.Ordinal);
    private Task? _listenerTask;

    public DynamicWindowActivityBridge(ActivityManager activityManager) => _activityManager = activityManager;

    public void Start()
    {
        if (_listenerTask is not null) return;
        _activityManager.ActionRequested += OnActionRequested;
        _listenerTask = ListenAsync(_stop.Token);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    8,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken);
                _ = HandleClientAsync(pipe, cancellationToken);
                pipe = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                pipe?.Dispose();
                break;
            }
            catch
            {
                pipe?.Dispose();
                try { await Task.Delay(500, cancellationToken); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        var ownerId = Guid.NewGuid().ToString("N");
        var connection = new ClientConnection(pipe);
        var ownedActivityIds = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            using (pipe)
            using (var reader = new StreamReader(pipe, leaveOpen: true))
            {
                await connection.SendAsync(new { operation = "ready", protocol = 1 }, cancellationToken);

                while (pipe.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null) break;
                    if (line.Length > 32_768) continue;

                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    var operation = ReadString(root, "operation");
                    var clientActivityId = ReadString(root, "activityId");
                    if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(clientActivityId)) continue;

                    var id = $"external:{ownerId}:{clientActivityId}";
                    if (operation.Equals("remove", StringComparison.OrdinalIgnoreCase))
                    {
                        _activityOwners.TryRemove(id, out _);
                        _activityManager.RemoveActivity(id);
                        ownedActivityIds.Remove(id);
                        continue;
                    }

                    if (!operation.Equals("upsert", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!TryCreateActivity(root, id, out var activity)) continue;

                    ownedActivityIds.Add(id);
                    _activityOwners[id] = new ActivityOwner(connection, clientActivityId);
                    _activityManager.AddOrUpdateActivity(activity);

                    if (activity.Status == ActivityStatus.Completed)
                    {
                        _ = ExpireCompletedActivityAsync(id, cancellationToken);
                    }
                }
            }
        }
        catch
        {
            // A disconnected or malformed local client must not stop the bridge.
        }
        finally
        {
            foreach (var id in ownedActivityIds)
            {
                _activityOwners.TryRemove(id, out _);
                _activityManager.RemoveActivity(id);
            }
        }
    }

    private bool TryCreateActivity(JsonElement root, string id, out ActivityItem activity)
    {
        activity = null!;
        var title = ReadString(root, "title");
        if (string.IsNullOrWhiteSpace(title)) return false;

        var source = ReadString(root, "source") ?? "Background task";
        var subtitle = ReadString(root, "subtitle") ?? "Working";
        var type = ActivityType.CustomDeveloper;
        if (Enum.TryParse(ReadString(root, "type"), ignoreCase: true, out ActivityType parsedType) &&
            parsedType is ActivityType.CustomDeveloper or ActivityType.AiGeneration or ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall or ActivityType.Notification)
        {
            type = parsedType;
        }

        var status = ActivityStatus.Active;
        if (Enum.TryParse(ReadString(root, "status"), ignoreCase: true, out ActivityStatus parsedStatus) &&
            parsedStatus is ActivityStatus.Active or ActivityStatus.Paused or ActivityStatus.Completed or ActivityStatus.Failed)
        {
            status = parsedStatus;
        }

        double? progress = null;
        if (root.TryGetProperty("progress", out var progressElement) && progressElement.TryGetDouble(out var rawProgress))
        {
            progress = Math.Clamp(rawProgress, 0, 100);
        }

        var accent = ReadString(root, "accentHex");
        if (accent is null || !Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}$")) accent = "#0A84FF";

        var actions = new List<ActivityAction>();
        if (root.TryGetProperty("actions", out var actionArray) && actionArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var action in actionArray.EnumerateArray().Take(2))
            {
                var actionId = ReadString(action, "id");
                var label = ReadString(action, "label");
                if (!string.IsNullOrWhiteSpace(actionId) && !string.IsNullOrWhiteSpace(label))
                {
                    actions.Add(new ActivityAction(actionId, label, IsPrimary: actions.Count == 0));
                }
            }
        }

        activity = new ActivityItem
        {
            Id = id,
            Type = type,
            Title = title,
            Subtitle = subtitle,
            AccentHex = accent,
            Progress = progress,
            IsIndeterminate = progress is null,
            Status = status,
            Priority = ActivityPriority.High,
            IsSimulated = false,
            Metadata = { ["SourceApp"] = source },
        };
        activity.Actions.AddRange(actions);
        return true;
    }

    private async Task ExpireCompletedActivityAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);
            var current = _activityManager.Activities.FirstOrDefault(item => item.Id == id);
            if (current?.Status == ActivityStatus.Completed)
            {
                _activityOwners.TryRemove(id, out _);
                _activityManager.RemoveActivity(id);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnActionRequested(object? sender, (string ActivityId, string ActionId) request)
    {
        if (_activityOwners.TryGetValue(request.ActivityId, out var owner))
        {
            _ = owner.Connection.SendAsync(new
            {
                operation = "action",
                activityId = owner.ClientActivityId,
                actionId = request.ActionId
            }, _stop.Token);
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public void Dispose()
    {
        _activityManager.ActionRequested -= OnActionRequested;
        _stop.Cancel();
        _stop.Dispose();
    }

    private sealed record ActivityOwner(ClientConnection Connection, string ClientActivityId);

    private sealed class ClientConnection(NamedPipeServerStream pipe)
    {
        private readonly SemaphoreSlim _writeGate = new(1, 1);

        public async Task SendAsync(object message, CancellationToken cancellationToken)
        {
            if (!pipe.IsConnected) return;
            await _writeGate.WaitAsync(cancellationToken);
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
                await pipe.WriteAsync(bytes, cancellationToken);
                await pipe.WriteAsync(new byte[] { (byte)'\n' }, cancellationToken);
                await pipe.FlushAsync(cancellationToken);
            }
            catch { }
            finally { _writeGate.Release(); }
        }
    }
}
