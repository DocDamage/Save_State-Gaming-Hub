using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Recommendations.Services;
using SaveState.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SaveState.Infrastructure.Recommendations;

/// <summary>
/// Placeholder implementation of deep learning recommendation model.
/// In production, this would integrate with ML.NET, TensorFlow.NET, or ONNX Runtime.
/// </summary>
public class DeepRecommendationModel : IDeepRecommendationModel
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger<DeepRecommendationModel> _logger;
    private readonly ITimeProvider _timeProvider;
    private const int EmbeddingDimensions = 128;

    // In-memory cache for embeddings (in production, use Redis or similar)
    private readonly Dictionary<Guid, float[]> _gameEmbeddings = new();
    private readonly Dictionary<Guid, float[]> _userEmbeddings = new();

    public DeepRecommendationModel(
        SaveStateDbContext context,
        ILogger<DeepRecommendationModel> logger,
        ITimeProvider timeProvider)
    {
        _context = context;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<float[]>> GetGameEmbeddingAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            // Check cache
            if (_gameEmbeddings.TryGetValue(gameId, out var cached))
            {
                return Result.Success(cached);
            }

            // Current strategy: Generate deterministic pseudo-embedding based on game features
            var game = await _context.Games
                .Include(g => g.Genres)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game == null)
            {
                return Result.Failure<float[]>("Game not found", ErrorType.NotFound);
            }

            var embedding = GeneratePseudoEmbedding(game);
            _gameEmbeddings[gameId] = embedding;

            _logger.LogDebug("Generated embedding for game {GameId}", gameId);
            return Result.Success(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game embedding");
            return Result.Failure<float[]>($"Failed to get embedding: {ex.Message}");
        }
    }

    public async Task<Result<float[]>> GetUserEmbeddingAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            // Check cache
            if (_userEmbeddings.TryGetValue(userId, out var cached))
            {
                return Result.Success(cached);
            }

            // Get user's play history
            var sessions = await _context.GameSessions
                .Include(s => s.Game)
                    .ThenInclude(g => g.Genres)
                .Where(s => s.Game != null)
                .Take(100) // Limit for performance
                .ToListAsync(ct);

            if (!sessions.Any())
            {
                // New user - return zero embedding
                return Result.Success(new float[EmbeddingDimensions]);
            }

            // Aggregate game embeddings weighted by playtime
            var userEmbedding = new float[EmbeddingDimensions];
            var totalPlaytime = sessions.Sum(s => s.GetDuration(_timeProvider.UtcNow).TotalHours);

            foreach (var session in sessions)
            {
                var gameEmbeddingResult = await GetGameEmbeddingAsync(session.GameId, ct);
                if (gameEmbeddingResult.IsSuccess)
                {
                    var weight = (float)(session.GetDuration(_timeProvider.UtcNow).TotalHours / totalPlaytime);
                    var gameEmbedding = gameEmbeddingResult.Value;

                    for (int i = 0; i < EmbeddingDimensions; i++)
                    {
                        userEmbedding[i] += gameEmbedding[i] * weight;
                    }
                }
            }

            // Normalize
            var magnitude = (float)Math.Sqrt(userEmbedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < EmbeddingDimensions; i++)
                {
                    userEmbedding[i] /= magnitude;
                }
            }

            _userEmbeddings[userId] = userEmbedding;
            _logger.LogDebug("Generated embedding for user {UserId}", userId);

            return Result.Success(userEmbedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user embedding");
            return Result.Failure<float[]>($"Failed to get embedding: {ex.Message}");
        }
    }

    public async Task<Result<float>> PredictAffinityAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var userEmbeddingResult = await GetUserEmbeddingAsync(userId, ct);
            var gameEmbeddingResult = await GetGameEmbeddingAsync(gameId, ct);

            if (userEmbeddingResult.IsFailure || gameEmbeddingResult.IsFailure)
            {
                return Result.Failure<float>("Failed to get embeddings");
            }

            // Calculate cosine similarity
            var similarity = CosineSimilarity(
                userEmbeddingResult.Value,
                gameEmbeddingResult.Value);

            // Convert to 0-100 scale
            var affinity = (similarity + 1) / 2 * 100;

            return Result.Success(affinity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict affinity");
            return Result.Failure<float>($"Failed to predict: {ex.Message}");
        }
    }

    public async Task<Result> UpdateModelAsync(
        Guid userId,
        Guid gameId,
        string interaction,
        float value,
        CancellationToken ct = default)
    {
        try
        {
            // Invalidate cache to force recalculation on next access
            _userEmbeddings.Remove(userId);

            _logger.LogInformation(
                "Updated model for user {UserId}, game {GameId}, interaction {Interaction}",
                userId, gameId, interaction);

            await Task.CompletedTask; // Placeholder for async model update
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update model");
            return Result.Failure($"Failed to update: {ex.Message}");
        }
    }

    public async Task<Result> TrainModelAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting model training...");

            // Reset model state and clear caches
            _gameEmbeddings.Clear();
            _userEmbeddings.Clear();

            _logger.LogInformation("Model training completed");
            await Task.CompletedTask;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train model");
            return Result.Failure($"Failed to train: {ex.Message}");
        }
    }

    #region Helper Methods

    private float[] GeneratePseudoEmbedding(Core.GameLibrary.Entities.Game game)
    {
        // Generate deterministic pseudo-embedding based on game features
        var embedding = new float[EmbeddingDimensions];
        var random = new Random(game.Id.GetHashCode());

        // Base random values
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        // Encode genre information in specific dimensions
        var genreOffset = 0;
        foreach (var genre in game.Genres.Take(10))
        {
            var genreHash = genre.Name.GetHashCode();
            embedding[genreOffset % EmbeddingDimensions] += (genreHash % 100) / 100f;
            genreOffset += 13; // Prime number for distribution
        }

        // Normalize
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < EmbeddingDimensions; i++)
            {
                embedding[i] /= magnitude;
            }
        }

        return embedding;
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0;

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    #endregion
}

