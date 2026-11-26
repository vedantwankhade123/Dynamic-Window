using Isla.Core.Events;
using Isla.Core.Models;

namespace Isla.Core.Interfaces;

public interface IActivityService
{
    IReadOnlyList<ActivityItem> Activities { get; }
    ActivityItem? PrimaryActivity { get; }

    event EventHandler<ActivityChangedEventArgs>? ActivitiesChanged;
    event EventHandler<ActivityItem?>? PrimaryActivityChanged;

    void AddOrUpdateActivity(ActivityItem activity);
    void RemoveActivity(string activityId);
    void SetPrimaryActivity(string activityId);
    void ExecuteAction(string activityId, string actionId);
}

public interface IMusicService
{
    MusicTrack CurrentTrack { get; }
    bool IsPlaying { get; }
    event EventHandler<MusicTrack>? TrackUpdated;

    void TogglePlayPause();
    void Play();
    void Pause();
    void NextTrack();
    void PreviousTrack();
    void Seek(TimeSpan position);
    void SetVolume(double volume);
}

public interface INotificationService
{
    IReadOnlyList<NotificationItem> Notifications { get; }
    event EventHandler<NotificationItem>? NotificationReceived;
    event EventHandler? NotificationsChanged;

    void PushNotification(NotificationItem notification);
    void MarkAsRead(string notificationId);
    void DismissNotification(string notificationId);
    void ClearAll();
}

public interface ISystemMonitorService
{
    SystemStats CurrentStats { get; }
    event EventHandler<SystemStats>? StatsUpdated;
    void TriggerSpike();
}

public interface IAiService
{
    IReadOnlyList<AiModelOption> AvailableModels { get; }
    AiModelOption SelectedModel { get; set; }
    IReadOnlyList<AiMessage> Messages { get; }
    event EventHandler? MessagesUpdated;

    Task SendMessageAsync(string prompt, CancellationToken cancellationToken = default);
    void ClearHistory();
}

public interface ISettingsService
{
    IslaSettings Current { get; }
    event EventHandler<IslaSettings>? SettingsChanged;

    Task LoadAsync();
    Task SaveAsync(IslaSettings settings);
    Task ResetToDefaultsAsync();
}

public interface IDemoController
{
    bool IsDemoMode { get; }
    void StartMusicDemo();
    void ToggleMusicDemo();
    void StartDownloadDemo();
    void StartTimerDemo(int seconds = 90);
    void CompleteTimerDemo();
    void StartPomodoroDemo();
    void StartAiTaskDemo();
    void StartFileOperationDemo();
    void StartAppInstallDemo();
    void StartCustomDevDemo();
    void StartScreenRecordingDemo();
    void StartFocusAssistDemo();
    void StartLowBatteryDemo();
    void StartBluetoothDemo();
    void StartIncomingCallDemo();
    void SimulateNotification(string kind = "message");
    void TriggerSystemSpike();
    void ResetDemoData();
}
