using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Isla.Core.Interfaces;
using Isla.Core.Models;
using Microsoft.UI.Dispatching;

namespace Isla.App.ViewModels;

public partial class MainIslandViewModel : ObservableObject
{
    private readonly IActivityService _activityService;
    private readonly IMusicService _musicService;
    private readonly INotificationService _notificationService;
    private readonly ISystemMonitorService _systemMonitorService;
    private readonly IAiService _aiService;
    private readonly ISettingsService _settingsService;
    private readonly IDemoController _demoController;

    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private IslandVisualState _visualState = IslandVisualState.Compact;

    [ObservableProperty]
    private ActivityItem? _primaryActivity;

    [ObservableProperty]
    private MusicTrack _currentTrack;

    [ObservableProperty]
    private SystemStats _systemStats;

    [ObservableProperty]
    private IslaSettings _settings;

    [ObservableProperty]
    private AiModelOption _selectedAiModel;

    [ObservableProperty]
    private string _aiPromptText = string.Empty;

    [ObservableProperty]
    private bool _isAiSending;

    [ObservableProperty]
    private string _dashboardSection = "Overview";

    public ObservableCollection<ActivityItem> Activities { get; } = new();
    public ObservableCollection<NotificationItem> Notifications { get; } = new();
    public ObservableCollection<AiMessage> AiMessages { get; } = new();
    public ObservableCollection<AiModelOption> AiModels { get; } = new();
    public ObservableCollection<QuickControlItem> QuickControls { get; } = new();

    public bool IsCompact => VisualState == IslandVisualState.Compact;
    public bool IsExpanded => VisualState == IslandVisualState.Expanded;
    public bool IsDashboard => VisualState == IslandVisualState.Dashboard;

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

    public string TimerDisplay => PrimaryActivity?.Metadata.TryGetValue("Remaining", out var rem) == true ? rem : "0:22";

    public string CompactTitle => PrimaryActivity?.Title ?? "Isla Ready";
    public string CompactSubtitle => PrimaryActivity?.Subtitle ?? "No active tasks";
    public double CompactProgress => PrimaryActivity?.Progress ?? 0;
    public bool HasCompactProgress => Settings.ShowProgressInCompact && PrimaryActivity?.Progress is not null;

    public MainIslandViewModel(
        IActivityService activityService,
        IMusicService musicService,
        INotificationService notificationService,
        ISystemMonitorService systemMonitorService,
        IAiService aiService,
        ISettingsService settingsService,
        IDemoController demoController)
    {
        _activityService = activityService;
        _musicService = musicService;
        _notificationService = notificationService;
        _systemMonitorService = systemMonitorService;
        _aiService = aiService;
        _settingsService = settingsService;
        _demoController = demoController;

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

    public void AttachDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    private void RunOnUi(Action action)
    {
        if (_dispatcherQueue is not null)
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
        else
        {
            action();
        }
    }

    partial void OnVisualStateChanged(IslandVisualState value)
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
        if (value is not null)
        {
            _aiService.SelectedModel = value;
        }
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
        QuickControls.Add(new QuickControlItem { Id = "wifi", Title = "Wi-Fi", Subtitle = "IslaNet-5G", IconGlyph = "\uE701", IsActive = true, IsPrototypeOnly = true });
        QuickControls.Add(new QuickControlItem { Id = "bt", Title = "Bluetooth", Subtitle = "2 connected", IconGlyph = "\uE702", IsActive = true, IsPrototypeOnly = true });
        QuickControls.Add(new QuickControlItem { Id = "night", Title = "Night Light", Subtitle = "Off", IconGlyph = "\uE708", IsActive = false, IsPrototypeOnly = true });
        QuickControls.Add(new QuickControlItem { Id = "focus", Title = "Focus Mode", Subtitle = "Do Not Disturb", IconGlyph = "\uE706", IsActive = false, IsPrototypeOnly = true });
        QuickControls.Add(new QuickControlItem { Id = "shot", Title = "Screenshot", Subtitle = "Snipping Tool", IconGlyph = "\uE722", IsActive = false, IsPrototypeOnly = true });
        QuickControls.Add(new QuickControlItem { Id = "settings", Title = "Settings", Subtitle = "Preferences", IconGlyph = "\uE713", IsActive = false, IsPrototypeOnly = false });
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
        foreach (var act in _activityService.Activities)
        {
            Activities.Add(act);
        }
        PrimaryActivity = _activityService.PrimaryActivity;
        NotifyCompactProperties();
    }

