namespace DynamicWindow.Core.Models;

public sealed class QuickControlItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPrototypeOnly { get; init; } = true;
    public bool HasSlider { get; init; }
    public double SliderValue { get; set; }
}
