using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Newton.Core.Services;
using Microsoft.UI.Dispatching;

namespace Newton.App.ViewModels;

public partial class MainNewtonndViewModel : ObservableObject
{
    private readonly IActivityService _activityService;
    private readonly IMusicService _musicService;
    private readonly INotificationService _notificationService;
    private readonly ISystemMonitorService _systemMonitorService;
    private readonly IAiService _aiService;
    private readonly ISettingsService _settingsService;
    private readonly ITimerService _timerService;

    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private NewtonndVisualState _visualState = NewtonndVisualState.Compact;

    [ObservableProperty]
    private ActivityItem? _primaryActivity;

    [ObservableProperty]
    private MusicTrack _currentTrack;

    [ObservableProperty]
    private SystemStats _systemStats;

    [ObservableProperty]
    private NewtonSettings _settings;

    [ObservableProperty]
    private AiModelOption _selectedAiModel;

    [ObservableProperty]
    private string _aiPromptText = string.Empty;

    [ObservableProperty]
    private bool _isAiSending;

    [ObservableProperty]
    private int _customTimerMinutes = 10;

    public ObservableCollection<ActivityItem> Activities { get; } = new();
    public ObservableCollection<NotificationItem> Notifications { get; } = new();
    public ObservableCollection<AiMessage> AiMessages { get; } = new();
    public ObservableCollection<AiModelOption> AiModels { get; } = new();
    public ObservableCollection<QuickControlItem> QuickControls { get; } = new();

    public bool IsCompact => VisualState == NewtonndVisualState.Compact;
    public bool IsExpanded => VisualState == NewtonndVisualState.Expanded;
    public bool IsDashboard => VisualState == NewtonndVisualState.Dashboard;

    public bool IsMusicPrimary => PrimaryActivity?.Type == ActivityType.Music;
    public bool IsTimerPrimary => PrimaryActivity?.Type is ActivityType.Timer or ActivityType.Pomodoro;
    public bool IsScreenRecordingPrimary => PrimaryActivity?.Type == ActivityType.ScreenRecording;
    public bool IsFocusAssistPrimary => PrimaryActivity?.Type == ActivityType.FocusAssist;
    public bool IsLowBatteryPrimary => PrimaryActivity?.Type == ActivityType.LowBattery;
    public bool IsBluetoothPrimary => PrimaryActivity?.Type == ActivityType.BluetoothAccessory;
    public bool IsIncomingCallPrimary => PrimaryActivity?.Type == ActivityType.IncomingCall;
    public bool IsTransferOrBuildPrimary => PrimaryActivity?.Type is ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall or ActivityType.CustomDeveloper;
    public bool IsAiOrOtherPrimary => PrimaryActivity is not null && !IsMusicPrimary && !IsTimerPrimary && !IsScreenRecordingPrimary && !IsFocusAssistPrimary && !IsLowBatteryPrimary && !IsBluetoothPrimary && !IsIncomingCallPrimary && !IsTransferOrBuildPrimary;
    public bool IsNonMusicPrimary => PrimaryActivity is not null && PrimaryActivity.Type != ActivityType.Music;

    public string TimerDisplay => PrimaryActivity?.Metadata.TryGetValue("Remaining", out var rem) == true ? rem : "0:00";
    public string CompactTitle => PrimaryActivity?.Title ?? "Newton";
    public string CompactSubtitle => PrimaryActivity?.Subtitle ?? "Ready";
    public double CompactProgress => PrimaryActivity?.Progress ?? 0;
    public bool HasCompactProgress => Settings.ShowProgressInCompact && PrimaryActivity?.Progress is not null;

