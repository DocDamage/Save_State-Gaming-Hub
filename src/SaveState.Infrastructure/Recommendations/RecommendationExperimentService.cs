using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.Services;
using SaveState.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SaveState.Infrastructure.Recommendations;

/// <summary>
/// Implementation of A/B testing framework for recommendation algorithms.
/// </summary>
public class RecommendationExperimentService : IRecommendationExperimentService
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger<RecommendationExperimentService> _logger;

    // In-memory storage (in production, use database)
    private static readonly Dictionary<string, Experiment> _experiments = new();
    private static readonly Dictionary<Guid, string> _userAssignments = new();
    private static readonly List<InteractionRecord> _interactions = new();

    public RecommendationExperimentService(
        SaveStateDbContext context,
        ILogger<RecommendationExperimentService> logger)
    {
        _context = context;
        _logger = logger;

        // Initialize default experiment if none exist
        if (!_experiments.Any())
        {
            InitializeDefaultExperiment();
        }
    }

    public Task<Result<ExperimentConfig>> GetUserExperimentAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            // Check if user already assigned
            if (_userAssignments.TryGetValue(userId, out var assignedVariant))
            {
                var experiment = _experiments.Values.FirstOrDefault();
                if (experiment != null && experiment.Variants.TryGetValue(assignedVariant, out var weights))
                {
                    return Task.FromResult(Result.Success(
                        new ExperimentConfig(experiment.Id, assignedVariant, weights)));
                }
            }

            // Assign user to experiment variant
            var activeExperiment = _experiments.Values.FirstOrDefault(e => e.IsActive);
            if (activeExperiment == null)
            {
                // No active experiment - use default weights
                return Task.FromResult(Result.Success(
                    new ExperimentConfig("default", "control", GetDefaultWeights())));
            }

            // Random assignment with even distribution
            var variantKeys = activeExperiment.Variants.Keys.ToList();
            var variantIndex = Math.Abs(userId.GetHashCode()) % variantKeys.Count;
            var selectedVariant = variantKeys[variantIndex];

            _userAssignments[userId] = selectedVariant;
            activeExperiment.UserCounts[selectedVariant]++;

            _logger.LogInformation(
                "Assigned user {UserId} to experiment {ExperimentId}, variant {VariantId}",
                userId, activeExperiment.Id, selectedVariant);

            return Task.FromResult(Result.Success(
                new ExperimentConfig(
                    activeExperiment.Id,
                    selectedVariant,
                    activeExperiment.Variants[selectedVariant])));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user experiment");
            return Task.FromResult(Result.Failure<ExperimentConfig>(
                $"Failed to get experiment: {ex.Message}"));
        }
    }

    public Task<Result> RecordInteractionAsync(
        Guid userId,
        string experimentId,
        Guid gameId,
        bool wasClicked,
        bool wasPlayed,
        CancellationToken ct = default)
    {
        try
        {
            var interaction = new InteractionRecord
            {
                UserId = userId,
                ExperimentId = experimentId,
                GameId = gameId,
                WasClicked = wasClicked,
                WasPlayed = wasPlayed,
                Timestamp = DateTime.UtcNow
            };

            _interactions.Add(interaction);

            _logger.LogDebug(
                "Recorded interaction for user {UserId}, experiment {ExperimentId}, clicked: {Clicked}, played: {Played}",
                userId, experimentId, wasClicked, wasPlayed);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record interaction");
            return Task.FromResult(Result.Failure($"Failed to record: {ex.Message}"));
        }
    }

    public Task<Result<ExperimentResults>> GetExperimentResultsAsync(
        string experimentId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_experiments.TryGetValue(experimentId, out var experiment))
            {
                return Task.FromResult(Result.Failure<ExperimentResults>(
                    "Experiment not found", ErrorType.NotFound));
            }

            var variantMetrics = new Dictionary<string, VariantMetrics>();

            foreach (var variantId in experiment.Variants.Keys)
            {
                var variantInteractions = _interactions
                    .Where(i => i.ExperimentId == experimentId)
                    .Where(i => _userAssignments.TryGetValue(i.UserId, out var v) && v == variantId)
                    .ToList();

                var impressions = variantInteractions.Count;
                var clicks = variantInteractions.Count(i => i.WasClicked);
                var plays = variantInteractions.Count(i => i.WasPlayed);

                var ctr = impressions > 0 ? (float)clicks / impressions : 0;
                var ptr = impressions > 0 ? (float)plays / impressions : 0;

                // Calculate 95% confidence interval for CTR
                var confidenceInterval = impressions > 0
                    ? 1.96f * (float)Math.Sqrt(ctr * (1 - ctr) / impressions)
                    : 0;

                variantMetrics[variantId] = new VariantMetrics(
                    variantId,
                    impressions,
                    clicks,
                    plays,
                    ctr * 100,
                    ptr * 100,
                    confidenceInterval * 100);
            }

            var results = new ExperimentResults(
                experimentId,
                variantMetrics,
                experiment.StartedAt,
                experiment.UserCounts.Values.Sum());

            return Task.FromResult(Result.Success(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get experiment results");
            return Task.FromResult(Result.Failure<ExperimentResults>(
                $"Failed to get results: {ex.Message}"));
        }
    }

    public Task<Result<string>> CreateExperimentAsync(
        string name,
        List<AlgorithmWeights> variants,
        CancellationToken ct = default)
    {
        try
        {
            var experimentId = $"exp_{Guid.NewGuid():N}";

            var variantDict = new Dictionary<string, AlgorithmWeights>();
            for (int i = 0; i < variants.Count; i++)
            {
                variantDict[$"variant_{i}"] = variants[i];
            }

            var experiment = new Experiment
            {
                Id = experimentId,
                Name = name,
                Variants = variantDict,
                IsActive = true,
                StartedAt = DateTime.UtcNow,
                UserCounts = variantDict.Keys.ToDictionary(k => k, _ => 0)
            };

            // Deactivate other experiments
            foreach (var exp in _experiments.Values)
            {
                exp.IsActive = false;
            }

            _experiments[experimentId] = experiment;

            _logger.LogInformation(
                "Created experiment {ExperimentId} with {VariantCount} variants",
                experimentId, variants.Count);

            return Task.FromResult(Result.Success(experimentId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create experiment");
            return Task.FromResult(Result.Failure<string>($"Failed to create: {ex.Message}"));
        }
    }

    #region Helper Methods

    private void InitializeDefaultExperiment()
    {
        var defaultWeights = new Dictionary<string, AlgorithmWeights>
        {
            ["control"] = GetDefaultWeights(),
            ["collaborative_boost"] = new AlgorithmWeights(0.4f, 0.4f, 0.2f, 0.0f, 0.0f),
            ["deep_learning"] = new AlgorithmWeights(0.3f, 0.2f, 0.1f, 0.4f, 0.0f),
            ["diversity"] = new AlgorithmWeights(0.4f, 0.2f, 0.1f, 0.0f, 0.3f)
        };

        _experiments["default"] = new Experiment
        {
            Id = "default",
            Name = "Default Recommendation Algorithm Test",
            Variants = defaultWeights,
            IsActive = true,
            StartedAt = DateTime.UtcNow,
            UserCounts = defaultWeights.Keys.ToDictionary(k => k, _ => 0)
        };
    }

    private AlgorithmWeights GetDefaultWeights()
    {
        return new AlgorithmWeights(
            ContentWeight: 0.5f,
            CollaborativeWeight: 0.3f,
            PopularityWeight: 0.2f,
            DeepLearningWeight: 0.0f,
            DiversityBoost: 0.0f);
    }

    #endregion

    #region Internal Classes

    private class Experiment
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, AlgorithmWeights> Variants { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; }
        public Dictionary<string, int> UserCounts { get; set; } = new();
    }

    private class InteractionRecord
    {
        public Guid UserId { get; set; }
        public string ExperimentId { get; set; } = string.Empty;
        public Guid GameId { get; set; }
        public bool WasClicked { get; set; }
        public bool WasPlayed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}

