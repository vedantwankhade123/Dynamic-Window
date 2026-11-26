namespace Newton.Core.Models;

public sealed class ClipboardHistoryItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Text { get; init; } = string.Empty;
    public string Preview => Text.Length > 80 ? Text[..80].Replace('\n', ' ') + "…" : Text.Replace('\n', ' ');
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public string TimeLabel => Timestamp.ToString("HH:mm");
}
