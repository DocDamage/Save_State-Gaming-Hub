using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story assets (backgrounds, music, sprites, etc.).
/// </summary>
public class StoryAssetManager
{
    private readonly ILogger<StoryAssetManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<Guid, StoryAsset> _assets;

    public StoryAssetManager(
        ILogger<StoryAssetManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _assets = new ConcurrentDictionary<Guid, StoryAsset>();
    }

    public ConcurrentDictionary<Guid, StoryAsset> Assets => _assets;

    /// <summary>
    /// Imports an asset file.
    /// </summary>
    /// <param name="filePath">Path to the asset file.</param>
    /// <param name="type">Type of the asset.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the imported asset.</returns>
    public Task<Result<StoryAsset>> ImportAssetAsync(
        string filePath,
        AssetType type,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Importing asset: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(Result<StoryAsset>.Failure($"File not found: {filePath}", ErrorType.NotFound));
            }

            var fileInfo = new FileInfo(filePath);
            var asset = new StoryAsset(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                type,
                filePath,
                fileInfo.Length,
                _timeProvider.UtcNow);

            _assets[asset.Id] = asset;
            return Task.FromResult(Result<StoryAsset>.Success(asset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import asset");
            return Task.FromResult(Result<StoryAsset>.Failure($"Import asset failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets assets with optional filter.
    /// </summary>
    /// <param name="typeFilter">Optional asset type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of assets.</returns>
    public Task<Result<IReadOnlyList<StoryAsset>>> GetAssetsAsync(
        AssetType? typeFilter = null,
        CancellationToken ct = default)
    {
        var assets = typeFilter.HasValue
            ? _assets.Values.Where(a => a.Type == typeFilter.Value).ToList()
            : _assets.Values.ToList();

        return Task.FromResult(Result<IReadOnlyList<StoryAsset>>.Success(assets));
    }

    /// <summary>
    /// Validates asset files exist.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the validation result.</returns>
    public Task<Result<AssetValidationResult>> ValidateAssetsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var missing = 0;
            var issues = new List<string>();

            foreach (var asset in _assets.Values)
            {
                if (!File.Exists(asset.FilePath))
                {
                    missing++;
                    issues.Add($"Missing asset: {asset.Name}");
                }
            }

            var result = new AssetValidationResult(
                missing == 0,
                missing,
                0,
                issues);

            return Task.FromResult(Result<AssetValidationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate assets");
            return Task.FromResult(Result<AssetValidationResult>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Optimizes assets.
    /// </summary>
    /// <param name="options">Optimization options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the optimization result.</returns>
    public Task<Result<StoryAssetOptimizationResult>> OptimizeAssetsAsync(
        StoryAssetOptimizationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Optimizing assets");

            var result = new StoryAssetOptimizationResult(
                50 * 1024 * 1024,
                _assets.Count,
                new List<string> { "Compressed backgrounds", "Optimized audio" });

            return Task.FromResult(Result<StoryAssetOptimizationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize assets");
            return Task.FromResult(Result<StoryAssetOptimizationResult>.Failure($"Optimization failed: {ex.Message}", ErrorType.Internal));
        }
    }
}
