using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Microsoft.Extensions.Logging;

namespace Newton.Infrastructure.AI;

public sealed class LocalAiService : IAiService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<LocalAiService> _logger;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly List<AiMessage> _messages = new();
    private readonly List<AiModelOption> _models = new();

    public IReadOnlyList<AiModelOption> AvailableModels => _models.AsReadOnly();
    public AiModelOption SelectedModel { get; set; }
    public IReadOnlyList<AiMessage> Messages => _messages.AsReadOnly();
    public event EventHandler? MessagesUpdated;

    public LocalAiService(ISettingsService settingsService, ILogger<LocalAiService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        var endpoint = _settingsService.Current.LocalEndpoint;
        var defaultModel = _settingsService.Current.DefaultLocalModel;
        SelectedModel = new AiModelOption(defaultModel, defaultModel, "Ollama (Local)", true, endpoint);
        _models.Add(SelectedModel);

        _ = RefreshModelsAsync();
    }

    public async Task RefreshModelsAsync()
    {
        var endpoint = _settingsService.Current.LocalEndpoint.TrimEnd('/');
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var json = await _http.GetFromJsonAsync<JsonElement>($"{endpoint}/api/tags", cts.Token);
            if (json.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                _models.Clear();
                foreach (var m in arr.EnumerateArray())
                {
                    var name = m.GetProperty("name").GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        _models.Add(new AiModelOption(name, name, "Ollama (Local)", true, endpoint));
                    }
                }
                if (_models.Count > 0) SelectedModel = _models[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama not reachable at {Endpoint}.", endpoint);
        }
    }

    public async Task SendMessageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;

        _messages.Add(new AiMessage
        {
            Role = AiMessageRole.User,
            Content = prompt.Trim(),
            ModelName = SelectedModel.DisplayName,
            ProviderName = SelectedModel.ProviderName,
            IsLocal = SelectedModel.IsLocal
        });
        MessagesUpdated?.Invoke(this, EventArgs.Empty);

        var reply = new AiMessage
        {
            Role = AiMessageRole.Assistant,
            Content = string.Empty,
            ModelName = SelectedModel.DisplayName,
            ProviderName = SelectedModel.ProviderName,
            IsLocal = SelectedModel.IsLocal,
            IsStreaming = true
        };
        _messages.Add(reply);
        MessagesUpdated?.Invoke(this, EventArgs.Empty);

        try
        {
            var endpoint = _settingsService.Current.LocalEndpoint.TrimEnd('/');
            var payload = new
            {
                model = SelectedModel.Id,
                stream = true,
                messages = new[] { new { role = "user", content = prompt.Trim() } }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/api/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var chunk))
                {
                    reply.Content += chunk.GetString();
                    MessagesUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            reply.Content = $"Unable to reach Ollama at {_settingsService.Current.LocalEndpoint} ({ex.Message}). Start Ollama or update the endpoint in Settings → AI.";
        }
        finally
        {
            reply.IsStreaming = false;
            MessagesUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearHistory()
    {
        _messages.Clear();
        MessagesUpdated?.Invoke(this, EventArgs.Empty);
    }
}
