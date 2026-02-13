namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a parsed MUGEN roster file (select.def) with ordered entries.
/// </summary>
public sealed record MugenRoster(
    IReadOnlyList<MugenRosterEntry> Entries,
    IReadOnlyList<string> HeaderLines,
    IReadOnlyList<string> FooterLines);

/// <summary>
/// Represents a single roster entry within select.def.
/// </summary>
public sealed record MugenRosterEntry(
    MugenRosterEntryType EntryType,
    string? CharacterPath,
    string? StagePath,
    string? Category,
    string? RawLine);

/// <summary>
/// Entry type for roster items.
/// </summary>
public enum MugenRosterEntryType
{
    Category,
    Character,
    Comment
}
