using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.External;

/// <summary>
/// HTTP client implementation for RetroAchievements.org API.
/// API documentation: https://api-docs.retroachievements.org/
/// </summary>
public class RetroAchievementsClient : IRetroAchievementsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RetroAchievementsClient> _logger;
    private string? _username;
    private string? _apiKey;

    private const string BaseUrl = "https://retroachievements.org/API/";

    public bool IsAuthenticated => !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_apiKey);

    public RetroAchievementsClient(HttpClient httpClient, ILogger<RetroAchievementsClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _logger = logger;
    }

    public async Task<bool> AuthenticateAsync(string username, string apiKey, CancellationToken ct = default)
    {
        try
        {
            _username = username;
            _apiKey = apiKey;

            // Verify credentials by fetching user profile
            var profileResult = await GetUserProfileAsync(ct).ConfigureAwait(false);

            if (profileResult.IsFailure)
            {
                _username = null;
                _apiKey = null;
                return false;
            }

            _logger.LogInformation("RetroAchievements authenticated as {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with RetroAchievements");
            _username = null;
            _apiKey = null;
            return false;
        }
    }

    public async Task<Result<RAUserProfile>> GetUserProfileAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<RAUserProfile>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetUserSummary.php?z={_username}&y={_apiKey}&u={_username}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user profile: {StatusCode}", response.StatusCode);
                return Result.Failure<RAUserProfile>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

            var profile = new RAUserProfile(
                Username: json.GetProperty("User").GetString() ?? _username!,
                TotalPoints: json.TryGetProperty("TotalPoints", out var tp) ? tp.GetInt32() : 0,
                TotalTruePoints: json.TryGetProperty("TotalTruePoints", out var ttp) ? ttp.GetInt32() : 0,
                Rank: json.TryGetProperty("Rank", out var rank) ? rank.GetInt32() : 0,
                TotalGamesPlayed: json.TryGetProperty("TotalGamesPlayed", out var tgp) ? tgp.GetInt32() : 0,
                AvatarUrl: json.TryGetProperty("UserPic", out var av) ? av.GetString() : null
            );

            return Result.Success<RAUserProfile>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile");
            return Result.Failure<RAUserProfile>($"Exception fetching user profile: {ex.Message}", ErrorType.ExternalService);
        }
    }

    public async Task<Result<RAGameInfo>> GetGameByHashAsync(string romHash, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<RAGameInfo>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetGameInfoAndUserProgress.php?z={_username}&y={_apiKey}&m={romHash}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<RAGameInfo>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

            if (!json.TryGetProperty("ID", out var idProp) || idProp.ValueKind == JsonValueKind.Null)
            {
                return Result.Failure<RAGameInfo>("Game not found for hash", ErrorType.NotFound);
            }

            var gameInfo = ParseGameInfo(json);
            if (gameInfo == null)
                return Result.Failure<RAGameInfo>("Failed to parse game info", ErrorType.Internal);

            return Result.Success<RAGameInfo>(gameInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up game by hash {Hash}", romHash);
            return Result.Failure<RAGameInfo>($"Exception looking up game: {ex.Message}", ErrorType.ExternalService);
        }
    }

    public async Task<Result<RAGameInfo>> GetGameInfoAsync(int gameId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<RAGameInfo>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetGame.php?z={_username}&y={_apiKey}&i={gameId}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<RAGameInfo>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
            var gameInfo = ParseGameInfo(json);

            if (gameInfo == null)
                return Result.Failure<RAGameInfo>("Failed to parse game info", ErrorType.Internal);

            return Result.Success<RAGameInfo>(gameInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching game info for ID {GameId}", gameId);
            return Result.Failure<RAGameInfo>($"Exception fetching game info: {ex.Message}", ErrorType.ExternalService);
        }
    }

    public async Task<Result<IReadOnlyList<RAAchievement>>> GetGameAchievementsAsync(int gameId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<IReadOnlyList<RAAchievement>>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetGameExtended.php?z={_username}&y={_apiKey}&i={gameId}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<IReadOnlyList<RAAchievement>>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

            if (!json.TryGetProperty("Achievements", out var achievementsJson))
            {
                return Result.Success<IReadOnlyList<RAAchievement>>(Array.Empty<RAAchievement>());
            }

            var achievements = new List<RAAchievement>();

            foreach (var prop in achievementsJson.EnumerateObject())
            {
                var ach = prop.Value;
                achievements.Add(new RAAchievement(
                    Id: ach.GetProperty("ID").GetInt32(),
                    Title: ach.GetProperty("Title").GetString() ?? "Unknown",
                    Description: ach.GetProperty("Description").GetString() ?? "",
                    Points: ach.GetProperty("Points").GetInt32(),
                    BadgeUrl: ach.TryGetProperty("BadgeName", out var badge)
                        ? $"https://media.retroachievements.org/Badge/{badge.GetString()}.png"
                        : null,
                    IsHardcore: false,
                    NumAwarded: ach.TryGetProperty("NumAwarded", out var awarded) ? awarded.GetInt32() : 0,
                    RarityPercent: ach.TryGetProperty("NumAwardedHardcore", out var rarity)
                        ? rarity.GetSingle()
                        : 0f
                ));
            }

            return Result.Success<IReadOnlyList<RAAchievement>>(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching achievements for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<RAAchievement>>($"Exception fetching achievements: {ex.Message}", ErrorType.ExternalService);
        }
    }

    public async Task<Result<RAGameProgress>> GetUserGameProgressAsync(int gameId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<RAGameProgress>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetGameInfoAndUserProgress.php?z={_username}&y={_apiKey}&u={_username}&g={gameId}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<RAGameProgress>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

            var earnedIds = new List<int>();
            if (json.TryGetProperty("Achievements", out var achievements))
            {
                foreach (var prop in achievements.EnumerateObject())
                {
                    var ach = prop.Value;
                    if (ach.TryGetProperty("DateEarned", out var dateEarned) &&
                        dateEarned.ValueKind != JsonValueKind.Null)
                    {
                        earnedIds.Add(ach.GetProperty("ID").GetInt32());
                    }
                }
            }

            var totalAch = json.TryGetProperty("NumAchievements", out var na) ? na.GetInt32() : 0;
            var totalPts = json.TryGetProperty("points_total", out var pt) ? pt.GetInt32() : 0;

            var progress = new RAGameProgress(
                GameId: gameId,
                AchievementsEarned: earnedIds.Count,
                AchievementsTotal: totalAch,
                PointsEarned: earnedIds.Count * 10, // Approximate
                PointsTotal: totalPts,
                CompletionPercentage: totalAch > 0 ? (float)earnedIds.Count / totalAch * 100 : 0,
                EarnedAchievementIds: earnedIds
            );

            return Result.Success<RAGameProgress>(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user progress for game {GameId}", gameId);
            return Result.Failure<RAGameProgress>($"Exception fetching user progress: {ex.Message}", ErrorType.ExternalService);
        }
    }

    public async Task<Result<IReadOnlyList<RAEarnedAchievement>>> GetRecentAchievementsAsync(int count = 50, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return Result.Failure<IReadOnlyList<RAEarnedAchievement>>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var response = await _httpClient.GetAsync(
                $"API_GetUserRecentAchievements.php?z={_username}&y={_apiKey}&u={_username}&c={count}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<IReadOnlyList<RAEarnedAchievement>>($"Upstream API error: {response.StatusCode}", ErrorType.ExternalService);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);

            if (json.ValueKind != JsonValueKind.Array)
            {
                return Result.Success<IReadOnlyList<RAEarnedAchievement>>(Array.Empty<RAEarnedAchievement>());
            }

            var achievements = new List<RAEarnedAchievement>();

            foreach (var item in json.EnumerateArray())
            {
                achievements.Add(new RAEarnedAchievement(
                    AchievementId: item.GetProperty("AchievementID").GetInt32(),
                    Title: item.GetProperty("Title").GetString() ?? "Unknown",
                    GameTitle: item.GetProperty("GameTitle").GetString() ?? "Unknown",
                    Points: item.GetProperty("Points").GetInt32(),
                    EarnedAt: DateTime.Parse(item.GetProperty("Date").GetString() ?? SystemTimeProvider.Instance.UtcNow.ToString()),
                    IsHardcore: item.TryGetProperty("HardcoreMode", out var hc) && hc.GetInt32() == 1
                ));
            }

            return Result.Success<IReadOnlyList<RAEarnedAchievement>>(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent achievements");
            return Result.Failure<IReadOnlyList<RAEarnedAchievement>>($"Exception fetching recent achievements: {ex.Message}", ErrorType.ExternalService);
        }
    }

    private static RAGameInfo? ParseGameInfo(JsonElement json)
    {
        try
        {
            return new RAGameInfo(
                Id: json.GetProperty("ID").GetInt32(),
                Title: json.GetProperty("Title").GetString() ?? "Unknown",
                ConsoleName: json.TryGetProperty("ConsoleName", out var cn) ? cn.GetString() ?? "" : "",
                ConsoleId: json.TryGetProperty("ConsoleID", out var cid) ? cid.GetInt32() : 0,
                ImageIcon: json.TryGetProperty("ImageIcon", out var img)
                    ? $"https://retroachievements.org{img.GetString()}"
                    : null,
                TotalAchievements: json.TryGetProperty("NumAchievements", out var na) ? na.GetInt32() : 0,
                TotalPoints: json.TryGetProperty("points_total", out var pt) ? pt.GetInt32() : 0
            );
        }
        catch
        {
            return null;
        }
    }
}

