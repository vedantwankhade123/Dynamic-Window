using Newton.Core.Interfaces;
using Newton.Core.Models;

namespace Newton.Core.Services;

public interface ITimerService
{
    bool IsRunning { get; }
    int RemainingSeconds { get; }
    void StartTimer(int seconds, string label = "Focus Timer", bool isPomodoro = false);
    void TogglePauseResume();
    void ResetTimer();
    void StopTimer();
}

public sealed class RealTimerAndNotificationService : INotificationService, ITimerService, IDisposable
{
    private readonly ActivityManager _activityManager;
    private readonly Timer _tickTimer;
    private readonly List<NotificationItem> _notifications = new();

    private int _totalSeconds;
    private int _remainingSeconds;
    private bool _isRunning;
    private bool _hasActiveTimer;
    private string _label = "Focus Timer";
    private bool _isPomodoro;

    public bool IsRunning => _isRunning;
    public int RemainingSeconds => _remainingSeconds;
    public IReadOnlyList<NotificationItem> Notifications => _notifications.AsReadOnly();

    public event EventHandler<NotificationItem>? NotificationReceived;
    public event EventHandler? NotificationsChanged;

    public RealTimerAndNotificationService(ActivityManager activityManager)
    {
        _activityManager = activityManager;
        _activityManager.ActionRequested += OnActionRequested;
        _tickTimer = new Timer(OnTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void OnActionRequested(object? sender, (string ActivityId, string ActionId) e)
    {
        switch (e.ActionId)
        {
            case "timer-toggle": TogglePauseResume(); break;
            case "timer-reset": ResetTimer(); break;
            case "timer-stop": StopTimer(); break;
            case "notif-ack": _activityManager.RemoveActivity(e.ActivityId); break;
        }
    }

    private void OnTick(object? state)
    {
        if (!_hasActiveTimer || !_isRunning) return;

        _remainingSeconds = Math.Max(0, _remainingSeconds - 1);
        PublishTimerActivity();

        if (_remainingSeconds == 0)
        {
            _isRunning = false;
            PushNotification(new NotificationItem
            {
                AppName = "Newton Timer",
                IconGlyph = "\uE916",
                AccentHex = "#F59E0B",
                Title = $"{_label} Completed",
                Message = "Your scheduled session has finished.",
                IsSimulated = false
            });
        }
    }

    public void StartTimer(int seconds, string label = "Focus Timer", bool isPomodoro = false)
    {
        _totalSeconds = Math.Max(1, seconds);
        _remainingSeconds = _totalSeconds;
        _label = label;
        _isPomodoro = isPomodoro;
        _hasActiveTimer = true;
        _isRunning = true;
        PublishTimerActivity();
        _activityManager.SetPrimaryActivity("newton-timer");
    }

    public void TogglePauseResume()
    {
        if (!_hasActiveTimer || _remainingSeconds <= 0) return;
        _isRunning = !_isRunning;
        PublishTimerActivity();
    }

    public void ResetTimer()
    {
        if (!_hasActiveTimer) return;
        _remainingSeconds = _totalSeconds;
        _isRunning = true;
        PublishTimerActivity();
    }

    public void StopTimer()
    {
        _hasActiveTimer = false;
        _isRunning = false;
        _remainingSeconds = 0;
        _activityManager.RemoveActivity("newton-timer");
    }

    private void PublishTimerActivity()
    {
        var ts = TimeSpan.FromSeconds(_remainingSeconds);
        var formatted = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        var progress = _totalSeconds > 0
            ? (double)(_totalSeconds - _remainingSeconds) / _totalSeconds * 100.0
            : 100;

        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "newton-timer",
            Type = _isPomodoro ? ActivityType.Pomodoro : ActivityType.Timer,
            Title = _label,
            Subtitle = _remainingSeconds == 0 ? "Completed" : $"{formatted} remaining",
            IconGlyph = _isPomodoro ? "\uE823" : "\uE916",
            AccentHex = "#F59E0B",
            Progress = progress,
            Status = _remainingSeconds == 0
                ? ActivityStatus.Completed
                : _isRunning ? ActivityStatus.Active : ActivityStatus.Paused,
            Priority = ActivityPriority.High,
            IsSimulated = false,
            Metadata = { ["Remaining"] = formatted },
            Actions =
            {
                new("timer-toggle", _isRunning ? "Pause" : "Resume", "\uE769", true),
                new("timer-reset", "Reset", "\uE72C"),
                new("timer-stop", "Stop", "\uE71A", false, true)
            }
        });
    }

    public void PushNotification(NotificationItem notification)
    {
        _notifications.Insert(0, notification);
        if (_notifications.Count > 30) _notifications.RemoveAt(_notifications.Count - 1);
        NotificationReceived?.Invoke(this, notification);
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkAsRead(string notificationId)
    {
        var item = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (item is not null)
        {
            item.IsRead = true;
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void DismissNotification(string notificationId)
    {
        _notifications.RemoveAll(n => n.Id == notificationId);
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearAll()
    {
        _notifications.Clear();
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _tickTimer.Dispose();
}
