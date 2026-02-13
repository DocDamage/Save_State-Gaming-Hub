namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Collective dream data.
/// </summary>
public class CollectiveDream
{
    public string DreamId { get; set; } = default!;
    public IReadOnlyList<string> PlayerIds { get; set; } = default!;
    public string ArenaId { get; set; } = default!;
    public DreamEmotionalState SharedEmotionalState { get; set; } = default!;
    public IReadOnlyList<SymbolicElement> ManifestedElements { get; set; } = default!;
    public DreamTheme DreamTheme { get; set; } = default!;
    public DateTime InitiatedAt { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public float CoherenceLevel { get; set; } = default!;
}

/// <summary>
/// Collective dream creation request.
/// </summary>
public class CollectiveDreamRequest
{
    public IReadOnlyList<string> PlayerIds { get; set; } = default!;
    public string ArenaId { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Dream emotional state data.
/// </summary>
public class DreamEmotionalState
{
    public string CharacterId { get; set; } = default!;
    public DreamEmotion PrimaryEmotion { get; set; } = default!;
    public float Intensity { get; set; } = default!;
}
