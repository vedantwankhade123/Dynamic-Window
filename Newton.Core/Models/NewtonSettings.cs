namespace Newton.Core.Models;

public enum NewtonThemeMode
{
    Dark,
    Light,
    System
}

public enum NewtonPositionMode
{
    TopCenter,
    TopLeft,
    TopRight,
    Custom
}

public enum NewtonndVisualState
{
    Compact,
    Expanded,
    Dashboard
}

public sealed class NewtonSettings
{
    // General
    public bool StartWithWindows { get; set; } = false;
    public bool IsNewtonndEnabled { get; set; } = true;
    public bool ShowOnStartup { get; set; } = true;
    public string Language { get; set; } = "English (US)";

    // Appearance
    public NewtonThemeMode ThemeMode { get; set; } = NewtonThemeMode.Dark;
    public string AccentColorHex { get; set; } = "#60A5FA";
    public double NewtonndOpacity { get; set; } = 0.96;
    public double CornerRadius { get; set; } = 24;
    public double CompactWidth { get; set; } = 216;
    public double CompactHeight { get; set; } = 46;
    public double AnimationSpeed { get; set; } = 1.0;
    public bool EnableBlur { get; set; } = true;

    // Position
    public NewtonPositionMode PositionMode { get; set; } = NewtonPositionMode.TopCenter;
    public double HorizontalOffset { get; set; } = 0;
    public double VerticalOffset { get; set; } = 10;
    public int SelectedMonitorIndex { get; set; } = 0;

    // Interactions
    public bool ClickToExpand { get; set; } = true;
    public bool DoubleClickForDashboard { get; set; } = true;
    public bool HoverToPeek { get; set; } = false;
    public string ToggleShortcut { get; set; } = "Ctrl + Alt + I";
    public int AutoCollapseSeconds { get; set; } = 0;
    public bool ClickOutsideToCollapse { get; set; } = true;
    public bool DragToReposition { get; set; } = false;

    // Activities
    public bool EnableMusic { get; set; } = true;
    public bool EnableDownloads { get; set; } = true;
    public bool EnableTimers { get; set; } = true;
    public bool EnableAi { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableSystemOps { get; set; } = true;
    public bool EnableDevActivities { get; set; } = true;
    public bool ShowProgressInCompact { get; set; } = true;

    // AI
    public bool EnableAiFeatures { get; set; } = true;
    public string DefaultAiProvider { get; set; } = "Ollama (Local)";
    public string LocalEndpoint { get; set; } = "http://localhost:11434";
    public string DefaultLocalModel { get; set; } = "llama3.2:latest";
    public string CloudProvider { get; set; } = "Gemini API (Disabled)";
    public bool ShareActiveContextWithAi { get; set; } = false;
}
