using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of launch experience manager with cinematic sequences.
/// </summary>
public class LaunchExperienceManager : ILaunchExperienceManager
{
    private readonly IGameRepository _gameRepository;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly ILogger<LaunchExperienceManager> _logger;

    // In-memory storage for launch configurations (can be replaced with repository later)
    private readonly Dictionary<Guid, LaunchExperienceConfig> _launchConfigs = new();

    public LaunchExperienceManager(
        IGameRepository gameRepository,
        IAiOrchestrator aiOrchestrator,
        ISessionTrackingService sessionTrackingService,
        ILogger<LaunchExperienceManager> logger)
    {
        _gameRepository = gameRepository;
        _aiOrchestrator = aiOrchestrator;
        _sessionTrackingService = sessionTrackingService;
        _logger = logger;
    }

    public async Task<Result> ConfigureLaunchExperienceAsync(
        Guid gameId,
        LaunchExperienceConfig config,
        CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure($"Game with ID {gameId} not found");
            }

            _launchConfigs[gameId] = config;
            _logger.LogInformation("Configured launch experience for game {GameId} ({GameTitle})",
                gameId, game.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure launch experience for game {GameId}", gameId);
            return Result.Failure($"Failed to configure launch experience: {ex.Message}");
        }
    }

    public async Task<Result<LaunchSequence>> GenerateLaunchSequenceAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result<LaunchSequence>.Failure($"Game with ID {gameId} not found");
            }

            // Get configuration (use defaults if not configured)
            var config = _launchConfigs.GetValueOrDefault(gameId,
                new LaunchExperienceConfig(
                    ShowGameFacts: true,
                    ShowLastProgress: true,
                    ShowAchievementProgress: false,
                    PlayAmbientMusic: true,
                    MaxIntroDuration: TimeSpan.FromSeconds(15)));

            var steps = new List<LaunchStep>();
            var totalDuration = TimeSpan.Zero;

            // Add game facts step if enabled
            if (config.ShowGameFacts)
            {
                var facts = await GenerateGameFactsAsync(game, ct).ConfigureAwait(false);
                if (facts.Count > 0)
                {
                    steps.Add(new GameFactsStep(facts));
                    totalDuration = totalDuration.Add(TimeSpan.FromSeconds(5));
                }
            }

            // Add progress summary step if enabled
            if (config.ShowLastProgress)
            {
                var progressStep = await GenerateProgressStepAsync(gameId, ct).ConfigureAwait(false);
                if (progressStep != null)
                {
                    steps.Add(progressStep);
                    totalDuration = totalDuration.Add(progressStep.Duration);
                }
            }

            // Add ambient music step if enabled
            if (config.PlayAmbientMusic)
            {
                steps.Add(new AmbientMusicStep(null));
                totalDuration = totalDuration.Add(TimeSpan.FromSeconds(3));
            }

            // Cap total duration
            if (totalDuration > config.MaxIntroDuration)
            {
                totalDuration = config.MaxIntroDuration;
            }

            var sequence = new LaunchSequence(gameId, steps, totalDuration);

            _logger.LogInformation("Generated launch sequence for game {GameId} with {StepCount} steps",
                gameId, steps.Count);

            return Result<LaunchSequence>.Success(sequence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate launch sequence for game {GameId}", gameId);
            return Result<LaunchSequence>.Failure($"Failed to generate launch sequence: {ex.Message}");
        }
    }

    public async Task ExecuteLaunchSequenceAsync(
        LaunchSequence sequence,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Executing launch sequence for game {GameId}", sequence.GameId);

            foreach (var step in sequence.Steps)
            {
                ct.ThrowIfCancellationRequested();

                _logger.LogDebug("Executing step {StepType} for {Duration}",
                    step.Type, step.Duration);

                // In a real implementation, this would trigger UI updates, audio playback, etc.
                // For now, we just simulate the timing
                await Task.Delay(step.Duration, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Completed launch sequence for game {GameId}", sequence.GameId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Launch sequence cancelled for game {GameId}", sequence.GameId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute launch sequence for game {GameId}", sequence.GameId);
            throw new InvalidOperationException($"Failed to execute launch sequence: {ex.Message}", ex);
        }
    }

    public async Task<Result<LaunchExperienceConfig?>> GetLaunchExperienceConfigAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result<LaunchExperienceConfig?>.Failure($"Game with ID {gameId} not found");
            }

            var config = _launchConfigs.GetValueOrDefault(gameId, null);
            return Result<LaunchExperienceConfig?>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get launch experience config for game {GameId}", gameId);
            return Result<LaunchExperienceConfig?>.Failure($"Failed to get config: {ex.Message}");
        }
    }

    public async Task<Result> ResetLaunchExperienceConfigAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure($"Game with ID {gameId} not found");
            }

            _launchConfigs.Remove(gameId);
            _logger.LogInformation("Reset launch experience config for game {GameId}", gameId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset launch experience config for game {GameId}", gameId);
            return Result.Failure($"Failed to reset config: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<string>> GenerateGameFactsAsync(Game game, CancellationToken ct)
    {
        try
        {
            // Use AI to generate interesting facts about the game
            var prompt = $"Generate 2-3 interesting facts about the game '{game.Title}'. " +
                        $"Keep each fact to 1-2 sentences. Focus on unique or lesser-known information.";

            var aiResult = await _aiOrchestrator.GenerateTextAsync(prompt, ct: ct)
                .ConfigureAwait(false);

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Value))
            {
                return Array.Empty<string>();
            }

            // Split the response into individual facts
            var facts = aiResult.Value
                .Split(new[] { '\n', '.', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(fact => !string.IsNullOrWhiteSpace(fact))
                .Select(fact => fact.Trim())
                .Take(3)
                .ToArray();

            return facts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate game facts for {GameTitle}", game.Title);
            return Array.Empty<string>();
        }
    }

    private async Task<ProgressSummaryStep?> GenerateProgressStepAsync(Guid gameId, CancellationToken ct)
    {
        try
        {
            var statsResult = await _sessionTrackingService.GetStatisticsAsync(gameId, ct)
                .ConfigureAwait(false);

            if (!statsResult.IsSuccess || statsResult.Value == null)
            {
                return null;
            }

            var stats = statsResult.Value;
            var achievementsEarned = 0; // Placeholder until achievement system integration

            return new ProgressSummaryStep(stats.TotalPlaytime, achievementsEarned);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate progress step for game {GameId}", gameId);
            return null;
        }
    }
}
