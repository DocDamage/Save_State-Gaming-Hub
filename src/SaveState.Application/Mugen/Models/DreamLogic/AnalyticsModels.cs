namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Dream analytics data.
/// </summary>
public class DreamAnalytics
{
    public string ArenaId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public int TotalSurrealEvents { get; set; } = default!;
    public int GeometryTransformations { get; set; } = default!;
    public int SymbolicManifestations { get; set; } = default!;
    public int CollectiveDreamsHosted { get; set; } = default!;
    public float AverageStability { get; set; } = default!;
    public string MostCommonSurrealEvent { get; set; } = default!;
    public EmotionalImpact PlayerEmotionalImpact { get; set; } = default!;
    public float DreamCoherenceIndex { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Emotional impact metrics.
/// </summary>
public class EmotionalImpact
{
    public float AverageEmotionalIntensity { get; set; } = default!;
    public string MostCommonEmotion { get; set; } = default!;
    public float EmotionalVariety { get; set; } = default!;
    public float PositiveEmotionalRatio { get; set; } = default!;
}
