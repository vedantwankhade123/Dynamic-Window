using Microsoft.UI.Dispatching;
using System.Collections.Concurrent;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Newton.Windows.Notifications;

/// <summary>
/// Optional notification-listener add-on. Create it only from a user-initiated
/// action because creation displays the Windows notification-access prompt.
/// It has no dependency on Newton views or window handles.
/// </summary>
public sealed class UniversalReplyService : IAsyncDisposable
{
    private readonly UserNotificationListener _listener;
    private readonly IReadOnlyList<IChatReplyProvider> _replyProviders;
    private readonly Func<string, string, bool> _isChatSource;
    private readonly ConcurrentDictionary<uint, ChatNotification> _known = new();
    private bool _disposed;

    private UniversalReplyService(
        UserNotificationListener listener,
        IEnumerable<IChatReplyProvider>? replyProviders,
        Func<string, string, bool>? isChatSource)
    {
        _listener = listener;
        _replyProviders = (replyProviders ?? []).ToArray();
        _isChatSource = isChatSource ?? IsKnownChatSource;
    }

    public event EventHandler<ChatNotification>? OnChatMessageReceived;
    public event EventHandler<ChatNotification>? OnChatMessageRemoved;
    public event EventHandler<ChatReplyResult>? OnReplySent;

    /// <summary>
    /// Call from an explicit user gesture. Access request is marshalled to the
    /// supplied UI dispatcher as required by UserNotificationListener.
    /// </summary>
    public static async Task<UniversalReplyService?> CreateAsync(
        DispatcherQueue uiDispatcher,
        IEnumerable<IChatReplyProvider>? replyProviders = null,
        Func<string, string, bool>? isChatSource = null,
        Action<UniversalReplyService>? onCreated = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        var listener = UserNotificationListener.Current;
        var access = await OnUiThreadAsync(uiDispatcher, () => listener.RequestAccessAsync().AsTask(), cancellationToken);
        if (access != UserNotificationListenerAccessStatus.Allowed) return null;

        var service = new UniversalReplyService(listener, replyProviders, isChatSource);
        onCreated?.Invoke(service);
        service._listener.NotificationChanged += service.OnNotificationChanged;
        await service.LoadExistingAsync(cancellationToken);
        return service;
    }

    /// <summary>Attempts a reply through an explicitly registered app-specific provider.</summary>
    public async Task<ChatReplyResult> SendReplyAsync(
        ChatNotification notification,
        string replyText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (string.IsNullOrWhiteSpace(replyText))
            return new ChatReplyResult(notification, false, "Reply text is empty.");

        foreach (var provider in _replyProviders)
        {
            if (!provider.CanReplyTo(notification)) continue;
            try
            {
                var result = await provider.SendReplyAsync(notification, replyText, cancellationToken);
                OnReplySent?.Invoke(this, result);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                var failed = new ChatReplyResult(notification, false, ex.Message);
                OnReplySent?.Invoke(this, failed);
                return failed;
            }
        }

        var unsupported = new ChatReplyResult(notification, false,
            "This app does not expose a supported reply provider. Windows notification listening cannot invoke another app's reply action.");
        OnReplySent?.Invoke(this, unsupported);
        return unsupported;
    }

    private async Task LoadExistingAsync(CancellationToken cancellationToken)
    {
        if (_listener.GetAccessStatus() != UserNotificationListenerAccessStatus.Allowed) return;
        var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast).AsTask(cancellationToken);
        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PublishAsync(notification);
        }
    }

    private async void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        try
        {
            if (args.ChangeKind == UserNotificationChangedKind.Added)
            {
                var notification = sender.GetNotification(args.UserNotificationId);
                if (notification is not null) await PublishAsync(notification);
            }
            else if (_known.TryRemove(args.UserNotificationId, out var removed))
            {
                OnChatMessageRemoved?.Invoke(this, removed);
            }
        }
        catch
        {
            // A malformed or inaccessible notification must not stop the listener.
        }
    }

    private async Task PublishAsync(UserNotification notification)
    {
        try
        {
            var binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding is null) return;

            var parts = binding.GetTextElements()
                .Select(element => element.Text?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
            if (parts.Length == 0) return;

            var app = notification.AppInfo;
            var appName = app.DisplayInfo.DisplayName;
            if (!_isChatSource(app.Id, appName)) return;
            var item = new ChatNotification(
                notification.Id,
                app.Id,
                appName,
                parts[0]!,
                string.Join(Environment.NewLine, parts.Skip(1)),
                notification.CreationTime,
                await ReadAppIconAsync(app));

            _known[notification.Id] = item;
            OnChatMessageReceived?.Invoke(this, item);
        }
        catch
        {
            // Notification payloads are app-defined; skip only the item we cannot parse.
        }
    }

    private static async Task<byte[]?> ReadAppIconAsync(global::Windows.ApplicationModel.AppInfo app)
    {
        try
        {
            RandomAccessStreamReference? logo = app.DisplayInfo.GetLogo(new Size(32, 32));
            if (logo is null) return null;
            using var stream = await logo.OpenReadAsync();
            using var managed = stream.AsStreamForRead();
            using var buffer = new MemoryStream();
            await managed.CopyToAsync(buffer);
            return buffer.ToArray();
        }
        catch { return null; }
    }

    private static bool IsKnownChatSource(string appId, string appName)
    {
        var source = $"{appId} {appName}";
        return new[] { "whatsapp", "telegram", "discord", "slack", "teams", "skype", "zoom", "google meet", "signal" }
            .Any(name => source.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static Task<T> OnUiThreadAsync<T>(DispatcherQueue dispatcher, Func<Task<T>> action, CancellationToken token)
    {
        if (dispatcher.HasThreadAccess) return action();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(async () =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                completion.TrySetResult(await action());
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        }))
        {
            completion.TrySetException(new InvalidOperationException("The UI dispatcher is shutting down."));
        }
        return completion.Task;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _listener.NotificationChanged -= OnNotificationChanged;
            _known.Clear();
        }
        return ValueTask.CompletedTask;
    }
}

public sealed record ChatNotification(
    uint NotificationId,
    string AppId,
    string AppName,
    string Title,
    string Body,
    DateTimeOffset ReceivedAt,
    byte[]? AppIconBytes);

public sealed record ChatReplyResult(ChatNotification Notification, bool Succeeded, string? Error);

/// <summary>Optional, explicit per-app reply integration; Windows has no universal third-party invoke API.</summary>
public interface IChatReplyProvider
{
    bool CanReplyTo(ChatNotification notification);
    Task<ChatReplyResult> SendReplyAsync(ChatNotification notification, string replyText, CancellationToken cancellationToken);
}
