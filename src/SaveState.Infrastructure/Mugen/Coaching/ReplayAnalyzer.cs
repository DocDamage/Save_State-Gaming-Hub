using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.Coaching;

/// <summary>
/// Analyzer for MUGEN replay files to extract gameplay insights and coaching suggestions.
/// Acts as a coordinator for specialized engines.
/// </summary>
public sealed class ReplayAnalyzer
{
    private readonly IReplayPathResolver _pathResolver;
    private readonly IReplayParsingEngine _parsingEngine;
    private readonly ISequenceAnalysisEngine _sequenceEngine;
    private readonly IPlayerAnalysisEngine _playerEngine;
    private readonly ICoachingSuggestionEngine _coachingEngine;

    /// <summary>
    /// Creates a new ReplayAnalyzer with default engines.
    /// </summary>
    public ReplayAnalyzer()
    {
        _pathResolver = new ReplayPathResolver();
        _parsingEngine = new ReplayParsingEngine();
        _sequenceEngine = new SequenceAnalysisEngine();
        _playerEngine = new PlayerAnalysisEngine();
        _coachingEngine = new CoachingSuggestionEngine();
    }

    /// <summary>
    /// Creates a new ReplayAnalyzer with injected engines.
    /// </summary>
    public ReplayAnalyzer(
        IReplayPathResolver pathResolver,
        IReplayParsingEngine parsingEngine,
        ISequenceAnalysisEngine sequenceEngine,
        IPlayerAnalysisEngine playerEngine,
        ICoachingSuggestionEngine coachingEngine)
    {
        _pathResolver = pathResolver;
        _parsingEngine = parsingEngine;
        _sequenceEngine = sequenceEngine;
        _playerEngine = playerEngine;
        _coachingEngine = coachingEngine;
    }

    /// <summary>
    /// Resolves a replay path to an actual file path.
    /// </summary>
    public static Result<string> ResolveReplayPath(string replayPath) => ReplayPathResolver.ResolveStatic(replayPath);

    /// <summary>
    /// Analyzes a replay file and returns detailed analysis.
    /// </summary>
    public async Task<ReplayAnalysisResult> AnalyzeAsync(string replayPath, CancellationToken ct)
    {
        var metadata = new ReplayMetadata
        {
            Source = replayPath,
            RecordedAt = GetFileTimestamp(replayPath)
        };

        var events = new List<ReplayEvent>();
        var content = await File.ReadAllTextAsync(replayPath, ct);

        if (!string.IsNullOrWhiteSpace(content))
        {
            if (IsJsonPayload(content))
            {
                _parsingEngine.ParseJsonReplay(content, metadata, events);
            }

            if (events.Count == 0)
            {
                _parsingEngine.ParseTextReplay(content, metadata, events);
            }
        }

        var sequences = _sequenceEngine.BuildSequences(events);
        var players = _playerEngine.BuildPlayerSummaries(events, metadata, sequences);
        var outcome = DetermineOutcome(metadata);

        return new ReplayAnalysisResult(metadata, events, players, sequences, outcome);
    }

    /// <summary>
    /// Builds coaching suggestions based on replay analysis.
    /// </summary>
    public List<string> BuildCoachingSuggestions(ReplayAnalysisResult analysis) => 
        _coachingEngine.BuildCoachingSuggestions(analysis);

    /// <summary>
    /// Builds a coaching prompt for AI analysis.
    /// </summary>
    public string BuildCoachPrompt(ReplayAnalysisResult analysis) => 
        _coachingEngine.BuildCoachPrompt(analysis);

    private static DateTimeOffset? GetFileTimestamp(string replayPath)
    {
        if (!File.Exists(replayPath))
        {
            return null;
        }

        return new DateTimeOffset(File.GetLastWriteTimeUtc(replayPath), TimeSpan.Zero);
    }

    private static bool IsJsonPayload(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static ReplayOutcome DetermineOutcome(ReplayMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.Winner))
        {
            return ReplayOutcome.Unknown;
        }

        if (string.Equals(metadata.Winner, metadata.Player1, StringComparison.OrdinalIgnoreCase) ||
            metadata.Winner.Contains("p1", StringComparison.OrdinalIgnoreCase))
        {
            return ReplayOutcome.Player1Win;
        }

        if (string.Equals(metadata.Winner, metadata.Player2, StringComparison.OrdinalIgnoreCase) ||
            metadata.Winner.Contains("p2", StringComparison.OrdinalIgnoreCase))
        {
            return ReplayOutcome.Player2Win;
        }

        return ReplayOutcome.Unknown;
    }
}
