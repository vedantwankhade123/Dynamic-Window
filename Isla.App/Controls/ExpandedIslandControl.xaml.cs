using Isla.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Isla.App.Controls;

public sealed partial class ExpandedIslandControl : UserControl
{
    public ExpandedIslandControl()
    {
        InitializeComponent();
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string actionId && DataContext is MainIslandViewModel vm)
        {
            vm.ExecuteActivityActionCommand.Execute(actionId);
        }
    }
}
