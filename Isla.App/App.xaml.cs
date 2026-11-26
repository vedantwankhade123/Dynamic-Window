using Isla.App.ViewModels;
using Isla.App.Views;
using Isla.Core.Interfaces;
using Isla.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Isla.App;

public partial class App : Application
{
    private Window? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private DemoWindow? _demoWindow;

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddIslaCoreServices();
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
        OpenDemoWindow();
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

    public static void OpenDemoWindow()
    {
        if (Current is not App app) return;

        if (app._demoWindow is null)
        {
            app._demoWindow = new DemoWindow();
            app._demoWindow.Closed += (_, _) => app._demoWindow = null;
        }
        app._demoWindow.Activate();
    }
}
