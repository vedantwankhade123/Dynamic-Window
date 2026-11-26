using Isla.App.ViewModels;
using Isla.App.Views;
using Isla.Core.Interfaces;
using Isla.Infrastructure.DependencyInjection;
using Isla.Windows.Media;
using Isla.Windows.SystemMonitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Isla.App;

public partial class App : Application
{
    private Window? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private ManageWindow? _manageWindow;

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddIslaCoreServices();
        services.AddSingleton<IMusicService, WindowsMediaService>();
        services.AddSingleton<ISystemMonitorService, WindowsSystemMonitorService>();
        services.AddSingleton<MainIslandViewModel>();
        services.AddTransient<SettingsViewModel>();
        Services = services.BuildServiceProvider();

        UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logger = Services.GetService<ILogger<App>>();
        logger?.LogCritical(e.Exception, "Unhandled UI exception in Isla.");
        e.Handled = true;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        _mainWindow = new MainWindow();
        _mainWindow.Activate();
        OpenManageWindow();
    }

    public static void OpenSettingsWindow()
    {
        if (Current is not App app) return;

        if (app._settingsWindow is null)
        {
            app._settingsWindow = new SettingsWindow();
            app._settingsWindow.Closed += (_, _) => app._settingsWindow = null;
        }
        app._settingsWindow.Activate();
    }

    public static void OpenManageWindow()
    {
        if (Current is not App app) return;

        if (app._manageWindow is null)
        {
            app._manageWindow = new ManageWindow();
            app._manageWindow.Closed += (_, _) => app._manageWindow = null;
        }
        app._manageWindow.Activate();
    }
}
