using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;

namespace Newton.App.Controls;

public sealed partial class CompactNewtonndControl : UserControl
{
    private readonly DispatcherTimer _waveTimer;
    private double _phase;

    public CompactNewtonndControl()
    {
        InitializeComponent();
        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(85) };
        _waveTimer.Tick += OnWaveTick;
        Loaded += (_, _) => _waveTimer.Start();
        Unloaded += (_, _) => _waveTimer.Stop();
    }

    private void OnWaveTick(object? sender, object e)
    {
        if (WaveformBars.Visibility != Visibility.Visible) return;

        bool playing = DataContext is MainNewtonndViewModel vm && vm.CurrentTrack.IsPlaying;
        if (!playing)
        {
            Bar1.Height = Bar2.Height = Bar3.Height = Bar4.Height = Bar5.Height = Bar6.Height = 3.5;
            return;
        }

        _phase += 0.45;
        Bar1.Height = 5 + 6 * (0.5 + 0.5 * Math.Sin(_phase));
        Bar2.Height = 7 + 9 * (0.5 + 0.5 * Math.Sin(_phase + 0.9));
        Bar3.Height = 8 + 10 * (0.5 + 0.5 * Math.Sin(_phase + 1.8));
        Bar4.Height = 7 + 9 * (0.5 + 0.5 * Math.Sin(_phase + 2.7));
        Bar5.Height = 6 + 8 * (0.5 + 0.5 * Math.Sin(_phase + 3.6));
        Bar6.Height = 4 + 6 * (0.5 + 0.5 * Math.Sin(_phase + 4.5));
    }
}
