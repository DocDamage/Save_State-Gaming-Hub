using SaveState.Core.Common;

namespace SaveState.Core.Mugen.DTOs;

public class MugenNetplayLobby
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Ping { get; set; }
}





public class MugenAssetEntry
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Sprite, Sound, Palette
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class MugenCompatibilityIssue
{
    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Critical, Warning, Info
    public string Description { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string? SuggestedFix { get; set; }
}

public class MugenCompatibilityFix
{
    public string FixType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Applied { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; }
}

public class CompatibilityAnalysisResult
{
    public List<MugenCompatibilityIssue> Issues { get; set; } = new();
}

public class CompatibilityFixResult
{
    public List<MugenCompatibilityFix> Fixes { get; set; } = new();
    public List<MugenCompatibilityIssue> Issues { get; set; } = new(); // Remaining issues
}

public class MugenEloRating
{
    public string CharacterName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate => MatchesPlayed == 0 ? 0 : (double)Wins / MatchesPlayed;
}

public class MugenDiscoveryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long DownloadCount { get; set; }
    public double Rating { get; set; }
    public string Version { get; set; } = string.Empty;
}

public class MugenRosterEntry
{
    public MugenRosterEntryType EntryType { get; set; }
    public string? CharacterPath { get; set; }
    public string? StagePath { get; set; }
    public string? Category { get; set; }
    public string? RawLine { get; set; }

    public MugenRosterEntry() { }

    public MugenRosterEntry(MugenRosterEntryType type, string? charPath, string? stagePath, string? category, string? rawLine)
    {
        EntryType = type;
        CharacterPath = charPath;
        StagePath = stagePath;
        Category = category;
        RawLine = rawLine;
    }
}

public enum MugenRosterEntryType
{
    Character,
    Category,
    Comment,
    Empty,
    Random,
    Locked,
    Hidden
}
