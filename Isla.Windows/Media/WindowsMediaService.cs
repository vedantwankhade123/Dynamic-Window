using Isla.Core.Interfaces;
using Isla.Core.Models;
using Isla.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Media.Control;

namespace Isla.Windows.Media;

public sealed class WindowsMediaService : IMusicService, IDisposable
{
    private readonly ActivityManager _activityManager;
    private readonly ILogger<WindowsMediaService> _logger;
    private readonly Timer _pollTimer;
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public MusicTrack CurrentTrack { get; private set; } = new()
    {
        Title = "No Media Playing",
        Artist = "Play audio in any Windows app",
        SourceApp = "Windows Media",
        IsPlaying = false,
        IsSimulated = false
    };

    public bool IsPlaying => CurrentTrack.IsPlaying;
    public event EventHandler<MusicTrack>? TrackUpdated;

    public WindowsMediaService(ActivityManager activityManager, ILogger<WindowsMediaService> logger)
    {
        _activityManager = activityManager;
        _logger = logger;
        _pollTimer = new Timer(_ => _ = RefreshTimelineAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += (_, _) => _ = AttachCurrentSessionAsync();
            await AttachCurrentSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Windows GlobalSystemMediaTransportControlsSessionManager unavailable.");
        }
    }

    private async Task AttachCurrentSessionAsync()
    {
        if (_sessionManager is null) return;

        try
        {
            if (_currentSession is not null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = _sessionManager.GetCurrentSession();
            if (_currentSession is null)
            {
                CurrentTrack = new MusicTrack
                {
                    Title = "No Media Playing",
                    Artist = "Play audio in any Windows app",
                    SourceApp = "Windows Media",
                    IsPlaying = false,
                    IsSimulated = false
                };
                _activityManager.RemoveActivity("win-media");
                TrackUpdated?.Invoke(this, CurrentTrack);
                return;
            }

            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            await RefreshMediaAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to attach Windows media session.");
        }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        => _ = RefreshMediaAsync();

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        => _ = RefreshMediaAsync();

    private async Task RefreshMediaAsync()
    {
        var session = _currentSession;
        if (session is null) return;

        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            var isPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var source = FormatAppName(session.SourceAppUserModelId);
            var duration = timeline.EndTime > timeline.StartTime ? timeline.EndTime - timeline.StartTime : TimeSpan.Zero;
            var position = timeline.Position >= timeline.StartTime ? timeline.Position - timeline.StartTime : TimeSpan.Zero;

            CurrentTrack = new MusicTrack
            {
                Title = string.IsNullOrWhiteSpace(props.Title) ? "Active Media" : props.Title,
                Artist = string.IsNullOrWhiteSpace(props.Artist) ? source : props.Artist,
                Album = props.AlbumTitle ?? string.Empty,
                Duration = duration,
                Position = position,
                IsPlaying = isPlaying,
                SourceApp = source,
                IsSimulated = false
            };

            SyncActivity();
            TrackUpdated?.Invoke(this, CurrentTrack);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading Windows media properties.");
        }
    }

    private Task RefreshTimelineAsync()
    {
        var session = _currentSession;
        if (session is null || !CurrentTrack.IsPlaying) return Task.CompletedTask;

        try
        {
            var timeline = session.GetTimelineProperties();
            var duration = timeline.EndTime > timeline.StartTime ? timeline.EndTime - timeline.StartTime : CurrentTrack.Duration;
            var position = timeline.Position >= timeline.StartTime ? timeline.Position - timeline.StartTime : CurrentTrack.Position;
            CurrentTrack.Duration = duration;
            CurrentTrack.Position = position;
            SyncActivity();
            TrackUpdated?.Invoke(this, CurrentTrack);
        }
        catch
        {
            // Ignore transient COM read issues
        }

        return Task.CompletedTask;
    }

    private void SyncActivity()
    {
        double? progress = CurrentTrack.Duration.TotalSeconds > 0
            ? Math.Clamp(CurrentTrack.Position.TotalSeconds / CurrentTrack.Duration.TotalSeconds * 100.0, 0, 100)
            : null;

        _activityManager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "win-media",
            Type = ActivityType.Music,
            Title = CurrentTrack.Title,
            Subtitle = $"{CurrentTrack.Artist} • {CurrentTrack.SourceApp}",
            IconGlyph = "\uE8D6",
            AccentHex = "#60A5FA",
            Progress = progress,
            Status = CurrentTrack.IsPlaying ? ActivityStatus.Active : ActivityStatus.Paused,
            Priority = ActivityPriority.Normal,
            IsSimulated = false,
            Actions =
            {
                new("music-prev", "Previous", "\uE892"),
                new("music-toggle", CurrentTrack.IsPlaying ? "Pause" : "Play", CurrentTrack.IsPlaying ? "\uE769" : "\uE768", true),
                new("music-next", "Next", "\uE893")
            }
        });
    }

    private static string FormatAppName(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId)) return "Windows Media";
        var clean = appId.Split('!').FirstOrDefault() ?? appId;
        clean = clean.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        var lastDot = clean.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < clean.Length - 1) clean = clean[(lastDot + 1)..];
        return clean;
    }

    public void TogglePlayPause() => _ = _currentSession?.TryTogglePlayPauseAsync();
    public void Play() => _ = _currentSession?.TryPlayAsync();
    public void Pause() => _ = _currentSession?.TryPauseAsync();
    public void NextTrack() => _ = _currentSession?.TrySkipNextAsync();
    public void PreviousTrack() => _ = _currentSession?.TrySkipPreviousAsync();
    public void Seek(TimeSpan position) => _ = _currentSession?.TryChangePlaybackPositionAsync(position.Ticks);
    public void SetVolume(double volume) => CurrentTrack.Volume = Math.Clamp(volume, 0, 100);

    public void Dispose() => _pollTimer.Dispose();
}
