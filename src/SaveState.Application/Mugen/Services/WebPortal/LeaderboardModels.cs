namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Leaderboard data.
/// </summary>
public class WebPortalServiceLeaderboardData
{
    public WebPortalServiceLeaderboardType WebPortalServiceLeaderboardType { get; set; } = default!;
    public WebPortalServiceTimeFrame WebPortalServiceTimeFrame { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceLeaderboardEntry> Entries { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public int TotalEntries { get; set; } = default!;
}

/// <summary>
/// Leaderboard query parameters.
/// </summary>
public class WebPortalServiceLeaderboardQuery
{
    public WebPortalServiceLeaderboardType WebPortalServiceLeaderboardType { get; set; } = default!;
    public WebPortalServiceTimeFrame WebPortalServiceTimeFrame { get; set; } = default!;
    public int Limit { get; set; } = default!;
}

/// <summary>
/// Leaderboard entry.
/// </summary>
public class WebPortalServiceLeaderboardEntry
{
    public int Rank { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int Score { get; set; } = default!;
    public int Change { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = default!;
}
