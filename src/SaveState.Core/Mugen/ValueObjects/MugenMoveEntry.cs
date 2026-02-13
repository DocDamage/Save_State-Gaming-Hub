namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a single command/move entry extracted from a character CMD file.
/// </summary>
public sealed record MugenMoveEntry(string Name, string Command, string? Comment);
