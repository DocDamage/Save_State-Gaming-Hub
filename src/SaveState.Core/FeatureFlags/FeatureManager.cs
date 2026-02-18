using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SaveState.Core.FeatureFlags;

/// <summary>
/// Default implementation of feature manager using configuration and caching.
/// </summary>
public class FeatureManager : IFeatureManager
{
    private readonly IFeatureStore _featureStore;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureManager> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public FeatureManager(
        IFeatureStore featureStore,
        IMemoryCache cache,
        ILogger<FeatureManager> logger)
    {
        _featureStore = featureStore;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        var cacheKey = $"feature:{featureName}";
        
        if (_cache.TryGetValue(cacheKey, out bool cachedValue))
        {
            return cachedValue;
        }

        var isEnabled = await _featureStore.IsEnabledAsync(featureName, ct);
        
        _cache.Set(cacheKey, isEnabled, _cacheDuration);
        
        _logger.LogDebug("Feature {FeatureName} is {Status}", 
            featureName, 
            isEnabled ? "enabled" : "disabled");
        
        return isEnabled;
    }

    public async Task<bool> IsEnabledAsync<TContext>(
        string featureName, 
        TContext context, 
        CancellationToken ct = default)
    {
        // Check if feature has custom logic for this context
        var feature = await _featureStore.GetFeatureAsync(featureName, ct);
        
        if (feature == null)
        {
            return false;
        }

        // If feature uses percentage rollout
        if (feature.PercentageRollout < 100)
        {
            var hash = GetConsistentHash(context?.ToString() ?? "default", featureName);
            if (hash > feature.PercentageRollout)
            {
                return false;
            }
        }

        // Check user targeting
        if (feature.TargetUsers?.Any() == true)
        {
            var userId = GetUserIdFromContext(context);
            if (!string.IsNullOrEmpty(userId) && feature.TargetUsers.Contains(userId))
            {
                return true;
            }
        }

        return await IsEnabledAsync(featureName, ct);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllFeaturesAsync(CancellationToken ct = default)
    {
        return await _featureStore.GetAllFeaturesAsync(ct);
    }

    public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName, CancellationToken ct = default)
    {
        var feature = await _featureStore.GetFeatureAsync(featureName, ct);
        
        if (feature == null) return null;

        return new FeatureDefinition
        {
            Name = feature.Name,
            DisplayName = feature.DisplayName,
            Description = feature.Description,
            DefaultValue = feature.DefaultValue,
            Stage = feature.Stage,
            Tags = feature.Tags
        };
    }

    private static int GetConsistentHash(string input, string salt)
    {
        var combined = input + salt;
        var hash = 0;
        foreach (var c in combined)
        {
            hash = ((hash << 5) - hash) + c;
            hash |= 0;
        }
        return Math.Abs(hash) % 100;
    }

    private static string? GetUserIdFromContext<TContext>(TContext context)
    {
        // Try to extract user ID from common context types
        if (context is string str) return str;
        if (context is Guid guid) return guid.ToString();
        
        var properties = typeof(TContext).GetProperties();
        var userIdProperty = properties.FirstOrDefault(p => 
            p.Name.Contains("UserId", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("User", StringComparison.OrdinalIgnoreCase));
        
        return userIdProperty?.GetValue(context)?.ToString();
    }
}
