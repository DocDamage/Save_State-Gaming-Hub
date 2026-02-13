using Microsoft.Extensions.Logging;
using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.RetroArch.Services;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for RetroAchievements integration.
/// </summary>
public partial class RetroAchievementsEngine : IRetroAchievementsEngine
{
    private readonly ILogger<RetroAchievementsEngine> _logger;
    private readonly IRetroAchievementsClient? _client;

    public RetroAchievementsEngine(
        ILogger<RetroAchievementsEngine> logger,
        IRetroAchievementsClient? client = null)
    {
        _logger = logger;
        _client = client;
    }

    /// <inheritdoc />
    public bool IsConfigured => _client != null;

    /// <inheritdoc />
    public bool IsAuthenticated => _client?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public void Initialize(string? username, string? apiKey)
    {
        if (_client == null || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var authenticated = await _client.AuthenticateAsync(username, apiKey).ConfigureAwait(false);

                if (authenticated)
                {
                    LogRetroAchievementsAuthenticated(_logger, username);
                }
                else
                {
                    LogRetroAchievementsAuthFailed(_logger);
                }
            }
            catch (HttpRequestException ex)
            {
                LogRetroAchievementsAuthError(_logger, ex);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Achievement>>> GetAchievementsAsync(string gameHash, CancellationToken ct = default)
    {
        try
        {
            if (_client == null)
            {
                LogRetroAchievementsNotConfigured(_logger);
                return Result.Success<IReadOnlyList<Achievement>>(Array.Empty<Achievement>());
            }

            if (!_client.IsAuthenticated)
            {
                LogRetroAchievementsNotAuthenticated(_logger);
                return Result.Failure<IReadOnlyList<Achievement>>("RetroAchievements client not authenticated", ErrorType.Unauthorized);
            }

            LogFetchingAchievements(_logger, gameHash);

            // Get game info by hash
            var gameInfoResult = await _client.GetGameByHashAsync(gameHash, ct);
            if (gameInfoResult.IsFailure || gameInfoResult.Value == null)
            {
                LogGameNotFoundByHash(_logger, gameHash);
                return Result.Success<IReadOnlyList<Achievement>>(Array.Empty<Achievement>());
            }

            var gameInfo = gameInfoResult.Value;

            // Get achievements for the game
            var achievementsResult = await _client.GetGameAchievementsAsync(gameInfo.Id, ct);
            if (achievementsResult.IsFailure || achievementsResult.Value == null)
            {
                return Result.Failure<IReadOnlyList<Achievement>>(achievementsResult.Error ?? "Failed to fetch achievements");
            }

            // Map RetroAchievements to our Achievement model
            var achievements = achievementsResult.Value
                .Select(ra => new Achievement
                {
                    Id = ra.Id,
                    Title = ra.Title,
                    Description = ra.Description,
                    Points = ra.Points,
                    BadgeUrl = ra.BadgeUrl,
                    IsUnlocked = false,
                    UnlockedAt = null
                })
                .ToList();

            LogAchievementsFetched(_logger, achievements.Count, gameInfo.Title);
            return Result.Success<IReadOnlyList<Achievement>>(achievements);
        }
        catch (Exception ex)
        {
            LogGetAchievementsError(_logger, ex);
            return Result.Failure<IReadOnlyList<Achievement>>($"Error getting achievements: {ex.Message}");
        }
    }

    #region Logging

    [LoggerMessage(EventId = 801, Level = LogLevel.Information, Message = "RetroAchievements authenticated as {Username}")]
    static partial void LogRetroAchievementsAuthenticated(ILogger logger, string username);

    [LoggerMessage(EventId = 802, Level = LogLevel.Warning, Message = "RetroAchievements authentication failed")]
    static partial void LogRetroAchievementsAuthFailed(ILogger logger);

    [LoggerMessage(EventId = 803, Level = LogLevel.Error, Message = "Error authenticating with RetroAchievements")]
    static partial void LogRetroAchievementsAuthError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 804, Level = LogLevel.Warning, Message = "RetroAchievements client not configured")]
    static partial void LogRetroAchievementsNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 805, Level = LogLevel.Warning, Message = "RetroAchievements client not authenticated")]
    static partial void LogRetroAchievementsNotAuthenticated(ILogger logger);

    [LoggerMessage(EventId = 806, Level = LogLevel.Information, Message = "Fetching achievements for game hash: {Hash}")]
    static partial void LogFetchingAchievements(ILogger logger, string hash);

    [LoggerMessage(EventId = 807, Level = LogLevel.Warning, Message = "Game not found for hash: {Hash}")]
    static partial void LogGameNotFoundByHash(ILogger logger, string hash);

    [LoggerMessage(EventId = 808, Level = LogLevel.Information, Message = "Fetched {Count} achievements for game: {GameTitle}")]
    static partial void LogAchievementsFetched(ILogger logger, int count, string gameTitle);

    [LoggerMessage(EventId = 809, Level = LogLevel.Error, Message = "Error getting achievements")]
    static partial void LogGetAchievementsError(ILogger logger, Exception ex);

    #endregion
}
