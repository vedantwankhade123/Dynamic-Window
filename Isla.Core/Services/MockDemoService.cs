using Isla.Core.Interfaces;
using Isla.Core.Models;

namespace Isla.Core.Services;

public sealed class MockDemoService :
    IMusicService,
    INotificationService,
    ISystemMonitorService,
    IAiService,
    IDemoController,
    IDisposable
{
    private readonly ActivityManager _activityManager;
    private readonly Timer _tickTimer;
    private readonly Random _random = new();

    private readonly List<MusicTrack> _playlist;
    private int _trackIndex;

    private readonly List<NotificationItem> _notifications = new();
    private readonly List<AiMessage> _messages = new();
    private readonly List<AiModelOption> _models;

    private int _timerRemainingSeconds = 90;
    private int _timerTotalSeconds = 90;
    private bool _timerRunning;

    private double _downloadProgress;
    private bool _downloadRunning;

    private double _aiTaskProgress;
    private bool _aiTaskRunning;

    private double _cpuSpikeBoost;

    public bool IsDemoMode => true;
    public MusicTrack CurrentTrack => _playlist[_trackIndex];
    public bool IsPlaying => CurrentTrack.IsPlaying;
    public IReadOnlyList<NotificationItem> Notifications => _notifications.AsReadOnly();
    public SystemStats CurrentStats { get; }
    public IReadOnlyList<AiModelOption> AvailableModels => _models.AsReadOnly();
    public AiModelOption SelectedModel { get; set; }
    public IReadOnlyList<AiMessage> Messages => _messages.AsReadOnly();

    public event EventHandler<MusicTrack>? TrackUpdated;
    public event EventHandler<NotificationItem>? NotificationReceived;
    public event EventHandler? NotificationsChanged;
    public event EventHandler<SystemStats>? StatsUpdated;
    public event EventHandler? MessagesUpdated;

    public MockDemoService(ActivityManager activityManager)
    {
        _activityManager = activityManager;
        _activityManager.ActionRequested += OnActivityActionRequested;

        _playlist = new List<MusicTrack>
        {
            new()
            {
                Id = "track-1",
                Title = "Midnight City (M83 Rework)",
                Artist = "M83",
                Album = "Hurry Up, We're Dreaming",
                Duration = TimeSpan.FromSeconds(243),
                Position = TimeSpan.FromSeconds(78),
                IsPlaying = true,
                Volume = 72,
                SourceApp = "Spotify",
                CoverGradientStart = "#4F46E5",
                CoverGradientEnd = "#0F172A"
            },
            new()
            {
                Id = "track-2",
                Title = "Resonance",
                Artist = "HOME",
                Album = "Odyssey",
                Duration = TimeSpan.FromSeconds(212),
                Position = TimeSpan.FromSeconds(44),
                IsPlaying = false,
                Volume = 72,
                SourceApp = "Apple Music",
                CoverGradientStart = "#EC4899",
                CoverGradientEnd = "#1E1B4B"
            },
            new()
            {
                Id = "track-3",
                Title = "Intro",
                Artist = "The xx",
                Album = "xx",
                Duration = TimeSpan.FromSeconds(128),
                Position = TimeSpan.FromSeconds(19),
                IsPlaying = false,
                Volume = 72,
                SourceApp = "Spotify",
                CoverGradientStart = "#0EA5E9",
                CoverGradientEnd = "#09090B"
            }
        };

        _models = new List<AiModelOption>
        {
            new("llama3.2:latest", "Llama 3.2 8B", "Ollama (Local)", true, "http://localhost:11434"),
            new("qwen2.5-coder:14b", "Qwen 2.5 Coder 14B", "Ollama (Local)", true, "http://localhost:11434"),
            new("gemini-2.5-flash", "Gemini 2.5 Flash", "Gemini API (Cloud)", false, "https://generativelanguage.googleapis.com")
        };
        SelectedModel = _models[0];

        CurrentStats = new SystemStats
        {
            CpuUsagePercent = 24,
            RamUsagePercent = 48,
            RamUsedGb = 15.4,
            RamTotalGb = 32.0,
            GpuUsagePercent = 18,
            GpuTempCelsius = 51,
            BatteryPercent = 86,
            IsCharging = true,
            NetworkDownMbps = 42.6,
            NetworkUpMbps = 6.4,
            CpuHistory = { 18, 22, 20, 26, 24, 21, 25, 24 },
            GpuHistory = { 14, 16, 15, 21, 19, 17, 18, 18 }
        };

        SeedInitialData();
        _tickTimer = new Timer(OnTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void SeedInitialData()
    {
        SyncMusicActivity();

        _notifications.AddRange(new[]
        {
            new NotificationItem
            {
                AppName = "Slack",
                IconGlyph = "\uE8BD",
                AccentHex = "#38BDF8",
                Title = "Design Review at 3:30 PM",
                Message = "Maya: Updated the Windows 11 glass tokens in Figma.",
                Timestamp = DateTimeOffset.Now.AddMinutes(-3),
                Actions = { new("reply", "Reply", true), new("dismiss", "Dismiss") }
            },
            new NotificationItem
            {
                AppName = "Edge",
                IconGlyph = "\uE896",
                AccentHex = "#34D399",
                Title = "Download Completed",
                Message = "WinAppSDK-1.6-x64.msix (148 MB) saved to Downloads.",
                Timestamp = DateTimeOffset.Now.AddMinutes(-11),
                Actions = { new("open", "Open Folder", true) }
            },
            new NotificationItem
            {
                AppName = "Windows Update",
                IconGlyph = "\uE777",
                AccentHex = "#A78BFA",
                Title = "System Update Ready",
                Message = "Cumulative Update for Windows 11 (KB5044284) ready to install.",
                Timestamp = DateTimeOffset.Now.AddMinutes(-27)
            }
        });

        _messages.AddRange(new[]
        {
            new AiMessage
            {
                Role = AiMessageRole.User,
                Content = "Summarize my active desktop tasks right now.",
                Timestamp = DateTimeOffset.Now.AddMinutes(-2),
                ModelName = SelectedModel.DisplayName,
                ProviderName = SelectedModel.ProviderName,
                IsLocal = SelectedModel.IsLocal
            },
            new AiMessage
            {
                Role = AiMessageRole.Assistant,
                Content = "You have Midnight City playing on Spotify, 2 unread notifications from Slack and Edge, and system load is nominal (CPU 24%, RAM 15.4/32 GB). [Simulated AI Response]",
                Timestamp = DateTimeOffset.Now.AddMinutes(-2),
                ModelName = SelectedModel.DisplayName,
                ProviderName = SelectedModel.ProviderName,
                IsLocal = SelectedModel.IsLocal
            }
        });
    }

    private void OnTick(object? state)
    {
        // 1. Music playback tick
        if (CurrentTrack.IsPlaying)
        {
            CurrentTrack.Position += TimeSpan.FromSeconds(1);
            if (CurrentTrack.Position >= CurrentTrack.Duration)
            {
                NextTrack();
            }
            else
            {
                SyncMusicActivity();
                TrackUpdated?.Invoke(this, CurrentTrack);
            }
        }

        // 2. Download simulation tick
        if (_downloadRunning)
        {
            _downloadProgress = Math.Min(100, _downloadProgress + 7.5);
            var remainingSec = Math.Max(0, (int)Math.Ceiling((100 - _downloadProgress) / 7.5));
            var speed = (64.2 + _random.NextDouble() * 14.0).ToString("F1");

            var dl = new ActivityItem
            {
                Id = "demo-download",
                Type = ActivityType.Download,
                Title = "WindowsAppSDK-1.6.zip",
                Subtitle = _downloadProgress >= 100 ? "Download complete • 482 MB" : $"{speed} MB/s • {remainingSec}s remaining",
                IconGlyph = "\uE896",
                AccentHex = "#34D399",
                Progress = _downloadProgress,
                Status = _downloadProgress >= 100 ? ActivityStatus.Completed : ActivityStatus.Active,
                Priority = ActivityPriority.High,
                Metadata = { ["Speed"] = $"{speed} MB/s", ["Eta"] = $"{remainingSec}s", ["Size"] = "482 MB" },
                Actions =
                {
                    new("pause-dl", _downloadProgress >= 100 ? "Open" : "Pause", "\uE769", true),
                    new("cancel-dl", "Dismiss", "\uE711", false, true)
                }
            };
            _activityManager.AddOrUpdateActivity(dl);

            if (_downloadProgress >= 100)
            {
                _downloadRunning = false;
                SimulateNotification("download");
            }
        }

        // 3. Timer tick
        if (_timerRunning)
        {
            _timerRemainingSeconds = Math.Max(0, _timerRemainingSeconds - 1);
            var ts = TimeSpan.FromSeconds(_timerRemainingSeconds);
            var progress = _timerTotalSeconds > 0
                ? (double)(_timerTotalSeconds - _timerRemainingSeconds) / _timerTotalSeconds * 100.0
                : 100;

            _activityManager.AddOrUpdateActivity(new ActivityItem
            {
                Id = "demo-timer",
                Type = ActivityType.Timer,
                Title = "Focus Timer",
                Subtitle = _timerRemainingSeconds == 0 ? "Timer completed!" : $"{ts:mm\\:ss} remaining",
                IconGlyph = "\uE916",
                AccentHex = "#F59E0B",
                Progress = progress,
                Status = _timerRemainingSeconds == 0 ? ActivityStatus.Completed : ActivityStatus.Active,
                Priority = ActivityPriority.High,
                Metadata = { ["Remaining"] = $"{ts:mm\\:ss}" },
                Actions =
                {
                    new("timer-toggle", _timerRunning ? "Pause" : "Resume", "\uE769", true),
                    new("timer-reset", "Reset", "\uE72C"),
                    new("timer-stop", "Stop", "\uE71A", false, true)
                }
            });

            if (_timerRemainingSeconds == 0)
            {
                _timerRunning = false;
                SimulateNotification("timer");
            }
        }

        // 4. AI generation task tick
        if (_aiTaskRunning)
        {
            _aiTaskProgress = Math.Min(100, _aiTaskProgress + 12);
            _activityManager.AddOrUpdateActivity(new ActivityItem
            {
                Id = "demo-ai",
                Type = ActivityType.AiGeneration,
                Title = "Generating Summary",
                Subtitle = _aiTaskProgress >= 100
                    ? $"{SelectedModel.DisplayName} • Complete"
                    : $"{SelectedModel.DisplayName} • Streaming tokens...",
                IconGlyph = "\uE99A",
                AccentHex = "#A78BFA",
                Progress = _aiTaskProgress,
                Status = _aiTaskProgress >= 100 ? ActivityStatus.Completed : ActivityStatus.Active,
                Priority = ActivityPriority.High,
                Metadata = { ["Model"] = SelectedModel.DisplayName, ["Provider"] = SelectedModel.ProviderName },
                Actions = { new("ai-cancel", _aiTaskProgress >= 100 ? "Done" : "Cancel", "\uE711", false, _aiTaskProgress < 100) }
            });

            if (_aiTaskProgress >= 100)
            {
                _aiTaskRunning = false;
            }
        }

        // 5. System stats fluctuation
        _cpuSpikeBoost = Math.Max(0, _cpuSpikeBoost - 8);
        var nextCpu = Math.Clamp(18 + _random.Next(-4, 7) + _cpuSpikeBoost, 4, 98);
        var nextGpu = Math.Clamp(15 + _random.Next(-3, 6) + _cpuSpikeBoost * 0.6, 2, 95);
        CurrentStats.CpuUsagePercent = Math.Round(nextCpu, 0);
        CurrentStats.GpuUsagePercent = Math.Round(nextGpu, 0);
        CurrentStats.GpuTempCelsius = Math.Round(48 + nextGpu * 0.35, 0);
        CurrentStats.RamUsagePercent = Math.Round(47 + _random.NextDouble() * 2.5, 1);
        CurrentStats.RamUsedGb = Math.Round(CurrentStats.RamTotalGb * CurrentStats.RamUsagePercent / 100.0, 1);
        CurrentStats.NetworkDownMbps = Math.Round((_downloadRunning ? 68 : 12) + _random.NextDouble() * 14, 1);
        CurrentStats.NetworkUpMbps = Math.Round(2.5 + _random.NextDouble() * 4.5, 1);

        CurrentStats.CpuHistory.Add(CurrentStats.CpuUsagePercent);
        if (CurrentStats.CpuHistory.Count > 24) CurrentStats.CpuHistory.RemoveAt(0);
        CurrentStats.GpuHistory.Add(CurrentStats.GpuUsagePercent);
        if (CurrentStats.GpuHistory.Count > 24) CurrentStats.GpuHistory.RemoveAt(0);

        StatsUpdated?.Invoke(this, CurrentStats);
    }

    private void SyncMusicActivity()
    {
        var track = CurrentTrack;
        var pct = track.Duration.TotalSeconds > 0
            ? track.Position.TotalSeconds / track.Duration.TotalSeconds * 100.0
            : 0;

        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-music",
            Type = ActivityType.Music,
            Title = track.Title,
            Subtitle = $"{track.Artist} • {track.SourceApp}",
            IconGlyph = "\uE8D6",
            AccentHex = "#60A5FA",
            Progress = pct,
            Status = track.IsPlaying ? ActivityStatus.Active : ActivityStatus.Paused,
            Priority = ActivityPriority.Normal,
            Metadata =
            {
                ["Artist"] = track.Artist,
                ["Album"] = track.Album,
                ["Source"] = track.SourceApp
            },
            Actions =
            {
                new("music-prev", "Previous", "\uE892"),
                new("music-toggle", track.IsPlaying ? "Pause" : "Play", track.IsPlaying ? "\uE769" : "\uE768", true),
                new("music-next", "Next", "\uE893")
            }
        });
    }

    private void OnActivityActionRequested(object? sender, (string ActivityId, string ActionId) e)
    {
        switch (e.ActionId)
        {
            case "music-toggle": TogglePlayPause(); break;
            case "music-prev": PreviousTrack(); break;
            case "music-next": NextTrack(); break;
            case "pause-dl":
                _downloadRunning = !_downloadRunning && _downloadProgress < 100;
                break;
            case "cancel-dl":
                _downloadRunning = false;
                _activityManager.RemoveActivity("demo-download");
                break;
            case "timer-toggle":
                _timerRunning = !_timerRunning && _timerRemainingSeconds > 0;
                break;
            case "timer-reset":
                _timerRemainingSeconds = _timerTotalSeconds;
                _timerRunning = true;
                break;
            case "timer-stop":
                _timerRunning = false;
                _activityManager.RemoveActivity("demo-timer");
                break;
            case "ai-cancel":
                _aiTaskRunning = false;
                _activityManager.RemoveActivity("demo-ai");
                break;
            default:
                _activityManager.RemoveActivity(e.ActivityId);
                break;
        }
    }

    public void TogglePlayPause()
    {
        CurrentTrack.IsPlaying = !CurrentTrack.IsPlaying;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void Play()
    {
        CurrentTrack.IsPlaying = true;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void Pause()
    {
        CurrentTrack.IsPlaying = false;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void NextTrack()
    {
        var wasPlaying = CurrentTrack.IsPlaying;
        CurrentTrack.IsPlaying = false;
        _trackIndex = (_trackIndex + 1) % _playlist.Count;
        CurrentTrack.Position = TimeSpan.Zero;
        CurrentTrack.IsPlaying = wasPlaying || true;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void PreviousTrack()
    {
        var wasPlaying = CurrentTrack.IsPlaying;
        CurrentTrack.IsPlaying = false;
        _trackIndex = (_trackIndex - 1 + _playlist.Count) % _playlist.Count;
        CurrentTrack.Position = TimeSpan.Zero;
        CurrentTrack.IsPlaying = wasPlaying || true;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void Seek(TimeSpan position)
    {
        CurrentTrack.Position = position;
        SyncMusicActivity();
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void SetVolume(double volume)
    {
        CurrentTrack.Volume = Math.Clamp(volume, 0, 100);
        TrackUpdated?.Invoke(this, CurrentTrack);
    }

    public void PushNotification(NotificationItem notification)
    {
        _notifications.Insert(0, notification);
        if (_notifications.Count > 20) _notifications.RemoveAt(_notifications.Count - 1);
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

    public void TriggerSpike() => _cpuSpikeBoost = 62;

    public async Task SendMessageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;

        _messages.Add(new AiMessage
        {
            Role = AiMessageRole.User,
            Content = prompt.Trim(),
            ModelName = SelectedModel.DisplayName,
            ProviderName = SelectedModel.ProviderName,
            IsLocal = SelectedModel.IsLocal
        });
        MessagesUpdated?.Invoke(this, EventArgs.Empty);

        var reply = new AiMessage
        {
            Role = AiMessageRole.Assistant,
            Content = string.Empty,
            ModelName = SelectedModel.DisplayName,
            ProviderName = SelectedModel.ProviderName,
            IsLocal = SelectedModel.IsLocal,
            IsStreaming = true
        };
        _messages.Add(reply);
        MessagesUpdated?.Invoke(this, EventArgs.Empty);

        var responseText = $"[{SelectedModel.DisplayName}] Processed \"{prompt.Trim()}\". In Phase 7 this will stream directly from {SelectedModel.ProviderName} ({SelectedModel.Endpoint}).";
        foreach (var word in responseText.Split(' '))
        {
            if (cancellationToken.IsCancellationRequested) break;
            reply.Content += (reply.Content.Length == 0 ? "" : " ") + word;
            MessagesUpdated?.Invoke(this, EventArgs.Empty);
            await Task.Delay(35, cancellationToken);
        }

        reply.IsStreaming = false;
        MessagesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void ClearHistory()
    {
        _messages.Clear();
        MessagesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void StartMusicDemo()
    {
        Play();
        _activityManager.SetPrimaryActivity("demo-music");
    }

    public void ToggleMusicDemo() => TogglePlayPause();

    public void StartDownloadDemo()
    {
        _downloadProgress = 12;
        _downloadRunning = true;
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-download",
            Type = ActivityType.Download,
            Title = "WindowsAppSDK-1.6.zip",
            Subtitle = "68.4 MB/s • 12s remaining",
            IconGlyph = "\uE896",
            AccentHex = "#34D399",
            Progress = _downloadProgress,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions = { new("pause-dl", "Pause", "\uE769", true), new("cancel-dl", "Dismiss", "\uE711", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-download");
    }

    public void StartTimerDemo(int seconds = 90)
    {
        _timerTotalSeconds = seconds;
        _timerRemainingSeconds = seconds;
        _timerRunning = true;
        var ts = TimeSpan.FromSeconds(seconds);
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-timer",
            Type = ActivityType.Timer,
            Title = "Focus Timer",
            Subtitle = $"{ts:mm\\:ss} remaining",
            IconGlyph = "\uE916",
            AccentHex = "#F59E0B",
            Progress = 0,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions =
            {
                new("timer-toggle", "Pause", "\uE769", true),
                new("timer-reset", "Reset", "\uE72C"),
                new("timer-stop", "Stop", "\uE71A", false, true)
            }
        });
        _activityManager.SetPrimaryActivity("demo-timer");
    }

    public void CompleteTimerDemo()
    {
        _timerRemainingSeconds = 0;
        _timerRunning = false;
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-timer",
            Type = ActivityType.Timer,
            Title = "Focus Timer",
            Subtitle = "Timer completed!",
            IconGlyph = "\uE916",
            AccentHex = "#F59E0B",
            Progress = 100,
            Status = ActivityStatus.Completed,
            Priority = ActivityPriority.Urgent,
            Actions = { new("timer-reset", "Restart", "\uE72C", true), new("timer-stop", "Dismiss", "\uE711") }
        });
        _activityManager.SetPrimaryActivity("demo-timer");
        SimulateNotification("timer");
    }

    public void StartPomodoroDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-pomodoro",
            Type = ActivityType.Pomodoro,
            Title = "Pomodoro • Deep Work",
            Subtitle = "24:15 • Session 2 of 4",
            IconGlyph = "\uE823",
            AccentHex = "#F43F5E",
            Progress = 22,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions = { new("pomo-skip", "Skip Break", "\uE893"), new("pomo-stop", "End", "\uE711", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-pomodoro");
    }

    public void StartAiTaskDemo()
    {
        _aiTaskProgress = 10;
        _aiTaskRunning = true;
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-ai",
            Type = ActivityType.AiGeneration,
            Title = "Generating Summary",
            Subtitle = $"{SelectedModel.DisplayName} • Streaming tokens...",
            IconGlyph = "\uE99A",
            AccentHex = "#A78BFA",
            Progress = _aiTaskProgress,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions = { new("ai-cancel", "Cancel", "\uE711", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-ai");
    }

    public void StartFileOperationDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-fileop",
            Type = ActivityType.FileOperation,
            Title = "Moving Project Assets",
            Subtitle = "1,240 of 1,800 items • 312 MB/s",
            IconGlyph = "\uE8B7",
            AccentHex = "#38BDF8",
            Progress = 69,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Normal,
            Actions = { new("file-pause", "Pause", "\uE769"), new("file-cancel", "Cancel", "\uE711", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-fileop");
    }

    public void StartAppInstallDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-install",
            Type = ActivityType.AppInstall,
            Title = "Installing Visual Studio Code",
            Subtitle = "Extracting package • v1.96.0",
            IconGlyph = "\uE74C",
            AccentHex = "#60A5FA",
            Progress = 74,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Normal,
            Actions = { new("inst-hide", "Hide", "\uE711") }
        });
        _activityManager.SetPrimaryActivity("demo-install");
    }

    public void StartCustomDevDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-dev",
            Type = ActivityType.CustomDeveloper,
            Title = "Building project",
            Subtitle = "Vite production build • 65%",
            IconGlyph = "\uE943",
            AccentHex = "#22D3EE",
            Progress = 65,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions = { new("open-terminal", "View logs", "\uE756", true), new("cancel-build", "Cancel", "\uE711", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-dev");
    }

    public void StartScreenRecordingDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-recording",
            Type = ActivityType.ScreenRecording,
            Title = "Screen Recording",
            Subtitle = "0:42",
            IconGlyph = "\uE7C8",
            AccentHex = "#EF4444",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Urgent,
            Actions = { new("stop-rec", "Stop", "\uE71A", false, true) }
        });
        _activityManager.SetPrimaryActivity("demo-recording");
    }

    public void StartFocusAssistDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-focus",
            Type = ActivityType.FocusAssist,
            Title = "Focus Assist",
            Subtitle = "On",
            IconGlyph = "\uE708",
            AccentHex = "#818CF8",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            Actions = { new("toggle-focus", "Off", "\uE708") }
        });
        _activityManager.SetPrimaryActivity("demo-focus");
    }

    public void StartLowBatteryDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-battery",
            Type = ActivityType.LowBattery,
            Title = "Low Battery",
            Subtitle = "14%",
            IconGlyph = "\uEBAA",
            AccentHex = "#F43F5E",
            Progress = 14,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Urgent
        });
        _activityManager.SetPrimaryActivity("demo-battery");
    }

    public void StartBluetoothDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-bt",
            Type = ActivityType.BluetoothAccessory,
            Title = "WH-1000XM5",
            Subtitle = "Connected",
            IconGlyph = "\uE7F6",
            AccentHex = "#34D399",
            Progress = 85,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High
        });
        _activityManager.SetPrimaryActivity("demo-bt");
    }

    public void StartIncomingCallDemo()
    {
        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "demo-call",
            Type = ActivityType.IncomingCall,
            Title = "Tania Castillo",
            Subtitle = "Microsoft Teams",
            IconGlyph = "\uE717",
            AccentHex = "#22C55E",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Urgent,
            Actions = { new("decline-call", "Decline", "\uE778", false, true), new("accept-call", "Accept", "\uE717", true) }
        });
        _activityManager.SetPrimaryActivity("demo-call");
    }

    public void SimulateNotification(string kind = "message")
    {
        var notif = kind switch
        {
            "download" => new NotificationItem
            {
                AppName = "Isla Transfers",
                IconGlyph = "\uE896",
                AccentHex = "#34D399",
                Title = "Download Completed",
                Message = "WindowsAppSDK-1.6.zip (482 MB) is ready.",
                Actions = { new("open", "Open File", true) }
            },
            "battery" => new NotificationItem
            {
                AppName = "Power",
                IconGlyph = "\uEBAA",
                AccentHex = "#F59E0B",
                Title = "Battery Low (14%)",
                Message = "Connect your charger or enable Battery Saver.",
                Actions = { new("saver", "Battery Saver", true) }
            },
            "timer" => new NotificationItem
            {
                AppName = "Isla Timer",
                IconGlyph = "\uE916",
                AccentHex = "#F59E0B",
                Title = "Focus Timer Completed",
                Message = "Your focus session has ended. Take a short break.",
                Actions = { new("restart", "Restart", true) }
            },
            _ => new NotificationItem
            {
                AppName = "Teams",
                IconGlyph = "\uE8BD",
                AccentHex = "#818CF8",
                Title = "Alex Chen",
                Message = "Pushed the WinUI 3 animation polish PR — looks super clean!",
                Actions = { new("reply", "Quick Reply", true), new("mark", "Mark Read") }
            }
        };

        PushNotification(notif);

        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = $"notif-{notif.Id}",
            Type = ActivityType.Notification,
            Title = notif.Title,
            Subtitle = $"{notif.AppName} • {notif.Message}",
            IconGlyph = notif.IconGlyph,
            AccentHex = notif.AccentHex,
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.Urgent,
            Actions = { new("notif-ack", "Dismiss", "\uE711", true) }
        });
    }

    public void TriggerSystemSpike() => TriggerSpike();

    public void ResetDemoData()
    {
        _downloadRunning = false;
        _timerRunning = false;
        _aiTaskRunning = false;
        foreach (var act in _activityManager.Activities.Where(a => a.Id != "demo-music").ToList())
        {
            _activityManager.RemoveActivity(act.Id);
        }
        StartMusicDemo();
    }

    public void Dispose() => _tickTimer.Dispose();
}
