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

        var c = vm.MediaAccentColor;
        if (vm.CurrentTrack?.IsPlaying == true)
        {
            _phase += 0.1;
            byte a = (byte)(110 + 60 * Math.Sin(_phase));
            CompactGlowStop.Color = Color.FromArgb(a, c.R, c.G, c.B);
        }
        else
        {
            CompactGlowStop.Color = Color.FromArgb(50, c.R, c.G, c.B);
        }
    }
}
