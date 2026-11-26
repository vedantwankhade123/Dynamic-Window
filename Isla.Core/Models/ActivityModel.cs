namespace Isla.Core.Models;

public enum ActivityType
{
    Music,
    Download,
    FileOperation,
    Timer,
    Pomodoro,
    AiGeneration,
    AppInstall,
    SystemOperation,
    CustomDeveloper,
    Notification,
    ScreenRecording,
    FocusAssist,
    LowBattery,
    BluetoothAccessory,
    IncomingCall
}

public enum ActivityStatus
{
    Idle,
    Active,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public enum ActivityPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public sealed record ActivityAction(
    string ActionId,
    string Label,
    string IconGlyph = "",
    bool IsPrimary = false,
    bool IsDestructive = false);

public sealed class ActivityItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public ActivityType Type { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uE946";
    public string AccentHex { get; set; } = "#60A5FA";
    public double? Progress { get; set; }
    public bool IsIndeterminate { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Active;
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now;
    public string? ImageUri { get; set; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public List<ActivityAction> Actions { get; init; } = new();
    public ActivityPriority Priority { get; set; } = ActivityPriority.Normal;
    public bool IsExpanded { get; set; }
    public bool IsSimulated { get; init; } = true;
}
