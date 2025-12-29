namespace SaveState.Application.GameLibrary.DTOs;

public class ImportResult
{
    public IReadOnlyDictionary<string, ProviderResult> ProviderResults { get; set; } = new Dictionary<string, ProviderResult>();
    public int GamesImported { get; set; }
    public int GamesFailed { get; set; }
    public int GamesSkipped { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }

    public bool IsSuccess => GamesFailed == 0;
    public int TotalGames => GamesImported + GamesFailed + GamesSkipped;
}

public class ProviderResult
{
    public bool Success { get; set; }
    public int GamesFound { get; set; }
    public string? Error { get; set; }
}

public class ImportOptions
{
    public bool SkipMetadata { get; set; } = false;
    public bool OverwriteExisting { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public IReadOnlyList<string>? ProviderFilter { get; set; }
}

public class ImportProgress
{
    public ImportStage Stage { get; set; }
    public string? Provider { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public double Progress => Total > 0 ? (double)Current / Total : 0;
}

public enum ImportStage
{
    Discovery,
    Metadata,
    Import,
    Complete
}
