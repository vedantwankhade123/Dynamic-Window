using Isla.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace Isla.App.Views;

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

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(840, 600));
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag) return;

        SectionTitle.Text = tag;
        GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        AppearancePanel.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PositionPanel.Visibility = tag == "Position" ? Visibility.Visible : Visibility.Collapsed;
        InteractionsPanel.Visibility = tag == "Interactions" ? Visibility.Visible : Visibility.Collapsed;
        ActivitiesPanel.Visibility = tag == "Activities" ? Visibility.Visible : Visibility.Collapsed;
        AiPanel.Visibility = tag == "AI" ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;
    }
}
