namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Esports league data.
/// </summary>
public class MugenEsportsServiceEsportsLeague
{
    public string LeagueId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MugenEsportsServiceLeagueTier Tier { get; set; } = default!;
    public string Region { get; set; } = default!;
    public MugenEsportsServiceGameMode MugenEsportsServiceGameMode { get; set; } = default!;
    public int MaxTeams { get; set; } = default!;
    public int MinTeamSize { get; set; } = default!;
    public int MaxTeamSize { get; set; } = default!;
    public TimeSpan SeasonLength { get; set; } = default!;
    public MugenEsportsServiceLeagueStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? RegistrationDeadline { get; set; } = default!;
    public DateTime? SeasonStartDate { get; set; } = default!;
    public decimal PrizePool { get; set; } = default!;
    public List<string> Sponsors { get; set; } = default!;
    public List<string> RegisteredTeams { get; set; } = default!;
    public MugenEsportsServiceLeagueRules Rules { get; set; } = default!;
}

/// <summary>
/// League tier enumeration.
/// </summary>
public enum MugenEsportsServiceLeagueTier
{
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Masters,
    GrandMasters
}

/// <summary>
/// League status enumeration.
/// </summary>
public enum MugenEsportsServiceLeagueStatus
{
    Forming,
    RegistrationOpen,
    InProgress,
    Playoffs,
    Completed,
    Cancelled
}

/// <summary>
/// Game mode enumeration.
/// </summary>
public enum MugenEsportsServiceGameMode
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    SwissSystem,
    GroupStage
}

/// <summary>
/// League rules.
/// </summary>
public class MugenEsportsServiceLeagueRules
{
    public string Format { get; set; } = default!;
    public string MatchRules { get; set; } = default!;
    public string ConductRules { get; set; } = default!;
    public string PrizeDistribution { get; set; } = default!;
}

/// <summary>
/// League creation request.
/// </summary>
public class MugenEsportsServiceLeagueCreationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MugenEsportsServiceLeagueTier Tier { get; set; } = default!;
    public string Region { get; set; } = default!;
    public MugenEsportsServiceGameMode MugenEsportsServiceGameMode { get; set; } = default!;
    public int MaxTeams { get; set; } = default!;
    public int MinTeamSize { get; set; } = default!;
    public int MaxTeamSize { get; set; } = default!;
    public TimeSpan SeasonLength { get; set; } = default!;
    public decimal BasePrizePool { get; set; } = default!;
    public DateTime? RegistrationDeadline { get; set; } = default!;
    public DateTime SeasonStartDate { get; set; } = default!;
    public MugenEsportsServiceLeagueRules Rules { get; set; } = default!;
}

/// <summary>
/// Professional player data.
/// </summary>
public class MugenEsportsServiceProfessionalPlayer
{
    public string PlayerId { get; set; } = default!;
    public string GamerTag { get; set; } = default!;
    public string RealName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Country { get; set; } = default!;
    public DateTime DateOfBirth { get; set; } = default!;
    public DateTime RegistrationDate { get; set; } = default!;
    public MugenEsportsServicePlayerStatus Status { get; set; } = default!;
    public string? CurrentTeam { get; set; } = default!;
    public IReadOnlyList<string> MainCharacters { get; set; } = default!;
    public int RankingPoints { get; set; } = default!;
    public IReadOnlyList<string> Achievements { get; set; } = default!;
    public MugenEsportsServicePlayerStatistics Statistics { get; set; } = default!;
}

/// <summary>
/// Player status enumeration.
/// </summary>
public enum MugenEsportsServicePlayerStatus
{
    Active,
    Inactive,
    Suspended,
    Retired
}

