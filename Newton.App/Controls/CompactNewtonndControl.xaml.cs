using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;
using Windows.UI;

namespace Newton.App.Controls;

public sealed partial class CompactNewtonndControl : UserControl
{
    private readonly DispatcherTimer _gradTimer;
    private double _phase;

    public CompactNewtonndControl()
    {
        InitializeComponent();
        _gradTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(75) };
        _gradTimer.Tick += OnGradTick;
        Loaded += (_, _) => _gradTimer.Start();
        Unloaded += (_, _) => _gradTimer.Stop();
    }

    private void OnGradTick(object? sender, object e)
    {
        if (DataContext is not MainNewtonndViewModel vm || !vm.IsMusicPrimary) return;

        if (vm.CurrentTrack?.IsPlaying == true)
        {
            _phase += 0.1;
            byte a = (byte)(95 + 55 * Math.Sin(_phase));
            byte r = (byte)(140 + 50 * Math.Sin(_phase * 0.7));
            byte g = (byte)(50 + 30 * Math.Cos(_phase * 0.5));
            byte b = (byte)(110 + 70 * Math.Cos(_phase * 0.8));
            CompactGlowStop.Color = Color.FromArgb(a, r, g, b);
        }
        else
        {
            CompactGlowStop.Color = Color.FromArgb(40, 120, 40, 60);
        }
    }
}
