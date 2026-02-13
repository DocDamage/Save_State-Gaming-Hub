namespace SaveState.Application.Mugen.Models.CrossPhase;

/// <summary>
/// Mechanic conflict data.
/// </summary>
public class MechanicConflict
{
    public string ConflictId { get; set; } = default!;
    public MechanicType Mechanic1 { get; set; } = default!;
    public MechanicType Mechanic2 { get; set; } = default!;
    public string ConflictType { get; set; } = default!;
    public float Severity { get; set; } = default!;
}

/// <summary>
/// Individual conflict resolution data.
/// </summary>
public class ConflictResolution
{
    public string ConflictId { get; set; } = default!;
    public string ResolutionType { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
}

/// <summary>
/// Mechanic conflict resolution result data.
/// </summary>
public class MechanicConflictResolution
{
    public string SessionId { get; set; } = default!;
    public int ConflictsResolved { get; set; } = default!;
    public int SuccessfulResolutions { get; set; } = default!;
    public IReadOnlyList<ConflictResolution> Resolutions { get; set; } = default!;
    public DateTime ResolutionTimestamp { get; set; } = default!;
}
