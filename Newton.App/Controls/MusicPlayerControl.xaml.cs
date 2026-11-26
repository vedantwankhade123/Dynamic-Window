using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;

namespace Newton.App.Controls;

public sealed partial class MusicPlayerControl : UserControl
{
    private readonly DispatcherTimer _waveTimer;
    private readonly Random _rng = new();

    public MusicPlayerControl()
    {
        InitializeComponent();
        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        _waveTimer.Tick += OnWaveTick;
        Loaded += (_, _) => _waveTimer.Start();
        Unloaded += (_, _) => _waveTimer.Stop();
    }

    private void OnWaveTick(object? sender, object e)
    {
        if (DataContext is MainNewtonndViewModel vm && vm.CurrentTrack.IsPlaying)
        {
            W1.Height = _rng.Next(6, 15);
            W2.Height = _rng.Next(9, 19);
            W3.Height = _rng.Next(6, 17);
            W4.Height = _rng.Next(10, 20);
            W5.Height = _rng.Next(5, 14);
        }
    }
}
