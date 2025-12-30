namespace SaveState.Application.GameLibrary.ReadModels;

/// <summary>
/// Read model for platform statistics and analytics.
/// Optimized for dashboard and reporting views.
/// </summary>
public class PlatformStatistics
{
    public string PlatformName { get; init; } = string.Empty;
    public int TotalGames { get; init; }
    public int InstalledGames { get; init; }
    public int RunningGames { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
}
