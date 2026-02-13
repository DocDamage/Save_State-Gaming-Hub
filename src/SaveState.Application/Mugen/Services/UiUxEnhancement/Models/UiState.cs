namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI state data.
/// </summary>
public class UiState
{
    public string SessionId { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
    public DateTime LastUpdate { get; set; } = default!;
    public UiPerformanceMetrics PerformanceMetrics { get; set; } = default!;
    public UiUserPreferences UserPreferences { get; set; } = default!;
}
