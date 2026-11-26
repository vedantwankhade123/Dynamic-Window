using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Microsoft.UI.Dispatching;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Newton.Core.Services;
using Newton.Windows.WindowManagement;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;

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
    private readonly IClipboardService _clipboardService;

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
    public ObservableCollection<ClipboardHistoryItem> ClipboardItems { get; } = new();

    public bool IsCompact => VisualState == NewtonndVisualState.Compact;
    public bool IsHoverPeek => VisualState == NewtonndVisualState.HoverPeek;
    public bool IsCompactOrHover => VisualState is NewtonndVisualState.Compact or NewtonndVisualState.HoverPeek;
    public bool IsExpanded => VisualState == NewtonndVisualState.Expanded;
    public bool IsDashboard => VisualState == NewtonndVisualState.Dashboard;

    public bool IsMusicPrimary => PrimaryActivity?.Type == ActivityType.Music;
    public bool IsTimerPrimary => PrimaryActivity?.Type is ActivityType.Timer or ActivityType.Pomodoro;
    public bool IsScreenRecordingPrimary => PrimaryActivity?.Type == ActivityType.ScreenRecording;
    public bool IsFocusAssistPrimary => PrimaryActivity?.Type == ActivityType.FocusAssist;
    public bool IsLowBatteryPrimary => PrimaryActivity?.Type == ActivityType.LowBattery;
    public bool IsBluetoothPrimary => PrimaryActivity?.Type == ActivityType.BluetoothAccessory;
    public bool IsIncomingCallPrimary => PrimaryActivity?.Type == ActivityType.IncomingCall;
    public bool IsClipboardPrimary => PrimaryActivity?.Type == ActivityType.Clipboard;
    public bool IsTransferOrBuildPrimary => PrimaryActivity?.Type is ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall or ActivityType.CustomDeveloper;
    public bool IsAiOrOtherPrimary => PrimaryActivity is not null && !IsMusicPrimary && !IsTimerPrimary && !IsScreenRecordingPrimary && !IsFocusAssistPrimary && !IsLowBatteryPrimary && !IsBluetoothPrimary && !IsIncomingCallPrimary && !IsClipboardPrimary && !IsTransferOrBuildPrimary;
    public bool IsNonMusicPrimary => PrimaryActivity is not null && PrimaryActivity.Type != ActivityType.Music;

    public string TimerDisplay => PrimaryActivity?.Metadata.TryGetValue("Remaining", out var rem) == true ? rem : "0:00";
    public string CompactTitle => PrimaryActivity?.Title ?? "Newton";
    public string CompactSubtitle => PrimaryActivity?.Subtitle ?? "Ready";
    public double CompactProgress => PrimaryActivity?.Progress ?? 0;
    public bool HasCompactProgress => Settings.ShowProgressInCompact && PrimaryActivity?.Progress is not null;
    public string? CompactArtworkPath => PrimaryActivity?.ImageUri ?? (IsMusicPrimary ? (CurrentTrack?.ArtworkPath ?? CurrentTrack?.PlatformIconPath) : null);
    public bool HasCompactArtwork => !string.IsNullOrEmpty(CompactArtworkPath);
    public bool HasTrackArtwork => !string.IsNullOrEmpty(CurrentTrack?.ArtworkPath ?? CurrentTrack?.PlatformIconPath);
    public string? PlatformIconPath => IsMusicPrimary ? CurrentTrack?.PlatformIconPath : null;
    public bool HasPlatformIcon => Settings.ShowPlatformBadge && !string.IsNullOrEmpty(PlatformIconPath);
    public string PlatformLabel => IsMusicPrimary ? (CurrentTrack?.SourceApp ?? "Media") : "Newton";

    [ObservableProperty]
    private string? _foregroundAppName;

    [ObservableProperty]
    private string? _foregroundAppIconPath;

    public Symbol PlayPauseSymbol => CurrentTrack?.IsPlaying == true ? Symbol.Pause : Symbol.Play;
    public Symbol TimerPlayPauseSymbol => PrimaryActivity?.Status == ActivityStatus.Active ? Symbol.Pause : Symbol.Play;
    public Symbol SpeakerSymbol => IsAudioMuted ? Symbol.SpeakerMute : Symbol.Speaker2;

    [ObservableProperty]
    private bool _isVolumeBarOpen;

    [ObservableProperty]
    private bool _isAudioMuted;

    [ObservableProperty]
    private Color _mediaAccentColor = Color.FromArgb(255, 136, 36, 42);

    [ObservableProperty]
    private bool _isDragOver;

    private string? _lastExtractedArtPath;
    private CancellationTokenSource? _toastCts;

    public Symbol CompactSymbol => PrimaryActivity?.Type switch
    {
        ActivityType.Music => Symbol.MusicNote2,
        ActivityType.Timer or ActivityType.Pomodoro => Symbol.Timer,
        ActivityType.LowBattery => Symbol.BatteryWarning,
        ActivityType.ScreenRecording => Symbol.Record,
        ActivityType.FocusAssist => Symbol.WeatherMoon,
        ActivityType.BluetoothAccessory => Symbol.Headphones,
        ActivityType.IncomingCall => Symbol.Call,
        ActivityType.Clipboard => Symbol.ClipboardPaste,
        ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall => Symbol.ArrowDownload,
        _ => Symbol.Sparkle
    };

    public MainNewtonndViewModel(
        IActivityService activityService,
        IMusicService musicService,
        INotificationService notificationService,
        ISystemMonitorService systemMonitorService,
        IAiService aiService,
        ISettingsService settingsService,
        ITimerService timerService,
        IClipboardService clipboardService)
    {
        _activityService = activityService;
        _musicService = musicService;
        _notificationService = notificationService;
        _systemMonitorService = systemMonitorService;
        _aiService = aiService;
        _settingsService = settingsService;
        _timerService = timerService;
        _clipboardService = clipboardService;

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
            OnPropertyChanged(nameof(PlayPauseSymbol));
            OnPropertyChanged(nameof(HasTrackArtwork));
            OnPropertyChanged(nameof(CompactArtworkPath));
            OnPropertyChanged(nameof(HasCompactArtwork));
            if (track.ArtworkPath != _lastExtractedArtPath)
            {
                _lastExtractedArtPath = track.ArtworkPath;
                _ = UpdateMediaAccentAsync(track.ArtworkPath);
            }
        });
        _notificationService.NotificationsChanged += (_, _) => RunOnUi(RefreshNotifications);
        _systemMonitorService.StatsUpdated += (_, stats) => RunOnUi(() =>
        {
            SystemStats = stats;
            OnPropertyChanged(nameof(SystemStats));
            _ = UpdateForegroundAppAsync();
        });
        _clipboardService.HistoryChanged += (_, _) => RunOnUi(RefreshClipboard);
        _clipboardService.ItemCaptured += (_, entry) => RunOnUi(() => ShowTackToast(entry));
        _ = _clipboardService.RefreshAsync();
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
        OnPropertyChanged(nameof(IsHoverPeek));
        OnPropertyChanged(nameof(IsCompactOrHover));
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
        OnPropertyChanged(nameof(IsClipboardPrimary));
        OnPropertyChanged(nameof(IsTransferOrBuildPrimary));
        OnPropertyChanged(nameof(IsAiOrOtherPrimary));
        OnPropertyChanged(nameof(IsNonMusicPrimary));
        OnPropertyChanged(nameof(TimerDisplay));
        OnPropertyChanged(nameof(TimerPlayPauseSymbol));
        OnPropertyChanged(nameof(PlayPauseSymbol));
        NotifyCompactProperties();
    }

    partial void OnIsAudioMutedChanged(bool value) => OnPropertyChanged(nameof(SpeakerSymbol));

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
        OnPropertyChanged(nameof(CompactArtworkPath));
        OnPropertyChanged(nameof(HasCompactArtwork));
        OnPropertyChanged(nameof(HasTrackArtwork));
        OnPropertyChanged(nameof(PlatformIconPath));
        OnPropertyChanged(nameof(HasPlatformIcon));
        OnPropertyChanged(nameof(PlatformLabel));
        OnPropertyChanged(nameof(CompactSymbol));
    }

    private async Task UpdateForegroundAppAsync()
    {
        var app = await ActiveAppIconHelper.GetForegroundAppAsync();
        if (app is null) return;
        RunOnUi(() =>
        {
            ForegroundAppName = app.DisplayName;
            ForegroundAppIconPath = app.IconPath;
            OnPropertyChanged(nameof(CompactArtworkPath));
            OnPropertyChanged(nameof(HasCompactArtwork));
            OnPropertyChanged(nameof(PlatformIconPath));
            OnPropertyChanged(nameof(HasPlatformIcon));
            OnPropertyChanged(nameof(PlatformLabel));
        });
    }

    private void RefreshClipboard()
    {
        ClipboardItems.Clear();
        foreach (var item in _clipboardService.Items) ClipboardItems.Add(item);
    }

    [RelayCommand] private void CopyClipboardText(string text) => _clipboardService.CopyText(text);
    [RelayCommand] private async Task CopyClipboardItemAsync(ClipboardHistoryItem? item) { if (item is not null) await _clipboardService.CopyItemAsync(item); }
    [RelayCommand] private void TogglePinClipboardItem(string id) => _clipboardService.TogglePin(id);
    [RelayCommand] private void RemoveClipboardItem(string id) => _clipboardService.RemoveItem(id);
    [RelayCommand] private void ClearClipboard() => _clipboardService.ClearHistory();
    [RelayCommand] private Task RefreshClipboardAsync() => _clipboardService.RefreshAsync();

    [RelayCommand]
    private void OpenTackShelf()
    {
        _toastCts?.Cancel();
        _activityService.AddOrUpdateActivity(new ActivityItem
        {
            Id = "newton-tack-shelf",
            Type = ActivityType.Clipboard,
            Title = "Tack Shelf",
            Subtitle = $"{ClipboardItems.Count} items",
            AccentHex = "#30D158",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            IsSimulated = false
        });
        _activityService.SetPrimaryActivity("newton-tack-shelf");
        VisualState = NewtonndVisualState.Expanded;
    }

    public void HandleDropFiles(IReadOnlyList<string> paths) => _clipboardService.AddDroppedFiles(paths);
    public void HandleDropText(string text) => _clipboardService.AddDroppedText(text);

    private void ShowTackToast(ClipboardHistoryItem entry)
    {
        if (!Settings.EnableClipboardTracking) return;

        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        var title = entry.Kind switch
        {
            ClipboardItemKind.Image => "Screenshot Tacked",
            ClipboardItemKind.File => "File Tacked",
            _ => "Copied to Newton"
        };

        _activityService.AddOrUpdateActivity(new ActivityItem
        {
            Id = "newton-tack-shelf",
            Type = ActivityType.Clipboard,
            Title = title,
            Subtitle = entry.Preview,
            ImageUri = entry.ImagePath,
            AccentHex = "#30D158",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Urgent,
            IsSimulated = false
        });
        _activityService.SetPrimaryActivity("newton-tack-shelf");

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2600, token);
                if (!token.IsCancellationRequested && VisualState == NewtonndVisualState.Compact)
                {
                    RunOnUi(() => _activityService.RemoveActivity("newton-tack-shelf"));
                }
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    private async Task UpdateMediaAccentAsync(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            RunOnUi(() => MediaAccentColor = Color.FromArgb(255, 136, 36, 42));
            return;
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var transform = new BitmapTransform { ScaledWidth = 10, ScaledHeight = 10 };
            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);

            var bytes = pixels.DetachPixelData();
            double rSum = 0, gSum = 0, bSum = 0, weightSum = 0;
            for (int i = 0; i + 3 < bytes.Length; i += 4)
            {
                byte b = bytes[i], g = bytes[i + 1], r = bytes[i + 2];
                int max = Math.Max(r, Math.Max(g, b));
                int min = Math.Min(r, Math.Min(g, b));
                if (max < 45 || min > 225) continue; // ignore near-black & near-white

                double saturationWeight = 1.0 + (max - min) / 64.0;
                rSum += r * saturationWeight;
                gSum += g * saturationWeight;
                bSum += b * saturationWeight;
                weightSum += saturationWeight;
            }

            if (weightSum > 0)
            {
                byte avgR = (byte)Math.Clamp(rSum / weightSum, 35, 230);
                byte avgG = (byte)Math.Clamp(gSum / weightSum, 30, 210);
                byte avgB = (byte)Math.Clamp(bSum / weightSum, 35, 230);
                RunOnUi(() => MediaAccentColor = Color.FromArgb(255, avgR, avgG, avgB));
            }
        }
        catch
        {
            // Keep previous color on transient decode errors
        }
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

    [RelayCommand] private void TogglePlayPause()
    {
        _musicService.TogglePlayPause();
        if (CurrentTrack is not null)
        {
            CurrentTrack.IsPlaying = !CurrentTrack.IsPlaying;
            OnPropertyChanged(nameof(PlayPauseSymbol));
        }
    }
    [RelayCommand] private void NextTrack() => _musicService.NextTrack();
    [RelayCommand] private void PreviousTrack() => _musicService.PreviousTrack();
    [RelayCommand] private void SeekForward10()
    {
        if (CurrentTrack is null) return;
        var next = CurrentTrack.Position + TimeSpan.FromSeconds(10);
        if (CurrentTrack.Duration > TimeSpan.Zero && next > CurrentTrack.Duration) next = CurrentTrack.Duration;
        _musicService.Seek(next);
    }
    [RelayCommand] private void SeekBack10()
    {
        if (CurrentTrack is null) return;
        var prev = CurrentTrack.Position - TimeSpan.FromSeconds(10);
        if (prev < TimeSpan.Zero) prev = TimeSpan.Zero;
        _musicService.Seek(prev);
    }
    [RelayCommand] private void AddTimer1Min() => _timerService.StartTimer(Math.Max(60, _timerService.RemainingSeconds + 60), "Focus Timer");

    [RelayCommand] private void ToggleVolumeBar() => IsVolumeBarOpen = !IsVolumeBarOpen;
    [RelayCommand] private void VolumeUp() { IsAudioMuted = false; SendMediaKey(0xAF); }
    [RelayCommand] private void VolumeDown() { IsAudioMuted = false; SendMediaKey(0xAE); }
    [RelayCommand] private void ToggleMute() { IsAudioMuted = !IsAudioMuted; SendMediaKey(0xAD); }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private static void SendMediaKey(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, 2, UIntPtr.Zero);
    }

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
