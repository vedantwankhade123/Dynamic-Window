using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Newton.App.ViewModels;
using Newton.Core.Interfaces;
using Newton.Windows.WindowManagement;

namespace Newton.App.Views;

public sealed partial class VolumePillWindow : Window
{
    private readonly NewtonndWindowHelper _helper;

    public VolumePillWindow()
    {
        InitializeComponent();
        VolumeRoot.DataContext = App.Services.GetRequiredService<MainNewtonndViewModel>();
        _helper = new NewtonndWindowHelper(this);
        _helper.ConfigureAsFloatingNewtonnd();
    }

    public void PositionBelowMainPill(double mainBottomDip, ISettingsService settingsService)
    {
        var s = settingsService.Current;
        var clone = new Newton.Core.Models.NewtonSettings
        {
            PositionMode = s.PositionMode,
            HorizontalOffset = s.HorizontalOffset,
            VerticalOffset = mainBottomDip + 8,
            SelectedMonitorIndex = s.SelectedMonitorIndex
        };
        _helper.UpdateBounds(264, 46, 23, clone);
    }
}
