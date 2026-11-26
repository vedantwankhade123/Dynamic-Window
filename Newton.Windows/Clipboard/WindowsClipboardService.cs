using Microsoft.Extensions.Logging;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using ClipboardEntry = Newton.Core.Models.ClipboardHistoryItem;

namespace Newton.Windows.Clipboard;

public sealed class WindowsClipboardService : IClipboardService
{
    private readonly ILogger<WindowsClipboardService> _logger;
    private readonly List<ClipboardEntry> _items = new();
    private string _lastCapturedSignature = string.Empty;

    public IReadOnlyList<ClipboardEntry> Items => _items.AsReadOnly();
    public event EventHandler? HistoryChanged;
    public event EventHandler<ClipboardEntry>? ItemCaptured;

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
                var pinned = _items.Where(i => i.IsPinned).ToList();
                _items.Clear();
                _items.AddRange(pinned);

                foreach (var item in result.Items.Take(25))
                {
                    if (item.Content.Contains(StandardDataFormats.Text))
                    {
                        var text = await item.Content.GetTextAsync();
                        if (!string.IsNullOrWhiteSpace(text) && _items.All(x => x.Text != text))
                        {
                            _items.Add(new ClipboardEntry { Kind = ClipboardItemKind.Text, Text = text, Timestamp = item.Timestamp });
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
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                var bmpRef = await data.GetBitmapAsync();
                var imgPath = await SaveClipboardBitmapAsync(bmpRef);
                if (!string.IsNullOrEmpty(imgPath))
                {
                    var sig = $"img:{new FileInfo(imgPath).Length}";
                    if (sig == _lastCapturedSignature) return;
                    _lastCapturedSignature = sig;

                    var entry = new ClipboardEntry
                    {
                        Kind = ClipboardItemKind.Image,
                        ImagePath = imgPath,
                        Text = "Screenshot",
                        Timestamp = DateTimeOffset.Now
                    };
                    InsertEntry(entry, notify: true);
                    return;
                }
            }

            if (data.Contains(StandardDataFormats.StorageItems))
            {
                var storage = await data.GetStorageItemsAsync();
                var paths = storage.Select(s => s.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                if (paths.Count > 0)
                {
                    var sig = $"files:{string.Join("|", paths)}";
                    if (sig == _lastCapturedSignature) return;
                    _lastCapturedSignature = sig;

                    var entry = new ClipboardEntry
                    {
                        Kind = ClipboardItemKind.File,
                        FilePaths = paths,
                        Text = string.Join(", ", paths.Select(Path.GetFileName)),
                        Timestamp = DateTimeOffset.Now
                    };
                    InsertEntry(entry, notify: true);
                    return;
                }
            }

            if (data.Contains(StandardDataFormats.Text))
            {
                var text = await data.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var sig = $"txt:{text}";
                    if (sig == _lastCapturedSignature) return;
                    _lastCapturedSignature = sig;

                    _items.RemoveAll(x => !x.IsPinned && x.Kind == ClipboardItemKind.Text && x.Text == text);
                    var entry = new ClipboardEntry { Kind = ClipboardItemKind.Text, Text = text, Timestamp = DateTimeOffset.Now };
                    InsertEntry(entry, notify: true);
                }
            }
        }
        catch
        {
            // Clipboard may be locked transiently by another app
        }
    }

    private void InsertEntry(ClipboardEntry entry, bool notify)
    {
        _items.Insert(0, entry);
        TrimHistory();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        if (notify)
        {
            ItemCaptured?.Invoke(this, entry);
        }
    }

    private void TrimHistory()
    {
        while (_items.Count > 30)
        {
            var lastUnpinned = _items.FindLastIndex(i => !i.IsPinned);
            if (lastUnpinned < 0) break;
            _items.RemoveAt(lastUnpinned);
        }
    }

    private static async Task<string?> SaveClipboardBitmapAsync(RandomAccessStreamReference? bmpRef)
    {
        if (bmpRef is null) return null;
        try
        {
            using var stream = await bmpRef.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton", "Cache", "Clips");
            Directory.CreateDirectory(dir);
            var outPath = Path.Combine(dir, $"clip_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");

            var storageDir = await StorageFolder.GetFolderFromPathAsync(dir);
            var outFile = await storageDir.CreateFileAsync(Path.GetFileName(outPath), CreationCollisionOption.ReplaceExisting);
            using var outStream = await outFile.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
            var pixels = await decoder.GetPixelDataAsync();
            encoder.SetPixelData(
                decoder.BitmapPixelFormat,
                BitmapAlphaMode.Premultiplied,
                decoder.PixelWidth,
                decoder.PixelHeight,
                decoder.DpiX,
                decoder.DpiY,
                pixels.DetachPixelData());
            await encoder.FlushAsync();
            return outPath;
        }
        catch
        {
            return null;
        }
    }

    public void CopyText(string text)
    {
        try
        {
            var pkg = new DataPackage();
            pkg.SetText(text);
            _lastCapturedSignature = $"txt:{text}";
            global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy text to Windows Clipboard.");
        }
    }

    public async Task CopyItemAsync(ClipboardEntry item)
    {
        try
        {
            var pkg = new DataPackage();
            switch (item.Kind)
            {
                case ClipboardItemKind.Image when !string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath):
                    var imgFile = await StorageFile.GetFileFromPathAsync(item.ImagePath);
                    pkg.SetBitmap(RandomAccessStreamReference.CreateFromFile(imgFile));
                    _lastCapturedSignature = $"img:{new FileInfo(item.ImagePath).Length}";
                    break;

                case ClipboardItemKind.File when item.FilePaths.Count > 0:
                    var files = new List<IStorageItem>();
                    foreach (var p in item.FilePaths.Where(File.Exists))
                    {
                        files.Add(await StorageFile.GetFileFromPathAsync(p));
                    }
                    if (files.Count > 0)
                    {
                        pkg.SetStorageItems(files);
                        _lastCapturedSignature = $"files:{string.Join("|", item.FilePaths)}";
                    }
                    break;

                default:
                    pkg.SetText(item.Text);
                    _lastCapturedSignature = $"txt:{item.Text}";
                    break;
            }
            global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy item back to Windows Clipboard.");
        }
    }

    public void AddDroppedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var entry = new ClipboardEntry { Kind = ClipboardItemKind.Text, Text = text, IsPinned = true, Timestamp = DateTimeOffset.Now };
        InsertEntry(entry, notify: true);
    }

    public void AddDroppedFiles(IReadOnlyList<string> filePaths)
    {
        if (filePaths.Count == 0) return;
        var entry = new ClipboardEntry
        {
            Kind = ClipboardItemKind.File,
            FilePaths = filePaths.ToList(),
            Text = string.Join(", ", filePaths.Select(Path.GetFileName)),
            IsPinned = true,
            Timestamp = DateTimeOffset.Now
        };
        InsertEntry(entry, notify: true);
    }

    public void TogglePin(string id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is not null)
        {
            item.IsPinned = !item.IsPinned;
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RemoveItem(string id)
    {
        _items.RemoveAll(i => i.Id == id);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearHistory()
    {
        _items.RemoveAll(i => !i.IsPinned);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
