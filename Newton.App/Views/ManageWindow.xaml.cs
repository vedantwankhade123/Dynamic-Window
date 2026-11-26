using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newton.App.ViewModels;
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
        appWindow.Resize(new SizeInt32(1020, 680));
    }

    private void OnSectionNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        OverviewSection.Visibility = tag == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        PillStudioSection.Visibility = tag == "PillStudio" ? Visibility.Visible : Visibility.Collapsed;
        ClipboardSection.Visibility = tag == "Clipboard" ? Visibility.Visible : Visibility.Collapsed;
        DefaultsSection.Visibility = tag == "Defaults" ? Visibility.Visible : Visibility.Collapsed;

        HeaderTitle.Text = tag switch
        {
            "PillStudio" => "Pill Customization",
            "Clipboard" => "Clipboard History",
            "Defaults" => "Default Handlers",
            _ => "Overview & Pills"
        };
    }

    private void OnActivityFocusClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && Content is FrameworkElement fe && fe.DataContext is MainNewtonndViewModel vm)
        {
            vm.SelectActivityCommand.Execute(id);
        }
    }

    private void OnQuickToolTagClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && Content is FrameworkElement fe && fe.DataContext is MainNewtonndViewModel vm)
        {
            var match = vm.QuickControls.FirstOrDefault(q => q.Id == id);
            if (match is not null) vm.ToggleQuickControlCommand.Execute(match);
        }
    }

    private void OnCopyClipboardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string text && Content is FrameworkElement fe && fe.DataContext is MainNewtonndViewModel vm)
        {
            vm.CopyClipboardTextCommand.Execute(text);
        }
    }
}
