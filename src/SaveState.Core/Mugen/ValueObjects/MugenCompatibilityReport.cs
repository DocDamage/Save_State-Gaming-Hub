namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents compatibility analysis and applied fixes for a character.
/// </summary>
public sealed record MugenCompatibilityReport(
    IReadOnlyList<MugenCompatibilityIssue> Issues,
    IReadOnlyList<MugenCompatibilityFix> Fixes);

public sealed record MugenCompatibilityIssue(string Code, string Message);

public sealed record MugenCompatibilityFix(string Code, string Message);
