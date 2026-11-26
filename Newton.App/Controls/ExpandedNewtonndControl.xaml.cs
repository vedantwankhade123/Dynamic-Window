using Newton.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Newton.App.Controls;

public sealed partial class ExpandedNewtonndControl : UserControl
{
    public ExpandedNewtonndControl()
    {
        InitializeComponent();
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string actionId && DataContext is MainNewtonndViewModel vm)
        {
            vm.ExecuteActivityActionCommand.Execute(actionId);
        }
    }
}
