using Newton.App.ViewModels;
using Newton.App.Views;
using Newton.Core.Interfaces;
using Newton.Infrastructure.DependencyInjection;
using Newton.Windows.Clipboard;
using Newton.Windows.Media;
using Newton.Windows.SystemMonitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Newton.App;

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
        services.AddNewtonCoreServices();
        services.AddSingleton<IMusicService, WindowsMediaService>();
        services.AddSingleton<ISystemMonitorService, WindowsSystemMonitorService>();
        services.AddSingleton<IClipboardService, WindowsClipboardService>();
        services.AddSingleton<MainNewtonndViewModel>();
        services.AddTransient<SettingsViewModel>();
        Services = services.BuildServiceProvider();

        UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logger = Services.GetService<ILogger<App>>();
        logger?.LogCritical(e.Exception, "Unhandled UI exception in Newton.");
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:O}] {e.Exception}\n\n");
        }
        catch { }
        e.Handled = true;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Ensure single instance so an older pill never covers the active one
            var currentPid = Environment.ProcessId;
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("Newton.App"))
            {
                if (p.Id != currentPid)
                {
                    try { p.Kill(); } catch { }
                }
            }

            var settingsService = Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            _mainWindow = new MainWindow();
            _mainWindow.Activate();
            OpenManageWindow();
        }
        catch (Exception ex)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:O}] OnLaunched: {ex}\n\n");
        }
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
