namespace SaveState.Core.FeatureFlags;

/// <summary>
/// Storage interface for feature flags.
/// </summary>
public interface IFeatureStore
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
    Task<FeatureState?> GetFeatureAsync(string featureName, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, bool>> GetAllFeaturesAsync(CancellationToken ct = default);
    Task SetFeatureAsync(FeatureState feature, CancellationToken ct = default);
}

/// <summary>
/// Feature state with configuration.
/// </summary>
public class FeatureState
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public bool DefaultValue { get; set; }
    public FeatureStage Stage { get; set; } = FeatureStage.Production;
    public List<string> Tags { get; set; } = new();
    
    // Advanced targeting
    public int PercentageRollout { get; set; } = 100;
    public List<string>? TargetUsers { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
