using Isla.App.ViewModels;
using Isla.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Isla.App.Controls;

public sealed partial class DashboardControl : UserControl
{
    public DashboardControl()
    {
        InitializeComponent();
    }

    private void OnActivityCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainIslandViewModel vm)
        {
            vm.SelectActivityCommand.Execute(id);
        }
    }

    private void OnQuickControlClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is QuickControlItem item && DataContext is MainIslandViewModel vm)
        {
            vm.ToggleQuickControlCommand.Execute(item);
        }
    }

    private void OnDismissNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainIslandViewModel vm)
        {
            vm.DismissNotificationCommand.Execute(id);
        }
    }
}
