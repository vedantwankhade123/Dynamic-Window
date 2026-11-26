using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Newton.App.Controls;

public sealed partial class CompactNewtonndControl : UserControl
{
    private readonly DispatcherTimer _waveTimer;
    private readonly Random _rng = new();

    public CompactNewtonndControl()
    {
        InitializeComponent();
        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
        _waveTimer.Tick += OnWaveTick;
        Loaded += (_, _) => _waveTimer.Start();
        Unloaded += (_, _) => _waveTimer.Stop();
    }

    private void OnWaveTick(object? sender, object e)
    {
        if (WaveformBars.Visibility != Visibility.Visible) return;
        Bar1.Height = _rng.Next(5, 14);
        Bar2.Height = _rng.Next(8, 17);
        Bar3.Height = _rng.Next(5, 15);
        Bar4.Height = _rng.Next(4, 12);
    }
}
