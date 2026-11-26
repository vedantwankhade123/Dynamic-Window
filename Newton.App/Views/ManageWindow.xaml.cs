using Newton.App.ViewModels;
using Newton.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace Newton.App.Views;

public sealed partial class ManageWindow : Window
{
    public ManageWindow()
    {
        InitializeComponent();
        if (Content is FrameworkElement fe)
        {
            fe.DataContext = App.Services.GetRequiredService<MainNewtonndViewModel>();
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(900, 640));
    }

    private void OnActivityFocusClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && Content is FrameworkElement fe && fe.DataContext is MainNewtonndViewModel vm)
        {
            vm.SelectActivityCommand.Execute(id);
        }
    }

    private void OnQuickControlClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is QuickControlItem item && Content is FrameworkElement fe && fe.DataContext is MainNewtonndViewModel vm)
        {
            vm.ToggleQuickControlCommand.Execute(item);
        }
    }
}
