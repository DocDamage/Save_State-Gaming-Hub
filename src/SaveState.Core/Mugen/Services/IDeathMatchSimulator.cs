namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for death match simulation.
/// Simulates thousands of matches between characters to predict tournament outcomes.
/// </summary>
public interface IDeathMatchSimulator
{
    /// <summary>
    /// Runs N simulated matches between two characters using AI prediction.
    /// </summary>
    /// <param name="character1Id">First character ID.</param>
    /// <param name="character2Id">Second character ID.</param>
    /// <param name="matchCount">Number of matches to simulate (default 1000).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The simulation results.</returns>
    Task<Result<SimulationResult>> SimulateMatchesAsync(
        Guid character1Id,
        Guid character2Id,
        int matchCount = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Runs a full tournament simulation with all participants.
    /// </summary>
    /// <param name="participantIds">List of participant character IDs.</param>
    /// <param name="format">Tournament format.</param>
    /// <param name="simulationsPerMatch">Number of simulations per match.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tournament simulation results.</returns>
    Task<Result<TournamentSimulation>> SimulateTournamentAsync(
        IReadOnlyList<Guid> participantIds,
        TournamentFormat format,
        int simulationsPerMatch = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Launches the predicted finals match in actual MUGEN for viewing.
    /// </summary>
    /// <param name="simulationId">The simulation ID to use for predictions.</param>
    /// <param name="engine">The MUGEN engine to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The launched process.</returns>
    Task<Result<System.Diagnostics.Process>> LaunchPredictedFinalsAsync(
        Guid simulationId,
        MugenEngine engine = MugenEngine.IkemenGo,
        CancellationToken ct = default);
}