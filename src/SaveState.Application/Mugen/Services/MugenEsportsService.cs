using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional esports infrastructure for MUGEN competitive gaming.
/// Manages leagues, rankings, sponsorships, and professional player ecosystem.
/// </summary>
public class MugenEsportsService : MugenEsportsServiceIMugenEsportsService
{
    private readonly ILogger<MugenEsportsService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MugenEsportsServiceEsportsLeague> _activeLeagues = new();
    private readonly Dictionary<string, MugenEsportsServiceProfessionalPlayer> _registeredPlayers = new();
    private readonly Dictionary<string, MugenEsportsServiceTeam> _registeredTeams = new();
    private readonly MugenEsportsServiceRankingSystem _rankingSystem;
    private readonly MugenEsportsServiceSponsorshipManager _sponsorshipManager;

    public MugenEsportsService(
        ILogger<MugenEsportsService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _rankingSystem = new MugenEsportsServiceRankingSystem(loggerFactory.CreateLogger<MugenEsportsServiceRankingSystem>(), timeProvider);
        _sponsorshipManager = new MugenEsportsServiceSponsorshipManager(loggerFactory.CreateLogger<MugenEsportsServiceSponsorshipManager>(), timeProvider);
    }

    public async Task<Result<MugenEsportsServiceEsportsLeague>> CreateLeagueAsync(MugenEsportsServiceLeagueCreationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating esports league: {Name}", request.Name);

            // Validate league parameters
            var validation = ValidateLeagueRequest(request);
            if (!validation.IsSuccess)
            {
                return Result.Failure<MugenEsportsServiceEsportsLeague>(validation.Error!);
            }

            var league = new MugenEsportsServiceEsportsLeague
            {
                LeagueId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Tier = request.Tier,
                Region = request.Region,
                MugenEsportsServiceGameMode = request.MugenEsportsServiceGameMode,
                MaxTeams = request.MaxTeams,
                MinTeamSize = request.MinTeamSize,
                MaxTeamSize = request.MaxTeamSize,
                SeasonLength = request.SeasonLength,
                Status = MugenEsportsServiceLeagueStatus.Forming,
                CreatedAt = _timeProvider.UtcNow,
                RegistrationDeadline = request.RegistrationDeadline,
                SeasonStartDate = request.SeasonStartDate,
                PrizePool = request.BasePrizePool,
                Sponsors = new List<string>(),
                RegisteredTeams = new List<string>(),
                Rules = request.Rules
            };

            _activeLeagues[league.LeagueId] = league;

            // Initialize ranking system for the league
            await _rankingSystem.InitializeLeagueRankingsAsync(league.LeagueId, ct);

            _logger.LogInformation("Esports league created: {LeagueId}", league.LeagueId);
            return Result.Success<MugenEsportsServiceEsportsLeague>(league);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating league {Name}", request.Name);
            return Result.Failure<MugenEsportsServiceEsportsLeague>($"Failed to create league: {ex.Message}");
        }
    }

    public async Task<Result> RegisterTeamForLeagueAsync(string leagueId, string teamId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Registering team {TeamId} for league {LeagueId}", teamId, leagueId);

            if (!_activeLeagues.TryGetValue(leagueId, out var league))
            {
                return Result.Failure("League not found");
            }

            if (league.Status != MugenEsportsServiceLeagueStatus.Forming && league.Status != MugenEsportsServiceLeagueStatus.RegistrationOpen)
            {
                return Result.Failure("League registration is not open");
            }

            if (league.RegisteredTeams.Count >= league.MaxTeams)
            {
                return Result.Failure("League is at maximum capacity");
            }

            if (!league.RegisteredTeams.Contains(teamId))
            {
                league.RegisteredTeams.Add(teamId);
                await UpdateLeagueCacheAsync(league, ct);
            }

            _logger.LogInformation("MugenEsportsServiceTeam {TeamId} registered for league {LeagueId}", teamId, leagueId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering team {TeamId} for league {LeagueId}", teamId, leagueId);
            return Result.Failure($"Failed to register team: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceProfessionalPlayer>> RegisterProfessionalPlayerAsync(MugenEsportsServicePlayerRegistrationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Registering professional player: {PlayerId}", request.PlayerId);

            var player = new MugenEsportsServiceProfessionalPlayer
            {
                PlayerId = request.PlayerId,
                GamerTag = request.GamerTag,
                RealName = request.RealName,
                Email = request.Email,
                Country = request.Country,
                DateOfBirth = request.DateOfBirth,
                RegistrationDate = _timeProvider.UtcNow,
                Status = MugenEsportsServicePlayerStatus.Active,
                CurrentTeam = request.TeamId,
                MainCharacters = request.MainCharacters,
                RankingPoints = 1000, // Starting ranking
                Achievements = new List<string>(),
                Statistics = new MugenEsportsServicePlayerStatistics
                {
                    TotalMatches = 0,
                    Wins = 0,
                    Losses = 0,
                    WinRate = 0.0,
                    AverageMatchDuration = TimeSpan.Zero,
                    FavoriteCharacter = null,
                    BestStreak = 0
                }
            };

            _registeredPlayers[request.PlayerId] = player;

            // Initialize player ranking
            await _rankingSystem.RegisterPlayerAsync(request.PlayerId, ct);

            _logger.LogInformation("Professional player registered: {PlayerId}", request.PlayerId);
            return Result.Success<MugenEsportsServiceProfessionalPlayer>(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering player {PlayerId}", request.PlayerId);
            return Result.Failure<MugenEsportsServiceProfessionalPlayer>($"Failed to register player: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceTeam>> CreateTeamAsync(MugenEsportsServiceTeamCreationRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Creating esports team: {Name}", request.Name);

            var team = new MugenEsportsServiceTeam
            {
                TeamId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Tag = request.Tag,
                Description = request.Description,
                CaptainId = request.CaptainId,
                Region = request.Region,
                FoundedDate = _timeProvider.UtcNow,
                Status = MugenEsportsServiceTeamStatus.Active,
                Players = new List<string> { request.CaptainId },
                Coaches = (request.Coaches ?? Enumerable.Empty<string>()).ToList(),
                Staff = (request.Staff ?? Enumerable.Empty<string>()).ToList(),
                Sponsors = new List<string>(),
                Achievements = new List<string>(),
                Statistics = new MugenEsportsServiceTeamStatistics
                {
                    TotalTournaments = 0,
                    Wins = 0,
                    Losses = 0,
                    WinRate = 0.0,
                    BestFinish = null,
                    TotalPrizeMoney = 0
                }
            };

            _registeredTeams[team.TeamId] = team;

            // Add team to player's current team
            if (_registeredPlayers.TryGetValue(request.CaptainId, out var captain))
            {
                captain.CurrentTeam = team.TeamId;
            }

            _logger.LogInformation("Esports team created: {TeamId}", team.TeamId);
            return Result.Success<MugenEsportsServiceTeam>(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team {Name}", request.Name);
            return Result.Failure<MugenEsportsServiceTeam>($"Failed to create team: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceLeagueRankings>> GetLeagueRankingsAsync(string leagueId, CancellationToken ct = default)
    {
        try
        {
            var rankings = await _rankingSystem.GetLeagueRankingsAsync(leagueId, ct);
            return Result.Success<MugenEsportsServiceLeagueRankings>(rankings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting league rankings for {LeagueId}", leagueId);
            return Result.Failure<MugenEsportsServiceLeagueRankings>($"Failed to get rankings: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceGlobalRankings>> GetGlobalRankingsAsync(MugenEsportsServiceRankingPeriod period, CancellationToken ct = default)
    {
        try
        {
            var rankings = await _rankingSystem.GetGlobalRankingsAsync(period, ct);
            return Result.Success<MugenEsportsServiceGlobalRankings>(rankings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global rankings for period {Period}", period);
            return Result.Failure<MugenEsportsServiceGlobalRankings>($"Failed to get global rankings: {ex.Message}");
        }
    }

    public async Task<Result> UpdatePlayerStatisticsAsync(string playerId, MatchResult result, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating statistics for player {PlayerId}", playerId);

            if (!_registeredPlayers.TryGetValue(playerId, out var player))
            {
                return Result.Failure("Player not found");
            }

            // Update player statistics
            player.Statistics.TotalMatches++;

            if (result == MatchResult.Player1Win) // Assuming player is player 1
            {
                player.Statistics.Wins++;
            }
            else
            {
                player.Statistics.Losses++;
            }

            player.Statistics.WinRate = (double)player.Statistics.Wins / player.Statistics.TotalMatches;

            // Update ranking points
            var pointsChange = CalculateRankingPointsChange(result, player.RankingPoints);
            player.RankingPoints += pointsChange;

            // Update ranking system
            await _rankingSystem.UpdatePlayerRankingAsync(playerId, player.RankingPoints, ct);

            _logger.LogInformation("Player statistics updated for {PlayerId}", playerId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statistics for player {PlayerId}", playerId);
            return Result.Failure($"Failed to update statistics: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceSponsorshipDeal>> CreateSponsorshipAsync(MugenEsportsServiceSponsorshipRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating sponsorship deal for {RecipientId}", request.RecipientId);

            var deal = await _sponsorshipManager.CreateSponsorshipAsync(request, ct);

            // Add sponsor to league or team
            if (_activeLeagues.TryGetValue(request.RecipientId, out var league))
            {
                league.Sponsors.Add(request.SponsorName);
                league.PrizePool += request.Amount;
            }
            else if (_registeredTeams.TryGetValue(request.RecipientId, out var team))
            {
                team.Sponsors.Add(request.SponsorName);
            }

            return Result.Success<MugenEsportsServiceSponsorshipDeal>(deal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sponsorship for {RecipientId}", request.RecipientId);
            return Result.Failure<MugenEsportsServiceSponsorshipDeal>($"Failed to create sponsorship: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceEsportsEvent>> CreateEsportsEventAsync(MugenEsportsServiceEventCreationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating esports event: {Name}", request.Name);

            var esportEvent = new MugenEsportsServiceEsportsEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Venue = request.Venue,
                PrizePool = request.PrizePool,
                MaxParticipants = request.MaxParticipants,
                RegisteredParticipants = new List<string>(),
                Sponsors = request.Sponsors,
                Organizers = request.Organizers,
                Status = MugenEsportsServiceEventStatus.Planning
            };

            // Store event (simplified - would use repository)
            await Task.Delay(100, default); // Simulate storage

            _logger.LogInformation("Esports event created: {EventId}", esportEvent.EventId);
            return Result.Success<MugenEsportsServiceEsportsEvent>(esportEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating esports event {Name}", request.Name);
            return Result.Failure<MugenEsportsServiceEsportsEvent>($"Failed to create event: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceProfessionalPlayer>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            if (!_registeredPlayers.TryGetValue(playerId, out var player))
            {
                return Result.Failure<MugenEsportsServiceProfessionalPlayer>("Player not found");
            }

            return Result.Success<MugenEsportsServiceProfessionalPlayer>(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile for {PlayerId}", playerId);
            return Result.Failure<MugenEsportsServiceProfessionalPlayer>($"Failed to get profile: {ex.Message}");
        }
    }

    public async Task<Result<MugenEsportsServiceTeam>> GetTeamProfileAsync(string teamId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            if (!_registeredTeams.TryGetValue(teamId, out var team))
            {
                return Result.Failure<MugenEsportsServiceTeam>("MugenEsportsServiceTeam not found");
            }

            return Result.Success<MugenEsportsServiceTeam>(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team profile for {TeamId}", teamId);
            return Result.Failure<MugenEsportsServiceTeam>($"Failed to get profile: {ex.Message}");
        }
    }

    #region Private Methods

    private Result ValidateLeagueRequest(MugenEsportsServiceLeagueCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure("League name is required");

        if (request.MaxTeams < 4)
            return Result.Failure("League must have at least 4 teams");

        if (request.MinTeamSize < 1 || request.MaxTeamSize > 8)
            return Result.Failure("Invalid team size constraints");

        if (request.SeasonStartDate <= _timeProvider.UtcNow)
            return Result.Failure("Season start date must be in the future");

        return Result.Success();
    }

    private async Task UpdateLeagueCacheAsync(MugenEsportsServiceEsportsLeague league, CancellationToken ct)
    {
        var cacheKey = $"league_{league.LeagueId}";
        await _cache.SetAsync(cacheKey, league, TimeSpan.FromHours(1), ct);
    }

    private int CalculateRankingPointsChange(MatchResult result, int currentPoints)
    {
        var baseChange = 25; // Base points for a match

        if (result == MatchResult.Player1Win)
        {
            return baseChange;
        }
        else
        {
            return -baseChange;
        }
    }

    #endregion
}

/// <summary>
/// Ranking system for esports leagues and players.
/// </summary>
public class MugenEsportsServiceRankingSystem
{
    private readonly ILogger<MugenEsportsServiceRankingSystem> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MugenEsportsServiceLeagueRankings> _leagueRankings = new();
    private readonly Dictionary<string, int> _playerRankings = new();

    public MugenEsportsServiceRankingSystem(ILogger<MugenEsportsServiceRankingSystem> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task InitializeLeagueRankingsAsync(string leagueId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _leagueRankings[leagueId] = new MugenEsportsServiceLeagueRankings
        {
            LeagueId = leagueId,
            Rankings = new List<MugenEsportsServiceTeamRanking>(),
            LastUpdated = _timeProvider.UtcNow
        };
    }

    public async Task RegisterPlayerAsync(string playerId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _playerRankings[playerId] = 1000; // Starting ranking
    }

    public async Task UpdatePlayerRankingAsync(string playerId, int newPoints, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _playerRankings[playerId] = newPoints;
    }

    public async Task<MugenEsportsServiceLeagueRankings> GetLeagueRankingsAsync(string leagueId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        if (!_leagueRankings.TryGetValue(leagueId, out var rankings))
        {
            throw new InvalidOperationException("League rankings not found");
        }

        return rankings;
    }

    public async Task<MugenEsportsServiceGlobalRankings> GetGlobalRankingsAsync(MugenEsportsServiceRankingPeriod period, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var topPlayers = _playerRankings
            .OrderByDescending(kvp => kvp.Value)
            .Take(100)
            .Select((kvp, index) => new MugenEsportsServiceEsportsPlayerRanking
            {
                PlayerId = kvp.Key,
                Rank = index + 1,
                Points = kvp.Value,
                Change = 0 // Simplified
            })
            .ToList();

        return new MugenEsportsServiceGlobalRankings
        {
            Period = period,
            Rankings = topPlayers,
            LastUpdated = _timeProvider.UtcNow
        };
    }
}

/// <summary>
/// Sponsorship manager for managing sponsorship deals.
/// </summary>
public class MugenEsportsServiceSponsorshipManager
{
    private readonly ILogger<MugenEsportsServiceSponsorshipManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<MugenEsportsServiceSponsorshipDeal> _activeDeals = new();

    public MugenEsportsServiceSponsorshipManager(ILogger<MugenEsportsServiceSponsorshipManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<MugenEsportsServiceSponsorshipDeal> CreateSponsorshipAsync(MugenEsportsServiceSponsorshipRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var deal = new MugenEsportsServiceSponsorshipDeal
        {
            DealId = Guid.NewGuid().ToString(),
            SponsorName = request.SponsorName,
            RecipientId = request.RecipientId,
            RecipientType = request.RecipientType,
            Amount = request.Amount,
            Duration = request.Duration,
            Terms = request.Terms,
            StartDate = _timeProvider.UtcNow,
            Status = MugenEsportsServiceSponsorshipStatus.Active
        };

        _activeDeals.Add(deal);
        return deal;
    }
}

/// <summary>
/// MUGEN Esports service interface.
/// </summary>
public interface MugenEsportsServiceIMugenEsportsService
{
    Task<Result<MugenEsportsServiceEsportsLeague>> CreateLeagueAsync(MugenEsportsServiceLeagueCreationRequest request, CancellationToken ct = default);
    Task<Result> RegisterTeamForLeagueAsync(string leagueId, string teamId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceProfessionalPlayer>> RegisterProfessionalPlayerAsync(MugenEsportsServicePlayerRegistrationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceTeam>> CreateTeamAsync(MugenEsportsServiceTeamCreationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceLeagueRankings>> GetLeagueRankingsAsync(string leagueId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceGlobalRankings>> GetGlobalRankingsAsync(MugenEsportsServiceRankingPeriod period, CancellationToken ct = default);
    Task<Result> UpdatePlayerStatisticsAsync(string playerId, MatchResult result, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceSponsorshipDeal>> CreateSponsorshipAsync(MugenEsportsServiceSponsorshipRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceEsportsEvent>> CreateEsportsEventAsync(MugenEsportsServiceEventCreationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceProfessionalPlayer>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceTeam>> GetTeamProfileAsync(string teamId, CancellationToken ct = default);
}

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
