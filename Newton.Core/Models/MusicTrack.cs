namespace Newton.Core.Models;

public sealed class MusicTrack
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public TimeSpan Position { get; set; }
    public bool IsPlaying { get; set; }
    public double Volume { get; set; } = 72;
    public string SourceApp { get; set; } = "Spotify";
    public string? ArtworkPath { get; set; }
    public string? PlatformIconPath { get; set; }
    public string CoverGradientStart { get; set; } = "#312E81";
    public string CoverGradientEnd { get; set; } = "#0F172A";
    public bool IsSimulated { get; init; } = true;
}
