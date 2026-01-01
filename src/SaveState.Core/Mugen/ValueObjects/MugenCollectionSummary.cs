namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a summary projection of a MUGEN character collection.
/// Used for read-only list operations where full entity is not needed.
/// </summary>
public sealed record MugenCollectionSummary(
    Guid Id,
    string Name,
    string? Icon,
    int CharacterCount,
    DateTime CreatedAt);
