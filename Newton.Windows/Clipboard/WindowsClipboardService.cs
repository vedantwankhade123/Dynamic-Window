using Microsoft.Extensions.Logging;
using Newton.Core.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using ClipboardEntry = Newton.Core.Models.ClipboardHistoryItem;

namespace Newton.Windows.Clipboard;

public sealed class WindowsClipboardService : IClipboardService
{
    private readonly ILogger<WindowsClipboardService> _logger;
    private readonly List<ClipboardEntry> _items = new();
    private string _lastCaptured = string.Empty;

    public IReadOnlyList<ClipboardEntry> Items => _items.AsReadOnly();
    public event EventHandler? HistoryChanged;

    public WindowsClipboardService(ILogger<WindowsClipboardService> logger)
    {
        _logger = logger;
        try
        {
            global::Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += (_, _) => _ = CaptureCurrentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Clipboard.ContentChanged subscription failed.");
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            var result = await global::Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
            if (result.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                _items.Clear();
                foreach (var item in result.Items.Take(25))
                {
                    if (item.Content.Contains(StandardDataFormats.Text))
                    {
                        var text = await item.Content.GetTextAsync();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _items.Add(new ClipboardEntry { Text = text, Timestamp = item.Timestamp });
                        }
                    }
                }
                HistoryChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Clipboard history API unavailable; falling back to active content capture.");
        }

        await CaptureCurrentAsync();
    }

    private async Task CaptureCurrentAsync()
    {
        try
        {
            var data = global::Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (data.Contains(StandardDataFormats.Text))
            {
                var text = await data.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text) && text != _lastCaptured)
                {
                    _lastCaptured = text;
                    _items.RemoveAll(x => x.Text == text);
                    _items.Insert(0, new ClipboardEntry { Text = text, Timestamp = DateTimeOffset.Now });
                    if (_items.Count > 25) _items.RemoveAt(_items.Count - 1);
                    HistoryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch
        {
            // Clipboard may be locked transiently by another app
        }
    }

    public void CopyText(string text)
    {
        try
        {
            var pkg = new DataPackage();
            pkg.SetText(text);
            global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
            _lastCaptured = text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy text to Windows Clipboard.");
        }
    }

    public void ClearHistory()
    {
        _items.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
