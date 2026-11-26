using Newton.App.ViewModels;
using Newton.App.Views;
using Newton.App.Services;
using Newton.Core.Interfaces;
using Newton.Core.Services;
using Newton.Infrastructure.DependencyInjection;
using Newton.Windows.Clipboard;
using Newton.Windows.Activities;
using Newton.Windows.Media;
using Newton.Windows.Startup;
using Newton.Windows.SystemMonitoring;
using Newton.Windows.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Newton.App;

public partial class App : Application
{
    private Window? _mainWindow;
    private ManageWindow? _manageWindow;
    private NewtonActivityBridge? _activityBridge;
    private UniversalReplyService? _chatNotifications;
    private NewtonChatActivityAdapter? _chatActivityAdapter;
    private Microsoft.UI.Dispatching.DispatcherQueue? _uiDispatcher;

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddNewtonCoreServices();
        services.AddSingleton<IMusicService, WindowsMediaService>();
        services.AddSingleton<ISystemMonitorService, WindowsSystemMonitorService>();
        services.AddSingleton<IClipboardService, WindowsClipboardService>();
        services.AddSingleton<WindowsStartupService>();
        services.AddSingleton<NewtonActivityBridge>();
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

            if (!settingsService.Current.StartupPreferenceInitialized)
            {
                settingsService.Current.StartWithWindows = true;
                settingsService.Current.StartupPreferenceInitialized = true;
                await settingsService.SaveAsync(settingsService.Current);
            }

            var startupService = Services.GetRequiredService<WindowsStartupService>();
            startupService.SetEnabled(settingsService.Current.StartWithWindows);
            settingsService.SettingsChanged += (_, settings) => startupService.SetEnabled(settings.StartWithWindows);

            _activityBridge = Services.GetRequiredService<NewtonActivityBridge>();
            _activityBridge.Start();

            _uiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
            ((MainWindow)_mainWindow).RefreshIslandVisibility();
        }
        catch (Exception ex)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:O}] OnLaunched: {ex}\n\n");
        }
    }

    /// <summary>
    /// Call this from a user-initiated Newton command to enable notification access.
    /// The listener asks Windows for consent and publishes messages through the activity adapter.
    /// </summary>
    public static async Task<bool> EnableChatNotificationsAsync()
    {
        if (Current is not App app || app._uiDispatcher is null) return false;
        if (app._chatNotifications is not null) return true;

        NewtonChatActivityAdapter? adapter = null;
        UniversalReplyService? service;
        try
        {
            service = await UniversalReplyService.CreateAsync(
                app._uiDispatcher,
                onCreated: created => adapter = new NewtonChatActivityAdapter(
                    created,
                    Services.GetRequiredService<ActivityManager>()));
        }
        catch (Exception ex)
        {
            Services.GetService<ILogger<App>>()?.LogWarning(ex, "Chat notification access could not be initialized.");
            adapter?.Dispose();
            return false;
        }
        if (service is null) return false;

        app._chatNotifications = service;
        app._chatActivityAdapter = adapter;
        return true;
    }

    public static void OpenSettingsWindow()
    {
        if (Current is not App app) return;
        if (app._manageWindow is null)
        {
            app._manageWindow = new ManageWindow();
            app._manageWindow.Closed += (_, _) => app._manageWindow = null;
        }
        app._manageWindow.NavigateToSettings();
        app._manageWindow.Activate();
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
