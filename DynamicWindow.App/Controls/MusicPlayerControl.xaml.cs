using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DynamicWindow.App.ViewModels;

namespace DynamicWindow.App.Controls;

public sealed partial class MusicPlayerControl : UserControl
{
    private readonly DispatcherTimer _scrubberTimer;

    public MusicPlayerControl()
    {
        InitializeComponent();
        _scrubberTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _scrubberTimer.Tick += (_, _) => UpdateScrubber();
        Loaded += (_, _) => _scrubberTimer.Start();
        Unloaded += (_, _) => _scrubberTimer.Stop();
        ScrubberTrack.SizeChanged += (_, _) => UpdateScrubber();
    }

    private void UpdateScrubber()
    {
        if (DataContext is not MainDynamicWindowViewModel vm) return;

        double totalW = ScrubberTrack.ActualWidth;
        if (totalW <= 0) return;

        double pct = Math.Clamp(vm.CompactProgress / 100.0, 0.0, 1.0);
        double fillW = Math.Max(0, totalW * pct);
        ScrubberFill.Width = fillW;
        double thumbLeft = Math.Clamp(fillW - 5, 0, Math.Max(0, totalW - 10));
        ScrubberThumb.Margin = new Thickness(thumbLeft, 0, 0, 0);
    }
}
