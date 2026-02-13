using System;

namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Training-specific types.
/// </summary>
public static class TrainingTypes
{
    /// <summary>
    /// Training progress update data.
    /// </summary>
    public record ProgressUpdate(
        int CurrentRound,
        double Accuracy,
        TimeSpan? AverageTime,
        int? SequencesCompleted);

    /// <summary>
    /// Training progress data.
    /// </summary>
    public record TrainingProgressData(
        int CurrentRound,
        int TotalRounds,
        int CorrectResponses,
        int IncorrectResponses,
        TimeSpan AverageResponseTime,
        TimeSpan BestResponseTime);

    /// <summary>
    /// Training response data.
    /// </summary>
    public record TrainingResponse(
        string SessionId,
        bool IsCorrect,
        TimeSpan ResponseTime,
        string Feedback,
        ProgressUpdate ProgressUpdate);
}
