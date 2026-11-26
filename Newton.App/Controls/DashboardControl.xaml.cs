using Newton.App.ViewModels;
using Newton.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Newton.App.Controls;

public sealed partial class DashboardControl : UserControl
{
    public DashboardControl()
    {
        InitializeComponent();
    }

    private void OnActivityCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainNewtonndViewModel vm)
        {
            vm.SelectActivityCommand.Execute(id);
        }
    }

    private void OnQuickControlClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is QuickControlItem item && DataContext is MainNewtonndViewModel vm)
        {
            vm.ToggleQuickControlCommand.Execute(item);
        }
    }

    private void OnDismissNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainNewtonndViewModel vm)
        {
            vm.DismissNotificationCommand.Execute(id);
        }
    }
}
