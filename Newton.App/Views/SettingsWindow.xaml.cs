using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newton.App.ViewModels;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace Newton.App.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        if (Content is FrameworkElement fe)
        {
            fe.DataContext = _viewModel;
        }

        ConfigureDarkTitleBar();
        UpdateThemeCardBorders();
    }

    private void ConfigureDarkTitleBar()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(980, 650));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(SettingsTitleBar);

        var tb = appWindow.TitleBar;
        tb.ButtonBackgroundColor = Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        tb.ButtonForegroundColor = Color.FromArgb(255, 229, 229, 234);
        tb.ButtonInactiveForegroundColor = Color.FromArgb(255, 142, 142, 147);
        tb.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
        tb.ButtonHoverForegroundColor = Colors.White;
    }

    private void OnSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        AppearancePanel.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PositionPanel.Visibility = tag == "Position" ? Visibility.Visible : Visibility.Collapsed;
        InteractionsPanel.Visibility = tag == "Interactions" ? Visibility.Visible : Visibility.Collapsed;
        ActivitiesPanel.Visibility = tag == "Activities" ? Visibility.Visible : Visibility.Collapsed;
        AiPanel.Visibility = tag == "AI" ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;

        var activeBg = new SolidColorBrush(Color.FromArgb(255, 37, 37, 45));
        var clearBg = new SolidColorBrush(Colors.Transparent);
        NavGeneral.Background = tag == "General" ? activeBg : clearBg;
        NavAppearance.Background = tag == "Appearance" ? activeBg : clearBg;
        NavPosition.Background = tag == "Position" ? activeBg : clearBg;
        NavInteractions.Background = tag == "Interactions" ? activeBg : clearBg;
        NavActivities.Background = tag == "Activities" ? activeBg : clearBg;
        NavAi.Background = tag == "AI" ? activeBg : clearBg;
        NavAbout.Background = tag == "About" ? activeBg : clearBg;

        (SectionTitle.Text, SectionSubtitle.Text) = tag switch
        {
            "Appearance" => ("Appearance settings", "Change how Newton looks and feels on your desktop."),
            "Position" => ("Position settings", "Configure anchor placement, offsets, and multi-monitor target."),
            "Interactions" => ("Interaction settings", "Customize hover, click, drag, and global keyboard shortcuts."),
            "Activities" => ("Activity settings", "Enable or disable live Windows integrations in the pill."),
            "AI" => ("AI Assistant settings", "Configure your local Ollama endpoint and default model."),
            "About" => ("About Newton", "Version, architecture, and open-source information."),
            _ => ("General settings", "Configure Newton startup, visibility, and language preferences.")
        };
    }

    private void OnThemeCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idxStr && int.TryParse(idxStr, out int idx))
        {
            _viewModel.ThemeIndex = idx;
            UpdateThemeCardBorders();
        }
    }

    private void UpdateThemeCardBorders()
    {
        var active = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
        var inactive = new SolidColorBrush(Color.FromArgb(36, 255, 255, 255));

        ThemeCardDark.BorderBrush = _viewModel.ThemeIndex == 0 ? active : inactive;
        ThemeCardDark.BorderThickness = new Thickness(_viewModel.ThemeIndex == 0 ? 2 : 1);
        ThemeCardLight.BorderBrush = _viewModel.ThemeIndex == 1 ? active : inactive;
        ThemeCardLight.BorderThickness = new Thickness(_viewModel.ThemeIndex == 1 ? 2 : 1);
        ThemeCardSystem.BorderBrush = _viewModel.ThemeIndex == 2 ? active : inactive;
        ThemeCardSystem.BorderThickness = new Thickness(_viewModel.ThemeIndex == 2 ? 2 : 1);
    }
}