    private void RefreshNotifications()
    {
        Notifications.Clear();
        foreach (var n in _notificationService.Notifications)
        {
            Notifications.Add(n);
        }
    }

    private void RefreshAiMessages()
    {
        AiMessages.Clear();
        foreach (var msg in _aiService.Messages)
        {
            AiMessages.Add(msg);
        }
    }

    [RelayCommand]
    private void SetState(IslandVisualState state) => VisualState = state;

    [RelayCommand]
    private void ToggleExpand()
    {
        VisualState = VisualState == IslandVisualState.Compact
            ? IslandVisualState.Expanded
            : IslandVisualState.Compact;
    }

    [RelayCommand]
    private void OpenDashboard() => VisualState = IslandVisualState.Dashboard;

    [RelayCommand]
    private void Collapse() => VisualState = IslandVisualState.Compact;

    [RelayCommand]
    private void TogglePlayPause() => _musicService.TogglePlayPause();

    [RelayCommand]
    private void NextTrack() => _musicService.NextTrack();

    [RelayCommand]
    private void PreviousTrack() => _musicService.PreviousTrack();

    [RelayCommand]
    private void SelectActivity(string activityId)
    {
        _activityService.SetPrimaryActivity(activityId);
        if (VisualState == IslandVisualState.Compact)
        {
            VisualState = IslandVisualState.Expanded;
        }
    }

    [RelayCommand]
    private void ExecuteActivityAction(string actionId)
    {
        if (PrimaryActivity is not null)
        {
            _activityService.ExecuteAction(PrimaryActivity.Id, actionId);
        }
    }

    [RelayCommand]
    private void ToggleQuickControl(QuickControlItem? item)
    {
        if (item is null) return;
        if (item.Id == "settings")
        {
            App.OpenSettingsWindow();
            return;
        }

        item.IsActive = !item.IsActive;
        item.Subtitle = item.Id switch
        {
            "wifi" => item.IsActive ? "IslaNet-5G [Sim]" : "Disconnected [Sim]",
            "bt" => item.IsActive ? "2 connected [Sim]" : "Off [Sim]",
            "night" => item.IsActive ? "Warm 4200K [Sim]" : "Off [Sim]",
            "focus" => item.IsActive ? "Active [Sim]" : "Off [Sim]",
            _ => item.Subtitle
        };
        var idx = QuickControls.IndexOf(item);
        if (idx >= 0)
        {
            QuickControls[idx] = item;
        }
    }

    [RelayCommand]
    private void DismissNotification(string id) => _notificationService.DismissNotification(id);

    [RelayCommand]
    private void ClearNotifications() => _notificationService.ClearAll();

    [RelayCommand]
    private async Task SendAiPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(AiPromptText) || IsAiSending) return;
        var prompt = AiPromptText;
        AiPromptText = string.Empty;
        IsAiSending = true;
        try
        {
            await _aiService.SendMessageAsync(prompt);
        }
        finally
        {
            IsAiSending = false;
        }
    }

    // Demo triggers accessible from Dashboard & Demo Window (switch to Expanded to preview immediately)
    [RelayCommand] private void DemoMusic() { _demoController.StartMusicDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoDownload() { _demoController.StartDownloadDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoTimer() { _demoController.StartTimerDemo(90); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoCompleteTimer() { _demoController.CompleteTimerDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoPomodoro() { _demoController.StartPomodoroDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoAiTask() { _demoController.StartAiTaskDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoFileOp() { _demoController.StartFileOperationDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoAppInstall() { _demoController.StartAppInstallDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoCustomDev() { _demoController.StartCustomDevDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoScreenRecording() { _demoController.StartScreenRecordingDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoFocusAssist() { _demoController.StartFocusAssistDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoLowBattery() { _demoController.StartLowBatteryDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoBluetooth() { _demoController.StartBluetoothDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoIncomingCall() { _demoController.StartIncomingCallDemo(); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoNotification(string kind) { _demoController.SimulateNotification(kind); VisualState = IslandVisualState.Expanded; }
    [RelayCommand] private void DemoSpike() => _demoController.TriggerSystemSpike();
    [RelayCommand] private void DemoReset() => _demoController.ResetDemoData();
    [RelayCommand] private void OpenSettings() => App.OpenSettingsWindow();
    [RelayCommand] private void OpenDemoPanel() => App.OpenDemoWindow();
}
