using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;

namespace Newton.App.Controls;

public sealed partial class MusicPlayerControl : UserControl
{
    private readonly DispatcherTimer _waveTimer;
    private double _phase;

    public MusicPlayerControl()
    {
        InitializeComponent();
        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _waveTimer.Tick += OnWaveTick;
        Loaded += (_, _) => _waveTimer.Start();
        Unloaded += (_, _) => _waveTimer.Stop();
    }

    private void OnWaveTick(object? sender, object e)
    {
        bool playing = DataContext is MainNewtonndViewModel vm && vm.CurrentTrack.IsPlaying;
        if (!playing)
        {
            W1.Height = W2.Height = W3.Height = W4.Height = W5.Height = W6.Height = W7.Height = W8.Height = W9.Height = 3.5;
            return;
        }

        _phase += 0.42;
        W1.Height = 4 + 7 * (0.5 + 0.5 * Math.Sin(_phase));
        W2.Height = 6 + 10 * (0.5 + 0.5 * Math.Sin(_phase + 0.6));
        W3.Height = 8 + 13 * (0.5 + 0.5 * Math.Sin(_phase + 1.2));
        W4.Height = 10 + 14 * (0.5 + 0.5 * Math.Sin(_phase + 1.8));
        W5.Height = 11 + 15 * (0.5 + 0.5 * Math.Sin(_phase + 2.4));
        W6.Height = 10 + 13 * (0.5 + 0.5 * Math.Sin(_phase + 3.0));
        W7.Height = 8 + 11 * (0.5 + 0.5 * Math.Sin(_phase + 3.6));
        W8.Height = 6 + 9 * (0.5 + 0.5 * Math.Sin(_phase + 4.2));
        W9.Height = 4 + 6 * (0.5 + 0.5 * Math.Sin(_phase + 4.8));
    }
}
