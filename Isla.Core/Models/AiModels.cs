namespace Isla.Core.Models;

public enum AiMessageRole
{
    User,
    Assistant,
    System
}

public sealed class AiMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public AiMessageRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public string ModelName { get; init; } = "llama3.2:latest";
    public string ProviderName { get; init; } = "Ollama (Local)";
    public bool IsLocal { get; init; } = true;
    public bool IsStreaming { get; set; }
}

public sealed record AiModelOption(
    string Id,
    string DisplayName,
    string ProviderName,
    bool IsLocal,
    string Endpoint);
