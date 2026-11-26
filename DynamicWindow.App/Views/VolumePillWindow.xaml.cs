using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using DynamicWindow.App.ViewModels;
using DynamicWindow.Core.Interfaces;
using DynamicWindow.Windows.WindowManagement;

namespace DynamicWindow.App.Views;

public sealed partial class VolumePillWindow : Window
{
    private readonly DynamicWindowHelper _helper;

    public VolumePillWindow()
    {
        InitializeComponent();
        VolumeRoot.DataContext = App.Services.GetRequiredService<MainDynamicWindowViewModel>();
        _helper = new DynamicWindowHelper(this);
        _helper.ConfigureAsFloatingDynamicWindow();
    }

    public void PositionBelowMainPill(double mainBottomDip, ISettingsService settingsService)
    {
        var s = settingsService.Current;
        var clone = new DynamicWindow.Core.Models.DynamicWindowSettings
        {
            PositionMode = s.PositionMode,
            HorizontalOffset = s.HorizontalOffset,
            VerticalOffset = mainBottomDip + 8,
            SelectedMonitorIndex = s.SelectedMonitorIndex
        };
        _helper.UpdateBounds(264, 46, 23, clone);
    }
}
