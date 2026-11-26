using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;

namespace Newton.App.Controls;

public sealed partial class MusicPlayerControl : UserControl
{
    private readonly DispatcherTimer _gradTimer;
    private double _phase;
    private string? _lastArtPath;
    private Color _accentBase = Color.FromArgb(255, 136, 36, 42);

    public MusicPlayerControl()
    {
        InitializeComponent();
        _gradTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        _gradTimer.Tick += OnGradientTick;
        Loaded += (_, _) => _gradTimer.Start();
        Unloaded += (_, _) => _gradTimer.Stop();
    }

    private void OnGradientTick(object? sender, object e)
    {
        if (DataContext is not MainNewtonndViewModel vm) return;

        var artPath = vm.CurrentTrack?.ArtworkPath;
        if (artPath != _lastArtPath)
        {
            _lastArtPath = artPath;
            _ = ExtractAlbumAccentAsync(artPath);
        }

        if (vm.CurrentTrack?.IsPlaying == true)
        {
            _phase += 0.08;
            byte alpha = (byte)(125 + 65 * Math.Sin(_phase));
            GlowStop.Color = Color.FromArgb(alpha, _accentBase.R, _accentBase.G, _accentBase.B);
            double x = 0.5 + 0.25 * Math.Sin(_phase * 0.7);
            AmbientBrush.EndPoint = new Point(x, 1.0);
        }
        else
        {
            GlowStop.Color = Color.FromArgb(55, _accentBase.R, _accentBase.G, _accentBase.B);
            AmbientBrush.EndPoint = new Point(0.5, 1.0);
        }
    }

    private async Task ExtractAlbumAccentAsync(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            _accentBase = Color.FromArgb(255, 136, 36, 42);
            return;
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var transform = new BitmapTransform { ScaledWidth = 8, ScaledHeight = 8 };
            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);

            var bytes = pixels.DetachPixelData();
            long rSum = 0, gSum = 0, bSum = 0, count = 0;
            for (int i = 0; i + 3 < bytes.Length; i += 4)
            {
                byte b = bytes[i], g = bytes[i + 1], r = bytes[i + 2];
                if (r + g + b < 60) continue; // skip near-black pixels
                rSum += r; gSum += g; bSum += b; count++;
            }

            if (count > 0)
            {
                byte avgR = (byte)Math.Clamp(rSum / count, 40, 220);
                byte avgG = (byte)Math.Clamp(gSum / count, 30, 180);
                byte avgB = (byte)Math.Clamp(bSum / count, 40, 220);
                _accentBase = Color.FromArgb(255, avgR, avgG, avgB);
            }
        }
        catch
        {
            _accentBase = Color.FromArgb(255, 136, 36, 42);
        }
    }
}
