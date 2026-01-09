namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for managing MUGEN tournaments.
/// </summary>
public interface IMugenTournamentService
{
    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="request">The tournament creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created tournament.</returns>
    Task<Result<MugenTournament>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the bracket for a tournament.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tournament bracket.</returns>
    Task<Result<TournamentBracket>> GetBracketAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Adds a participant to a tournament.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="characterId">The participant character ID.</param>
    /// <param name="seed">Optional seeding position.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> AddParticipantAsync(Guid tournamentId, Guid characterId, int? seed = null, CancellationToken ct = default);

    /// <summary>
    /// Records the result of a tournament match.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="matchId">The match ID.</param>
    /// <param name="result">The match result.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RecordMatchResultAsync(Guid tournamentId, Guid matchId, MatchResult result, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TournamentStanding>>> GetStandingsAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Starts the tournament by generating the bracket and initial matches.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default);
}

/// <summary>
/// Request to create a new tournament.
/// </summary>
public sealed record CreateTournamentRequest(
    string Name,
    TournamentFormat Format,
    IReadOnlyList<Guid> ParticipantIds);
