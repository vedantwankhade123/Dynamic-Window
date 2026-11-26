namespace DynamicWindow.Core.Models;

public enum ClipboardItemKind
{
    Text,
    Image,
    File
}

public sealed class ClipboardHistoryItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public ClipboardItemKind Kind { get; init; } = ClipboardItemKind.Text;
    public string Text { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public List<string> FilePaths { get; init; } = new();
    public bool IsPinned { get; set; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string TimeLabel => Timestamp.ToString("HH:mm");
    public bool IsImage => Kind == ClipboardItemKind.Image && !string.IsNullOrEmpty(ImagePath);
    public bool IsFile => Kind == ClipboardItemKind.File;
    public bool IsText => Kind == ClipboardItemKind.Text;

    public string Preview => Kind switch
    {
        ClipboardItemKind.Image => "Screenshot / Image",
        ClipboardItemKind.File => FilePaths.Count == 1
            ? Path.GetFileName(FilePaths[0])
            : $"{FilePaths.Count} files",
        _ => Text.Length > 72 ? Text[..72].Replace('\n', ' ').Replace('\r', ' ') + "…" : Text.Replace('\n', ' ').Replace('\r', ' ')
    };
}
