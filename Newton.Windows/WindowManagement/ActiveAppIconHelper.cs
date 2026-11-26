using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Newton.Windows.WindowManagement;

public sealed class ActiveAppInfo
{
    public string ProcessName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? IconPath { get; init; }
}

public static class ActiveAppIconHelper
{
    private static readonly Dictionary<string, string?> IconCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<ActiveAppInfo?> GetForegroundAppAsync()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0 || pid == Environment.ProcessId) return null;

            using var proc = Process.GetProcessById((int)pid);
            var exePath = proc.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exePath)) return null;

            var name = proc.ProcessName;
            var iconPath = await GetOrExtractAppIconAsync(name, exePath);
            return new ActiveAppInfo
            {
                ProcessName = name,
                DisplayName = FormatFriendlyName(name),
                IconPath = iconPath
            };
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> GetIconForProcessNameAsync(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return null;
        if (IconCache.TryGetValue(processName, out var cached)) return cached;

        try
        {
            var procs = Process.GetProcessesByName(processName);
            foreach (var p in procs)
            {
                try
                {
                    var exePath = p.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(exePath))
                    {
                        return await GetOrExtractAppIconAsync(processName, exePath);
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        catch { }

        return null;
    }

    private static async Task<string?> GetOrExtractAppIconAsync(string keyName, string exePath)
    {
        if (IconCache.TryGetValue(keyName, out var cached) && cached is not null && File.Exists(cached))
        {
            return cached;
        }

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton", "Cache", "AppIcons");
            Directory.CreateDirectory(dir);
            var outPath = Path.Combine(dir, $"{keyName.ToLowerInvariant()}.png");

            if (!File.Exists(outPath))
            {
                var file = await StorageFile.GetFileFromPathAsync(exePath);
                using var thumb = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 64);
                if (thumb is null || thumb.Size == 0) return null;

                using var readStream = thumb.AsStreamForRead();
                using var fs = File.Create(outPath);
                await readStream.CopyToAsync(fs);
            }

            IconCache[keyName] = outPath;
            return outPath;
        }
        catch
        {
            IconCache[keyName] = null;
            return null;
        }
    }

    private static string FormatFriendlyName(string name) => name.ToLowerInvariant() switch
    {
        "chrome" => "Chrome",
        "msedge" => "Edge",
        "brave" => "Brave",
        "spotify" => "Spotify",
        "vlc" => "VLC",
        "code" => "VS Code",
        "explorer" => "Explorer",
        _ => char.ToUpper(name[0]) + name[1..]
    };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
