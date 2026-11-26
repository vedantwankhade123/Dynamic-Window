using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;
using Windows.Foundation;
using Windows.UI;

namespace Newton.App.Controls;

public sealed partial class MusicPlayerControl : UserControl
{
    private readonly DispatcherTimer _gradTimer;
    private double _phase;

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

        var c = vm.MediaAccentColor;
        if (vm.CurrentTrack?.IsPlaying == true)
        {
            _phase += 0.08;
            byte alpha = (byte)(130 + 65 * Math.Sin(_phase));
            GlowStop.Color = Color.FromArgb(alpha, c.R, c.G, c.B);
            double x = 0.5 + 0.25 * Math.Sin(_phase * 0.7);
            AmbientBrush.EndPoint = new Point(x, 1.0);
        }
        else
        {
            GlowStop.Color = Color.FromArgb(60, c.R, c.G, c.B);
            AmbientBrush.EndPoint = new Point(0.5, 1.0);
        }
    }
}