    public MainNewtonndViewModel(
        IActivityService activityService,
        IMusicService musicService,
        INotificationService notificationService,
        ISystemMonitorService systemMonitorService,
        IAiService aiService,
        ISettingsService settingsService,
        ITimerService timerService)
    {
        _activityService = activityService;
        _musicService = musicService;
        _notificationService = notificationService;
        _systemMonitorService = systemMonitorService;
        _aiService = aiService;
        _settingsService = settingsService;
        _timerService = timerService;

        _currentTrack = _musicService.CurrentTrack;
        _systemStats = _systemMonitorService.CurrentStats;
        _settings = _settingsService.Current;
        _selectedAiModel = _aiService.SelectedModel;

        InitializeQuickControls();
        RefreshAllCollections();

        _activityService.ActivitiesChanged += (_, _) => RunOnUi(RefreshActivities);
        _activityService.PrimaryActivityChanged += (_, act) => RunOnUi(() =>
        {
            PrimaryActivity = act;
            NotifyCompactProperties();
        });
        _musicService.TrackUpdated += (_, track) => RunOnUi(() =>
        {
            CurrentTrack = track;
            OnPropertyChanged(nameof(CurrentTrack));
        });
        _notificationService.NotificationsChanged += (_, _) => RunOnUi(RefreshNotifications);
        _systemMonitorService.StatsUpdated += (_, stats) => RunOnUi(() =>
        {
            SystemStats = stats;
            OnPropertyChanged(nameof(SystemStats));
        });
        _aiService.MessagesUpdated += (_, _) => RunOnUi(RefreshAiMessages);
        _settingsService.SettingsChanged += (_, s) => RunOnUi(() =>
        {
            Settings = s;
            NotifyCompactProperties();
        });
    }

    public void AttachDispatcher(DispatcherQueue dispatcherQueue) => _dispatcherQueue = dispatcherQueue;

    private void RunOnUi(Action action)
    {
        if (_dispatcherQueue is not null) _dispatcherQueue.TryEnqueue(() => action());
        else action();
    }

    partial void OnVisualStateChanged(NewtonndVisualState value)
    {
        OnPropertyChanged(nameof(IsCompact));
        OnPropertyChanged(nameof(IsExpanded));
        OnPropertyChanged(nameof(IsDashboard));
    }

    partial void OnPrimaryActivityChanged(ActivityItem? value)
    {
        OnPropertyChanged(nameof(IsMusicPrimary));
        OnPropertyChanged(nameof(IsTimerPrimary));
        OnPropertyChanged(nameof(IsScreenRecordingPrimary));
        OnPropertyChanged(nameof(IsFocusAssistPrimary));
        OnPropertyChanged(nameof(IsLowBatteryPrimary));
        OnPropertyChanged(nameof(IsBluetoothPrimary));
        OnPropertyChanged(nameof(IsIncomingCallPrimary));
        OnPropertyChanged(nameof(IsTransferOrBuildPrimary));
        OnPropertyChanged(nameof(IsAiOrOtherPrimary));
        OnPropertyChanged(nameof(IsNonMusicPrimary));
        OnPropertyChanged(nameof(TimerDisplay));
        NotifyCompactProperties();
    }

    partial void OnSelectedAiModelChanged(AiModelOption value)
    {
        if (value is not null) _aiService.SelectedModel = value;
    }

    private void NotifyCompactProperties()
    {
        OnPropertyChanged(nameof(CompactTitle));
        OnPropertyChanged(nameof(CompactSubtitle));
        OnPropertyChanged(nameof(CompactProgress));
        OnPropertyChanged(nameof(HasCompactProgress));
    }

    private void InitializeQuickControls()
    {
        QuickControls.Clear();
        QuickControls.Add(new QuickControlItem { Id = "snip", Title = "Screenshot", Subtitle = "Snipping Tool", IconGlyph = "\uE722", IsPrototypeOnly = false });
        QuickControls.Add(new QuickControlItem { Id = "sound", Title = "Sound", Subtitle = "Windows Audio", IconGlyph = "\uE767", IsPrototypeOnly = false });
        QuickControls.Add(new QuickControlItem { Id = "bt", Title = "Bluetooth", Subtitle = "Devices", IconGlyph = "\uE702", IsPrototypeOnly = false });
        QuickControls.Add(new QuickControlItem { Id = "focus", Title = "Focus Assist", Subtitle = "Windows DND", IconGlyph = "\uE708", IsPrototypeOnly = false });
        QuickControls.Add(new QuickControlItem { Id = "manage", Title = "Manage Newton", Subtitle = "Control Center", IconGlyph = "\uE9E9", IsPrototypeOnly = false });
        QuickControls.Add(new QuickControlItem { Id = "settings", Title = "Settings", Subtitle = "Preferences", IconGlyph = "\uE713", IsPrototypeOnly = false });
    }

