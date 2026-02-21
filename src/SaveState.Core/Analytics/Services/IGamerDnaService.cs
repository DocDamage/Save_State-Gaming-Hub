using SaveState.Core.Common;
using SaveState.Core.Analytics.Models.GamerProfile;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Service for analyzing and managing gamer DNA profiles.
/// </summary>
public interface IGamerDnaService
{
    /// <summary>
    /// Analyzes a user's gaming data to generate a complete DNA profile.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the user's gaming DNA profile.</returns>
    Task<Result<GamerDnaProfile>> AnalyzeProfileAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes a user's profile based on gaming data from a specific time period.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="since">Start date for the analysis period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the user's gaming DNA profile for the specified period.</returns>
    Task<Result<GamerDnaProfile>> AnalyzeProfileWithHistoryAsync(
        Guid userId,
        DateTime since,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the evolution history of a user's gaming DNA over time.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="months">Number of months of history to retrieve (default: 12).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of evolution snapshots.</returns>
    Task<Result<IReadOnlyList<DnaEvolutionSnapshot>>> GetEvolutionHistoryAsync(
        Guid userId,
        int months = 12,
        CancellationToken ct = default);

    /// <summary>
    /// Quickly determines a user's primary gamer archetype.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the primary archetype.</returns>
    Task<Result<GamerArchetype>> DetermineArchetypeAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available gamer archetypes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of all archetypes.</returns>
    Task<Result<IReadOnlyList<GamerArchetype>>> GetAllArchetypesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Triggers a background update of the user's profile.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateProfileAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a shareable profile card for social sharing.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="theme">Visual theme for the card.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the shareable profile card.</returns>
    Task<Result<ShareableProfileCard>> GenerateShareableCardAsync(
        Guid userId,
        ProfileCardTheme theme = ProfileCardTheme.Cyberpunk,
        CancellationToken ct = default);

    /// <summary>
    /// Compares two users' gaming DNA profiles.
    /// </summary>
    /// <param name="userId1">First user's identifier.</param>
    /// <param name="userId2">Second user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a compatibility score (0.0 - 1.0).</returns>
    Task<Result<float>> CompareProfilesAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets quiz questions for determining gamer type.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of quiz questions.</returns>
    Task<Result<IReadOnlyList<GamerTypeQuizQuestion>>> GetQuizQuestionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Processes quiz answers and determines the gamer type.
    /// </summary>
    /// <param name="answers">The user's answers to quiz questions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the quiz result with primary archetype.</returns>
    Task<Result<GamerTypeQuizResult>> ProcessQuizAnswersAsync(
        IReadOnlyList<(GamerArchetype Archetype, int Weight)> answers,
        CancellationToken ct = default);
}
