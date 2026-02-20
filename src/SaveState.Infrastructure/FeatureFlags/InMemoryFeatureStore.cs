using Microsoft.Extensions.Caching.Memory;
using SaveState.Core.Common.Services;
using SaveState.Core.FeatureFlags;

namespace SaveState.Infrastructure.FeatureFlags;

/// <summary>
/// In-memory feature store for development/testing.
/// </summary>
public class InMemoryFeatureStore : IFeatureStore
{
    private readonly Dictionary<string, FeatureState> _features = new();
    private readonly IMemoryCache _cache;
    private readonly ITimeProvider _timeProvider;

    public InMemoryFeatureStore(IMemoryCache cache, ITimeProvider timeProvider)
    {
        _cache = cache;
        _timeProvider = timeProvider;
        
        // Seed with default features
        SeedDefaultFeatures();
    }

    private void SeedDefaultFeatures()
    {
        _features["NewGameLauncher"] = new FeatureState
        {
            Name = "NewGameLauncher",
            DisplayName = "New Game Launcher",
            Description = "Use the new game launcher UI",
            Enabled = false,
            DefaultValue = false,
            Stage = FeatureStage.Beta,
            Tags = new List<string> { "ui", "launcher" }
        };

        _features["CloudSyncV2"] = new FeatureState
        {
            Name = "CloudSyncV2",
            DisplayName = "Cloud Sync V2",
            Description = "Improved cloud synchronization",
            Enabled = true,
            DefaultValue = true,
            Stage = FeatureStage.Production,
            Tags = new List<string> { "cloud", "sync" }
        };

        _features["AiAssistant"] = new FeatureState
        {
            Name = "AiAssistant",
            DisplayName = "AI Assistant",
            Description = "AI-powered game recommendations",
            Enabled = true,
            DefaultValue = true,
            Stage = FeatureStage.Production,
            Tags = new List<string> { "ai", "recommendations" }
        };
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        if (_features.TryGetValue(featureName, out var feature))
        {
            // Check date restrictions
            if (feature.StartDate.HasValue && feature.StartDate.Value > _timeProvider.UtcNow)
                return Task.FromResult(false);
            
            if (feature.EndDate.HasValue && feature.EndDate.Value < _timeProvider.UtcNow)
                return Task.FromResult(false);
            
            return Task.FromResult(feature.Enabled);
        }

        return Task.FromResult(false);
    }

    public Task<FeatureState?> GetFeatureAsync(string featureName, CancellationToken ct = default)
    {
        _features.TryGetValue(featureName, out var feature);
        return Task.FromResult(feature);
    }

    public Task<IReadOnlyDictionary<string, bool>> GetAllFeaturesAsync(CancellationToken ct = default)
    {
        var result = _features.ToDictionary(
            f => f.Key,
            f => f.Value.Enabled);
        
        return Task.FromResult<IReadOnlyDictionary<string, bool>>(result);
    }

    public Task SetFeatureAsync(FeatureState feature, CancellationToken ct = default)
    {
        _features[feature.Name] = feature;
        _cache.Remove($"feature_state:{feature.Name}");
        return Task.CompletedTask;
    }
}
