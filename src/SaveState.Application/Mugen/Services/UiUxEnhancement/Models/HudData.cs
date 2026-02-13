namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD data for updates.
/// </summary>
public class HudData
{
    public float Fps { get; set; } = default!;
    public float MemoryUsage { get; set; } = default!;
    public bool CriticalEvent { get; set; } = default!;
    public bool PerformanceMode { get; set; } = default!;
    public bool LowVisibility { get; set; } = default!;
    public IReadOnlyDictionary<string, object> ElementValues { get; set; } = default!;

    public object? GetValueForElement(string elementId)
    {
        return ElementValues.TryGetValue(elementId, out var value) ? value : null;
    }
}
