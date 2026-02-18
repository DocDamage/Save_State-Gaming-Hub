namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Generates coaching suggestions from replay analysis.
/// </summary>
public interface ICoachingSuggestionEngine
{
    /// <summary>
    /// Builds coaching suggestions based on replay analysis.
    /// </summary>
    List<string> BuildCoachingSuggestions(ReplayAnalysisResult analysis);

    /// <summary>
    /// Builds a coaching prompt for AI analysis.
    /// </summary>
    string BuildCoachPrompt(ReplayAnalysisResult analysis);
}
