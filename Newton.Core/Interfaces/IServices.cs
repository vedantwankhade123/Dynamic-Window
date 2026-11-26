using Newton.Core.Events;
using Newton.Core.Models;

namespace Newton.Core.Interfaces;

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
    NewtonSettings Current { get; }
    event EventHandler<NewtonSettings>? SettingsChanged;

    Task LoadAsync();
    Task SaveAsync(NewtonSettings settings);
    Task ResetToDefaultsAsync();
}

public interface IClipboardService
{
    IReadOnlyList<ClipboardHistoryItem> Items { get; }
    event EventHandler? HistoryChanged;
    event EventHandler<ClipboardHistoryItem>? ItemCaptured;

    Task RefreshAsync();
    void CopyText(string text);
    Task CopyItemAsync(ClipboardHistoryItem item);
    void AddDroppedText(string text);
    void AddDroppedFiles(IReadOnlyList<string> filePaths);
    void TogglePin(string id);
    void RemoveItem(string id);
    void ClearHistory();
}
