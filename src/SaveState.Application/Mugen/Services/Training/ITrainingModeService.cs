using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Interface for training mode service providing comprehensive skill development tools.
/// </summary>
public interface ITrainingModeService
{
    #region Session Management

    /// <summary>
    /// Starts a new reflex training session.
    /// </summary>
    Task<Result<TrainingSession>> StartReflexTrainingAsync(
        string userId,
        ReflexTrainingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a new pattern recognition training session.
    /// </summary>
    Task<Result<TrainingSession>> StartPatternRecognitionAsync(
        string userId,
        PatternRecognitionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a new combo lab session.
    /// </summary>
    Task<Result<TrainingSession>> StartComboLabAsync(
        string userId,
        ComboLabRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an active training session by ID.
    /// </summary>
    Task<Result<TrainingSession>> GetTrainingSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Ends a training session and saves results.
    /// </summary>
    Task<Result> EndTrainingSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    #endregion

    #region Input Processing

    /// <summary>
    /// Processes training input for an active session.
    /// </summary>
    Task<Result<TrainingTypes.TrainingResponse>> ProcessTrainingInputAsync(
        string sessionId,
        TrainingInput input,
        CancellationToken ct = default);

    #endregion

    #region Statistics and Recommendations

    /// <summary>
    /// Gets training statistics for a user over a specified period.
    /// </summary>
    Task<Result<TrainingStatistics>> GetTrainingStatisticsAsync(
        string userId,
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Generates personalized training recommendations.
    /// </summary>
    Task<Result<TrainingRecommendations>> GetTrainingRecommendationsAsync(
        string userId,
        CancellationToken ct = default);

    #endregion
}
