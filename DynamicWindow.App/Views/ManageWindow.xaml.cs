using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DynamicWindow.App.ViewModels;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace DynamicWindow.App.Views;

public sealed partial class ManageWindow : Window
{
    public ManageWindow()
    {
        InitializeComponent();
        if (Content is FrameworkElement fe)
        {
            fe.DataContext = App.Services.GetRequiredService<MainDynamicWindowViewModel>();
        }

        SettingsWorkspace.DataContext = App.Services.GetRequiredService<SettingsViewModel>();

        ConfigureDarkTitleBar();
    }

    public void NavigateToSettings()
    {
        ManageWorkspace.Visibility = Visibility.Collapsed;
        SettingsWorkspace.Visibility = Visibility.Visible;
    }

    private void OnSettingsBackRequested(object sender, EventArgs e)
    {
        SettingsWorkspace.Visibility = Visibility.Collapsed;
        ManageWorkspace.Visibility = Visibility.Visible;
    }

    private void ConfigureDarkTitleBar()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(960, 640));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(CustomTitleBar);

        var tb = appWindow.TitleBar;
        tb.ButtonBackgroundColor = Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        tb.ButtonForegroundColor = Color.FromArgb(255, 229, 229, 234);
        tb.ButtonInactiveForegroundColor = Color.FromArgb(255, 142, 142, 147);
        tb.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
        tb.ButtonHoverForegroundColor = Colors.White;
    }

    private void OnSectionNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        OverviewSection.Visibility = tag == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        PillStudioSection.Visibility = tag == "PillStudio" ? Visibility.Visible : Visibility.Collapsed;
        ClipboardSection.Visibility = tag == "Clipboard" ? Visibility.Visible : Visibility.Collapsed;
        DefaultsSection.Visibility = tag == "Defaults" ? Visibility.Visible : Visibility.Collapsed;

        var activeBg = new SolidColorBrush(Color.FromArgb(255, 37, 37, 44));
        var clearBg = new SolidColorBrush(Colors.Transparent);
        NavOverview.Background = tag == "Overview" ? activeBg : clearBg;
        NavPillStudio.Background = tag == "PillStudio" ? activeBg : clearBg;
        NavClipboard.Background = tag == "Clipboard" ? activeBg : clearBg;
        NavDefaults.Background = tag == "Defaults" ? activeBg : clearBg;

        HeaderTitle.Text = tag switch
        {
            "PillStudio" => "Pill Behavior",
            "Clipboard" => "Clipboard",
            "Defaults" => "Default Apps",
            _ => "Overview"
        };
    }

    private void OnActivityFocusClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && Content is FrameworkElement fe && fe.DataContext is MainDynamicWindowViewModel vm)
        {
            vm.SelectActivityCommand.Execute(id);
        }
    }

    private void OnQuickToolTagClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && Content is FrameworkElement fe && fe.DataContext is MainDynamicWindowViewModel vm)
        {
            var match = vm.QuickControls.FirstOrDefault(q => q.Id == id);
            if (match is not null) vm.ToggleQuickControlCommand.Execute(match);
        }
    }

    private void OnCopyClipboardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string text && Content is FrameworkElement fe && fe.DataContext is MainDynamicWindowViewModel vm)
        {
            vm.CopyClipboardTextCommand.Execute(text);
        }
    }
}
