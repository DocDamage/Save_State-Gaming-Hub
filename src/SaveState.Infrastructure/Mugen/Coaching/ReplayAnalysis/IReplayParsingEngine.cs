namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Parses replay files in JSON and text formats.
/// </summary>
public interface IReplayParsingEngine
{
    /// <summary>
    /// Parses a JSON replay file.
    /// </summary>
    void ParseJsonReplay(string json, ReplayMetadata metadata, List<ReplayEvent> events);

    /// <summary>
    /// Parses a text replay file.
    /// </summary>
    void ParseTextReplay(string text, ReplayMetadata metadata, List<ReplayEvent> events);
}
