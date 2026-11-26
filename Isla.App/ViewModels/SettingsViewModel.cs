using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Isla.Core.Interfaces;
using Isla.Core.Models;

namespace Isla.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _selectedSection = "General";

    [ObservableProperty]
    private IslaSettings _settings;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public IReadOnlyList<string> Languages { get; } = new[] { "English (US)", "English (UK)", "Deutsch", "Français", "日本語" };
    public IReadOnlyList<string> AiProviders { get; } = new[] { "Ollama (Local)", "OpenAI-Compatible Local", "Gemini API (Cloud)", "Disabled" };
    public IReadOnlyList<string> AccentSwatches { get; } = new[] { "#60A5FA", "#818CF8", "#34D399", "#F59E0B", "#FB7185", "#E2E8F0" };

    public int ThemeIndex
    {
        get => (int)Settings.ThemeMode;
        set { Settings.ThemeMode = (IslaThemeMode)value; OnPropertyChanged(); _ = SaveAsync(); }
    }

    public int PositionIndex
    {
        get => (int)Settings.PositionMode;
        set { Settings.PositionMode = (IslaPositionMode)value; OnPropertyChanged(); _ = SaveAsync(); }
    }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Current;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        await _settingsService.SaveAsync(Settings);
        StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    public async Task ResetDefaultsAsync()
    {
        await _settingsService.ResetToDefaultsAsync();
        Settings = _settingsService.Current;
        OnPropertyChanged(nameof(ThemeIndex));
        OnPropertyChanged(nameof(PositionIndex));
        StatusMessage = "Restored default settings";
    }
}
