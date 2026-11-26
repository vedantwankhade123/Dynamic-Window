namespace Newton.Core.Models;

public sealed record NotificationAction(string ActionId, string Label, bool IsPrimary = false);

public sealed class NotificationItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string AppName { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "\uEA8F";
    public string AccentHex { get; init; } = "#60A5FA";
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public bool IsRead { get; set; }
    public string Category { get; init; } = "General";
    public List<NotificationAction> Actions { get; init; } = new();
    public bool IsSimulated { get; init; } = true;
}