    private void RefreshAllCollections()
    {
        PrimaryActivity = _activityService.PrimaryActivity;
        RefreshActivities();
        RefreshNotifications();
        RefreshAiMessages();
        AiModels.Clear();
        foreach (var m in _aiService.AvailableModels) AiModels.Add(m);
    }

    private void RefreshActivities()
    {
        Activities.Clear();
        foreach (var act in _activityService.Activities) Activities.Add(act);
        PrimaryActivity = _activityService.PrimaryActivity;
        NotifyCompactProperties();
    }

    private void RefreshNotifications()
    {
        Notifications.Clear();
        foreach (var n in _notificationService.Notifications) Notifications.Add(n);
    }

    private void RefreshAiMessages()
    {
        AiMessages.Clear();
        foreach (var msg in _aiService.Messages) AiMessages.Add(msg);
    }

    [RelayCommand] private void SetState(NewtonndVisualState state) => VisualState = state;
    [RelayCommand] private void ToggleExpand() => VisualState = VisualState == NewtonndVisualState.Compact ? NewtonndVisualState.Expanded : NewtonndVisualState.Compact;
    [RelayCommand] private void OpenDashboard() => VisualState = NewtonndVisualState.Dashboard;
    [RelayCommand] private void Collapse() => VisualState = NewtonndVisualState.Compact;

    [RelayCommand] private void TogglePlayPause() => _musicService.TogglePlayPause();
    [RelayCommand] private void NextTrack() => _musicService.NextTrack();
    [RelayCommand] private void PreviousTrack() => _musicService.PreviousTrack();

    [RelayCommand]
    private void SelectActivity(string activityId)
    {
        _activityService.SetPrimaryActivity(activityId);
        if (VisualState == NewtonndVisualState.Compact) VisualState = NewtonndVisualState.Expanded;
    }

    [RelayCommand]
    private void ExecuteActivityAction(string actionId)
    {
        if (PrimaryActivity is not null) _activityService.ExecuteAction(PrimaryActivity.Id, actionId);
    }

    [RelayCommand]
    private void ToggleQuickControl(QuickControlItem? item)
    {
        if (item is null) return;
        switch (item.Id)
        {
            case "settings": App.OpenSettingsWindow(); break;
            case "manage": App.OpenManageWindow(); break;
            case "snip": LaunchUri("ms-screenclip:"); break;
            case "sound": LaunchUri("ms-settings:sound"); break;
            case "bt": LaunchUri("ms-settings:bluetooth"); break;
            case "focus": LaunchUri("ms-settings:quiethours"); break;
        }
    }

    private static void LaunchUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Ignore if URI handler unavailable
        }
    }

    [RelayCommand] private void DismissNotification(string id) => _notificationService.DismissNotification(id);
    [RelayCommand] private void ClearNotifications() => _notificationService.ClearAll();

    [RelayCommand]
    private async Task SendAiPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(AiPromptText) || IsAiSending) return;
        var prompt = AiPromptText;
        AiPromptText = string.Empty;
        IsAiSending = true;
        try { await _aiService.SendMessageAsync(prompt); }
        finally { IsAiSending = false; }
    }

    // Real Timer & Pomodoro controls
    [RelayCommand] private void StartTimer5Min() { _timerService.StartTimer(300, "5m Timer"); VisualState = NewtonndVisualState.Expanded; }
    [RelayCommand] private void StartTimer15Min() { _timerService.StartTimer(900, "15m Timer"); VisualState = NewtonndVisualState.Expanded; }
    [RelayCommand] private void StartPomodoro25Min() { _timerService.StartTimer(1500, "Pomodoro • Deep Work", true); VisualState = NewtonndVisualState.Expanded; }
    [RelayCommand] private void StartCustomTimer() { _timerService.StartTimer(Math.Max(1, CustomTimerMinutes) * 60, $"{CustomTimerMinutes}m Focus"); VisualState = NewtonndVisualState.Expanded; }
    [RelayCommand] private void ToggleTimerPause() => _timerService.TogglePauseResume();
    [RelayCommand] private void StopTimer() => _timerService.StopTimer();

    [RelayCommand] private async Task SaveQuickSettingsAsync() => await _settingsService.SaveAsync(Settings);
    [RelayCommand] private void OpenSettings() => App.OpenSettingsWindow();
    [RelayCommand] private void OpenManage() => App.OpenManageWindow();
}