/// <summary>
/// Player statistics.
/// </summary>
public class MugenEsportsServicePlayerStatistics
{
    public int TotalMatches { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
    public double WinRate { get; set; } = default!;
    public TimeSpan AverageMatchDuration { get; set; } = default!;
    public string? FavoriteCharacter { get; set; } = default!;
    public int BestStreak { get; set; } = default!;
}

/// <summary>
/// Player registration request.
/// </summary>
public class MugenEsportsServicePlayerRegistrationRequest
{
    public string PlayerId { get; set; } = default!;
    public string GamerTag { get; set; } = default!;
    public string RealName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Country { get; set; } = default!;
    public DateTime DateOfBirth { get; set; } = default!;
    public string? TeamId { get; set; } = default!;
    public IReadOnlyList<string> MainCharacters { get; set; } = default!;
}

/// <summary>
/// MugenEsportsServiceTeam data.
/// </summary>
public class MugenEsportsServiceTeam
{
    public string TeamId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Tag { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string CaptainId { get; set; } = default!;
    public string Region { get; set; } = default!;
    public DateTime FoundedDate { get; set; } = default!;
    public MugenEsportsServiceTeamStatus Status { get; set; } = default!;
    public List<string> Players { get; set; } = default!;
    public List<string> Coaches { get; set; } = default!;
    public List<string> Staff { get; set; } = default!;
    public List<string> Sponsors { get; set; } = default!;
    public List<string> Achievements { get; set; } = default!;
    public MugenEsportsServiceTeamStatistics Statistics { get; set; } = default!;
}

/// <summary>
/// MugenEsportsServiceTeam status enumeration.
/// </summary>
public enum MugenEsportsServiceTeamStatus
{
    Active,
    Inactive,
    Disbanded
}

/// <summary>
/// MugenEsportsServiceTeam statistics.
/// </summary>
public class MugenEsportsServiceTeamStatistics
{
    public int TotalTournaments { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
    public double WinRate { get; set; } = default!;
    public string? BestFinish { get; set; } = default!;
    public decimal TotalPrizeMoney { get; set; } = default!;
}

/// <summary>
/// MugenEsportsServiceTeam creation request.
/// </summary>
public class MugenEsportsServiceTeamCreationRequest
{
    public string Name { get; set; } = default!;
    public string Tag { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string CaptainId { get; set; } = default!;
    public string Region { get; set; } = default!;
    public IReadOnlyList<string>? Coaches { get; set; } = default!;
    public IReadOnlyList<string>? Staff { get; set; } = default!;
}

/// <summary>
/// League rankings data.
/// </summary>
public class MugenEsportsServiceLeagueRankings
{
    public string LeagueId { get; set; } = default!;
    public IReadOnlyList<MugenEsportsServiceTeamRanking> Rankings { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// MugenEsportsServiceTeam ranking data.
/// </summary>
public class MugenEsportsServiceTeamRanking
{
    public string TeamId { get; set; } = default!;
    public string TeamName { get; set; } = default!;
    public int Rank { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
    public int Points { get; set; } = default!;
    public double WinRate { get; set; } = default!;
}

/// <summary>
/// Global rankings data.
/// </summary>
public class MugenEsportsServiceGlobalRankings
{
    public MugenEsportsServiceRankingPeriod Period { get; set; } = default!;
    public IReadOnlyList<MugenEsportsServiceEsportsPlayerRanking> Rankings { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Ranking period enumeration.
/// </summary>
public enum MugenEsportsServiceRankingPeriod
{
    Daily,
    Weekly,
    Monthly,
    AllTime
}

/// <summary>
/// Player ranking data.
/// </summary>
public class MugenEsportsServiceEsportsPlayerRanking
{
    public string PlayerId { get; set; } = default!;
    public int Rank { get; set; } = default!;
    public int Points { get; set; } = default!;
    public int Change { get; set; } = default!;
}

/// <summary>
/// Sponsorship deal data.
/// </summary>
public class MugenEsportsServiceSponsorshipDeal
{
    public string DealId { get; set; } = default!;
    public string SponsorName { get; set; } = default!;
    public string RecipientId { get; set; } = default!;
    public MugenEsportsServiceSponsorshipRecipientType RecipientType { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public string Terms { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public MugenEsportsServiceSponsorshipStatus Status { get; set; } = default!;
}

/// <summary>
/// Sponsorship recipient type.
/// </summary>
public enum MugenEsportsServiceSponsorshipRecipientType
{
    League,
    MugenEsportsServiceTeam,
    Player,
    Event
}

/// <summary>
/// Sponsorship status.
/// </summary>
public enum MugenEsportsServiceSponsorshipStatus
{
    Pending,
    Active,
    Completed,
    Cancelled
}

/// <summary>
/// Sponsorship request.
/// </summary>
public class MugenEsportsServiceSponsorshipRequest
{
    public string SponsorName { get; set; } = default!;
    public string RecipientId { get; set; } = default!;
    public MugenEsportsServiceSponsorshipRecipientType RecipientType { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public string Terms { get; set; } = default!;
}

/// <summary>
/// Esports event data.
/// </summary>
public class MugenEsportsServiceEsportsEvent
{
    public string EventId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MugenEsportsServiceEventType Type { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public string Venue { get; set; } = default!;
    public decimal PrizePool { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public IReadOnlyList<string> RegisteredParticipants { get; set; } = default!;
    public IReadOnlyList<string> Sponsors { get; set; } = default!;
    public IReadOnlyList<string> Organizers { get; set; } = default!;
    public MugenEsportsServiceEventStatus Status { get; set; } = default!;
}

/// <summary>
/// Event type enumeration.
/// </summary>
public enum MugenEsportsServiceEventType
{
    Tournament,
    League,
    Exhibition,
    Charity
}

/// <summary>
/// Event status enumeration.
/// </summary>
public enum MugenEsportsServiceEventStatus
{
    Planning,
    RegistrationOpen,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Event creation request.
/// </summary>
public class MugenEsportsServiceEventCreationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MugenEsportsServiceEventType Type { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public string Venue { get; set; } = default!;
    public decimal PrizePool { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public IReadOnlyList<string> Sponsors { get; set; } = default!;
    public IReadOnlyList<string> Organizers { get; set; } = default!;
}
