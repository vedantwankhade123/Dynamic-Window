using Isla.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Isla.App.Views;

public sealed partial class DemoWindow : Window
{
    public DemoWindow()
    {
        InitializeComponent();
        if (Content is FrameworkElement fe)
        {
            fe.DataContext = App.Services.GetRequiredService<MainIslandViewModel>();
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(430, 500));
    }
}
