namespace SaveState.Core.FeatureFlags;

/// <summary>
/// Manages feature flags for enabling/disabling features dynamically.
/// </summary>
public interface IFeatureManager
{
    /// <summary>
    /// Checks if a feature is enabled.
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);

    /// <summary>
    /// Checks if a feature is enabled for a specific context.
    /// </summary>
    Task<bool> IsEnabledAsync<TContext>(string featureName, TContext context, CancellationToken ct = default);

    /// <summary>
    /// Gets all feature states.
    /// </summary>
    Task<IReadOnlyDictionary<string, bool>> GetAllFeaturesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets feature definition.
    /// </summary>
    Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName, CancellationToken ct = default);
}

/// <summary>
/// Feature definition metadata.
/// </summary>
public class FeatureDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool DefaultValue { get; set; }
    public FeatureStage Stage { get; set; } = FeatureStage.Production;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Feature development stage.
/// </summary>
public enum FeatureStage
{
    Development,
    Beta,
    Production,
    Deprecated
}
