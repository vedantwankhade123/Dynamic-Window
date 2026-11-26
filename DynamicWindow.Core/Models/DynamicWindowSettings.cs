namespace DynamicWindow.Core.Models;

public enum DynamicWindowThemeMode
{
    Dark,
    Light,
    System
}

public enum DynamicWindowPositionMode
{
    TopCenter,
    TopLeft,
    TopRight,
    Custom
}

public enum DynamicWindowVisualState
{
    Compact,
    Expanded,
    Dashboard
}

public sealed class DynamicWindowSettings
{
    // General
    public bool StartWithWindows { get; set; } = true;
    public bool StartupPreferenceInitialized { get; set; }
    public bool IsDynamicWindowEnabled { get; set; } = true;
    public bool ShowOnStartup { get; set; } = true;
    public string Language { get; set; } = "English (US)";

    // Appearance
    public DynamicWindowThemeMode ThemeMode { get; set; } = DynamicWindowThemeMode.Dark;
    public string AccentColorHex { get; set; } = "#60A5FA";
    public double DynamicWindowOpacity { get; set; } = 0.96;
    public double CornerRadius { get; set; } = 22;
    public double CompactWidth { get; set; } = 220;
    public double CompactHeight { get; set; } = 44;
    public double AnimationSpeed { get; set; } = 1.0;
    public bool EnableBlur { get; set; } = true;

    // Position
    public DynamicWindowPositionMode PositionMode { get; set; } = DynamicWindowPositionMode.TopCenter;
    public double HorizontalOffset { get; set; } = 0;
    public double VerticalOffset { get; set; } = 10;
    public int SelectedMonitorIndex { get; set; } = 0;

    // Interactions
    public bool ClickToExpand { get; set; } = true;
    public bool DoubleClickForDashboard { get; set; } = true;
    public string ToggleShortcut { get; set; } = "Ctrl + N";
    public int AutoCollapseSeconds { get; set; } = 0;
    public bool ClickOutsideToCollapse { get; set; } = true;
    public bool DragToReposition { get; set; } = false;
    public bool AutoDodgeBrowserTabs { get; set; } = true;

    // Activities & Per-Usecase Customization
    public bool EnableMusic { get; set; } = true;
    public bool EnableDownloads { get; set; } = true;
    public bool EnableTimers { get; set; } = true;
    public bool EnableAi { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableSystemOps { get; set; } = true;
    public bool EnableDevActivities { get; set; } = true;
    public bool EnableClipboardTracking { get; set; } = true;
    public bool ShowProgressInCompact { get; set; } = true;
    public bool ShowPlatformBadge { get; set; } = true;

    // Default Handlers (Use DynamicWindow as default overlay/tool for these usecases)
    public bool DefaultForMediaOverlay { get; set; } = true;
    public bool DefaultForTimers { get; set; } = true;
    public bool DefaultForClipboardBar { get; set; } = true;
    public bool DefaultForBatteryAlerts { get; set; } = true;
    public bool DefaultForAiPrompt { get; set; } = true;

    // AI
    public bool EnableAiFeatures { get; set; } = true;
    public string DefaultAiProvider { get; set; } = "Ollama (Local)";
    public string LocalEndpoint { get; set; } = "http://localhost:11434";
    public string DefaultLocalModel { get; set; } = "llama3.2:latest";
    public string CloudProvider { get; set; } = "Gemini API (Disabled)";
    public bool ShareActiveContextWithAi { get; set; } = false;
}
