using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Infrastructure.RetroAchievements;

/// <summary>
/// RetroAchievements.org API client implementation.
/// </summary>
public class RetroAchievementsApiClient : IRetroAchievementsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RetroAchievementsApiClient> _logger;
    private readonly RetroAchievementsOptions _options;
    private Timer? _richPresenceTimer;
    private int _currentGameId;
    private string? _currentUsername;

    public RetroAchievementsApiClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<RetroAchievementsApiClient> logger,
        IOptions<RetroAchievementsOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        
        _httpClient.BaseAddress = new Uri("https://retroachievements.org/API/");
    }

    public event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;
    public event EventHandler<ProgressUpdatedEventArgs>? ProgressUpdated;

    public async Task<Result<bool>> ValidateCredentialsAsync(string username, string apiKey, CancellationToken ct = default)
    {
        try
        {
            var url = $"?z={Uri.EscapeDataString(username)}&y={Uri.EscapeDataString(apiKey)}";
            var response = await _httpClient.GetAsync(url + "&r=user&z=" + username, ct);
            
            if (!response.IsSuccessStatusCode)
                return Result<bool>.Success(false);
            
            var content = await response.Content.ReadAsStringAsync(ct);
            return Result<bool>.Success(!content.Contains("error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate RetroAchievements credentials");
            return Result<bool>.Failure($"Validation failed: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<RetroUserSummary>> GetUserSummaryAsync(string username, CancellationToken ct = default)
    {
        var cacheKey = $"ra:user:{username}";
        if (_cache.TryGetValue(cacheKey, out RetroUserSummary? cached))
            return Result<RetroUserSummary>.Success(cached!);

        try
        {
            var url = BuildApiUrl("API_GetUserSummary.php", $"u={Uri.EscapeDataString(username)}&g=1&a=5");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<UserSummaryDto>(json, JsonOptions);
            
            if (dto == null)
                return Result<RetroUserSummary>.Failure("Invalid response from API", ErrorType.External);
            
            var summary = MapToUserSummary(dto);
            _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(5));
            
            return Result<RetroUserSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user summary for {Username}", username);
            return Result<RetroUserSummary>.Failure($"Failed to get user summary: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<RetroGameInfo>> GetGameInfoAsync(int gameId, CancellationToken ct = default)
    {
        var cacheKey = $"ra:game:{gameId}";
        if (_cache.TryGetValue(cacheKey, out RetroGameInfo? cached))
            return Result<RetroGameInfo>.Success(cached!);

        try
        {
            var url = BuildApiUrl("API_GetGame.php", $"i={gameId}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<GameInfoDto>(json, JsonOptions);
            
            if (dto == null)
                return Result<RetroGameInfo>.Failure("Invalid response from API", ErrorType.External);
            
            var info = MapToGameInfo(dto);
            _cache.Set(cacheKey, info, TimeSpan.FromHours(1));
            
            return Result<RetroGameInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game info for {GameId}", gameId);
            return Result<RetroGameInfo>.Failure($"Failed to get game info: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<List<RetroAchievement>>> GetGameAchievementsAsync(int gameId, CancellationToken ct = default)
    {
        var cacheKey = $"ra:achievements:{gameId}";
        if (_cache.TryGetValue(cacheKey, out List<RetroAchievement>? cached))
            return Result<List<RetroAchievement>>.Success(cached!);

        try
        {
            var url = BuildApiUrl("API_GetGameInfoAndUserProgress.php", $"g={gameId}&u={_options.Username}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<GameInfoExtendedDto>(json, JsonOptions);
            
            if (dto?.Achievements == null)
                return Result<List<RetroAchievement>>.Success(new List<RetroAchievement>());
            
            var achievements = dto.Achievements.Values.Select(MapToAchievement).ToList();
            _cache.Set(cacheKey, achievements, TimeSpan.FromMinutes(30));
            
            return Result<List<RetroAchievement>>.Success(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get achievements for game {GameId}", gameId);
            return Result<List<RetroAchievement>>.Failure($"Failed to get achievements: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<List<UserRetroAchievementProgress>>> GetUserGameProgressAsync(
        string username, int gameId, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_GetGameInfoAndUserProgress.php", $"g={gameId}&u={Uri.EscapeDataString(username)}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<GameInfoExtendedDto>(json, JsonOptions);
            
            if (dto?.Achievements == null)
                return Result<List<UserRetroAchievementProgress>>.Success(new List<UserRetroAchievementProgress>());
            
            var progress = dto.Achievements.Values.Select(a => new UserRetroAchievementProgress
            {
                AchievementId = a.ID,
                IsUnlocked = a.DateEarned != null,
                UnlockedAt = a.DateEarned != null ? DateTime.Parse(a.DateEarned) : null,
                IsHardcore = a.DateEarnedHardcore != null,
                LastUpdatedAt = DateTime.UtcNow
            }).ToList();
            
            return Result<List<UserRetroAchievementProgress>>.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user progress for {Username}, game {GameId}", username, gameId);
            return Result<List<UserRetroAchievementProgress>>.Failure($"Failed to get progress: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<List<RetroGameInfo>>> SearchGamesAsync(
        string query, int? consoleId = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_GetGameList.php", $"c={consoleId ?? 1}&f={Uri.EscapeDataString(query)}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dtos = JsonSerializer.Deserialize<List<GameListDto>>(json, JsonOptions);
            
            if (dtos == null)
                return Result<List<RetroGameInfo>>.Success(new List<RetroGameInfo>());
            
            var games = dtos.Select(MapToGameInfoFromList).ToList();
            return Result<List<RetroGameInfo>>.Success(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search games with query {Query}", query);
            return Result<List<RetroGameInfo>>.Failure($"Search failed: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<List<AchievementUnlockEvent>>> GetRecentUnlocksAsync(
        string username, int count = 10, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_GetUserRecentAchievements.php", 
                $"u={Uri.EscapeDataString(username)}&m={count}&r=true");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dtos = JsonSerializer.Deserialize<List<RecentAchievementDto>>(json, JsonOptions);
            
            if (dtos == null)
                return Result<List<AchievementUnlockEvent>>.Success(new List<AchievementUnlockEvent>());
            
            var events = dtos.Select(dto => new AchievementUnlockEvent
            {
                AchievementId = dto.AchievementID,
                UnlockedAt = DateTime.Parse(dto.Date),
                IsHardcore = dto.HardcoreMode == 1
            }).ToList();
            
            return Result<List<AchievementUnlockEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent unlocks for {Username}", username);
            return Result<List<AchievementUnlockEvent>>.Failure($"Failed to get recent unlocks: {ex.Message}", ErrorType.External);
        }
    }

    public Task<Result> StartRichPresenceAsync(int gameId, CancellationToken ct = default)
    {
        _currentGameId = gameId;
        _currentUsername = _options.Username;
        
        // Start polling for rich presence updates
        _richPresenceTimer?.Dispose();
        _richPresenceTimer = new Timer(
            async _ => await PollRichPresenceAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Started rich presence monitoring for game {GameId}", gameId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopRichPresenceAsync(CancellationToken ct = default)
    {
        _richPresenceTimer?.Dispose();
        _richPresenceTimer = null;
        _currentGameId = 0;
        
        _logger.LogInformation("Stopped rich presence monitoring");
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<List<RetroLeaderboardEntry>>> GetLeaderboardAsync(
        int gameId, int leaderboardId, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_GetLeaderboardEntries.php", 
                $"i={leaderboardId}&c=50&o=0");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<LeaderboardDto>(json, JsonOptions);
            
            if (dto?.Entries == null)
                return Result<List<RetroLeaderboardEntry>>.Success(new List<RetroLeaderboardEntry>());
            
            var entries = dto.Entries.Select(e => new RetroLeaderboardEntry
            {
                Username = e.User,
                Score = e.Score,
                Rank = e.Rank,
                DateAchieved = DateTime.Parse(e.DateSubmitted)
            }).ToList();
            
            return Result<List<RetroLeaderboardEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard {LeaderboardId}", leaderboardId);
            return Result<List<RetroLeaderboardEntry>>.Failure($"Failed to get leaderboard: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> AwardAchievementAsync(
        string username, int achievementId, int? hash = null, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_AwardAchievement.php", 
                $"a={achievementId}&h={hash ?? 1}&v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            
            if (json.Contains("error"))
            {
                _logger.LogWarning("Failed to award achievement {AchievementId}: {Response}", achievementId, json);
                return Result.Failure("Failed to award achievement", ErrorType.External);
            }
            
            _logger.LogInformation("Awarded achievement {AchievementId} to {Username}", achievementId, username);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to award achievement {AchievementId}", achievementId);
            return Result.Failure($"Failed to award achievement: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<List<GameCompletionStatus>>> GetUserCompletionProgressAsync(
        string username, CancellationToken ct = default)
    {
        try
        {
            var url = BuildApiUrl("API_GetUserCompletionProgress.php", $"u={Uri.EscapeDataString(username)}");
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<CompletionProgressDto>(json, JsonOptions);
            
            if (dto?.Results == null)
                return Result<List<GameCompletionStatus>>.Success(new List<GameCompletionStatus>());
            
            var progress = dto.Results.Select(r => new GameCompletionStatus
            {
                GameId = r.GameID,
                AchievementsEarned = r.NumAwarded,
                TotalAchievements = r.MaxPossible,
                CompletionPercentage = r.MaxPossible > 0 ? (decimal)r.NumAwarded / r.MaxPossible * 100 : 0
            }).ToList();
            
            return Result<List<GameCompletionStatus>>.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completion progress for {Username}", username);
            return Result<List<GameCompletionStatus>>.Failure($"Failed to get progress: {ex.Message}", ErrorType.External);
        }
    }

    #region Private Methods

    private string BuildApiUrl(string endpoint, string parameters)
    {
        var credentials = $"z={Uri.EscapeDataString(_options.Username)}&y={_options.ApiKey}";
        return $"{endpoint}?{credentials}&{parameters}";
    }

    private async Task PollRichPresenceAsync()
    {
        if (_currentGameId == 0 || string.IsNullOrEmpty(_currentUsername))
            return;

        try
        {
            // Poll for new achievements and progress
            var progressResult = await GetUserGameProgressAsync(_currentUsername, _currentGameId);
            if (progressResult.IsSuccess)
            {
                // Check for newly unlocked achievements and raise events
                foreach (var ach in progressResult.Value.Where(p => p.IsUnlocked))
                {
                    var cacheKey = $"ra:unlock:{_currentUsername}:{ach.AchievementId}";
                    if (!_cache.TryGetValue(cacheKey, out _))
                    {
                        _cache.Set(cacheKey, true, TimeSpan.FromHours(24));
                        
                        // Get achievement details for the event
                        var achievements = await GetGameAchievementsAsync(_currentGameId);
                        var achievement = achievements.Value?.FirstOrDefault(a => a.RetroId == ach.AchievementId);
                        
                        if (achievement != null)
                        {
                            AchievementUnlocked?.Invoke(this, new AchievementUnlockedEventArgs
                            {
                                AchievementId = achievement.RetroId,
                                AchievementTitle = achievement.Title,
                                Points = achievement.Points,
                                BadgeUrl = achievement.BadgeUrl,
                                IsHardcore = ach.IsHardcore
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling rich presence");
        }
    }

    private static RetroUserSummary MapToUserSummary(UserSummaryDto dto)
    {
        return new RetroUserSummary
        {
            Username = dto.User,
            TotalAchievements = int.TryParse(dto.TotalAchievements, out var ta) ? ta : 0,
            TotalPoints = int.TryParse(dto.TotalPoints, out var tp) ? tp : 0,
            Rank = int.TryParse(dto.Rank, out var r) ? r : 0,
            Motto = dto.Motto,
            AvatarUrl = $"https://retroachievements.org{dto.UserPic}",
            RecentlyPlayed = dto.RecentlyPlayed?.Select(rp => new RetroRecentlyPlayedGame
            {
                GameId = rp.GameID,
                Title = rp.Title,
                ConsoleName = rp.ConsoleName,
                LastPlayed = DateTime.Parse(rp.LastPlayed),
                AchievementsEarned = rp.NumAwarded
            }).ToList() ?? new List<RetroRecentlyPlayedGame>()
        };
    }

    private static RetroGameInfo MapToGameInfo(GameInfoDto dto)
    {
        return new RetroGameInfo
        {
            Id = dto.ID,
            Title = dto.Title,
            ConsoleId = dto.ConsoleID,
            ConsoleName = dto.ConsoleName,
            IconUrl = $"https://retroachievements.org{dto.ImageIcon}",
            AchievementCount = dto.NumAchievements,
            TotalPoints = dto.Points
        };
    }

    private static RetroGameInfo MapToGameInfoFromList(GameListDto dto)
    {
        return new RetroGameInfo
        {
            Id = dto.ID,
            Title = dto.Title,
            ConsoleId = dto.ConsoleID,
            ConsoleName = string.Empty,
            IconUrl = $"https://retroachievements.org{dto.ImageIcon}",
            AchievementCount = dto.NumAchievements,
            TotalPoints = dto.Points
        };
    }

    private static RetroAchievement MapToAchievement(AchievementDto dto)
    {
        return new RetroAchievement
        {
            RetroId = dto.ID,
            GameId = dto.GameID,
            Title = dto.Title,
            Description = dto.Description,
            Points = dto.Points,
            Type = ParseAchievementType(dto.Flags),
            BadgeUrl = $"https://retroachievements.org/Badge/{dto.BadgeName}.png",
            EarnedCount = dto.TrueRatio,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(dto.DateCreated).DateTime,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    private static RetroAchievementType ParseAchievementType(int flags)
    {
        return flags switch
        {
            3 => RetroAchievementType.Progression,
            5 => RetroAchievementType.WinCondition,
            4 => RetroAchievementType.Missable,
            _ => RetroAchievementType.Standard
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    #endregion
}

/// <summary>
/// Configuration options for RetroAchievements integration.
/// </summary>
public class RetroAchievementsOptions
{
    public const string SectionName = "RetroAchievements";
    
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool EnableRichPresence { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
}

#region DTOs

internal class UserSummaryDto
{
    public string User { get; set; } = string.Empty;
    public string TotalAchievements { get; set; } = string.Empty;
    public string TotalPoints { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public string? Motto { get; set; }
    public string UserPic { get; set; } = string.Empty;
    public List<RecentlyPlayedDto>? RecentlyPlayed { get; set; }
}

internal class RecentlyPlayedDto
{
    public int GameID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ConsoleName { get; set; } = string.Empty;
    public string LastPlayed { get; set; } = string.Empty;
    public int NumAwarded { get; set; }
}

internal class GameInfoDto
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ConsoleID { get; set; }
    public string ConsoleName { get; set; } = string.Empty;
    public string ImageIcon { get; set; } = string.Empty;
    public int NumAchievements { get; set; }
    public int Points { get; set; }
}

internal class GameInfoExtendedDto
{
    public Dictionary<string, AchievementDto>? Achievements { get; set; }
}

internal class AchievementDto
{
    public int ID { get; set; }
    public int GameID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Flags { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public int TrueRatio { get; set; }
    public int DateCreated { get; set; }
    public string? DateEarned { get; set; }
    public string? DateEarnedHardcore { get; set; }
}

internal class GameListDto
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ConsoleID { get; set; }
    public string ImageIcon { get; set; } = string.Empty;
    public int NumAchievements { get; set; }
    public int Points { get; set; }
}

internal class RecentAchievementDto
{
    public int AchievementID { get; set; }
    public string Date { get; set; } = string.Empty;
    public int HardcoreMode { get; set; }
}

internal class LeaderboardDto
{
    public List<LeaderboardEntryDto>? Entries { get; set; }
}

internal class LeaderboardEntryDto
{
    public string User { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Rank { get; set; }
    public string DateSubmitted { get; set; } = string.Empty;
}

internal class CompletionProgressDto
{
    public List<CompletionEntryDto>? Results { get; set; }
}

internal class CompletionEntryDto
{
    public int GameID { get; set; }
    public int NumAwarded { get; set; }
    public int MaxPossible { get; set; }
}

#endregion
