namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the death match simulator.
/// Simulates thousands of matches to predict tournament outcomes.
/// </summary>
public class DeathMatchSimulator : IDeathMatchSimulator
{
    private readonly IMatchPredictionEngine _predictionEngine;
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly IMugenLauncher _launcher;

    public DeathMatchSimulator(
        IMatchPredictionEngine predictionEngine,
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        IMugenLauncher launcher)
    {
        _predictionEngine = predictionEngine;
        _characterRepository = characterRepository;
        _launcher = launcher;
    }

    public async Task<Result<SimulationResult>> SimulateMatchesAsync(
        Guid character1Id,
        Guid character2Id,
        int matchCount = 1000,
        CancellationToken ct = default)
    {
        try
        {
            // Get character entities
            var character1Result = await _characterRepository.GetByIdAsync(character1Id, ct);
            if (character1Result.IsFailure)
                return Result.Failure<SimulationResult>("Character 1 not found");
            var character1 = character1Result.Value!;

            var character2Result = await _characterRepository.GetByIdAsync(character2Id, ct);
            if (character2Result.IsFailure)
                return Result.Failure<SimulationResult>("Character 2 not found");
            var character2 = character2Result.Value!;

            var startTime = DateTime.UtcNow;
            var char1Wins = 0;
            var char2Wins = 0;
            var draws = 0;

            var roundPredictions = new List<RoundPrediction>();

            // Run simulations
            for (var round = 1; round <= 3; round++) // Standard MUGEN is best of 3
            {
                var prediction = await _predictionEngine.PredictMatchAsync(character1, character2, ct);
                if (!prediction.IsSuccess)
                    return Result.Failure<SimulationResult>($"Prediction failed: {prediction.Error}");

                var pred = prediction.Value!;

                // Simulate round based on prediction
                var random = Random.Shared.NextDouble();
                if (random < pred.Character1WinProbability)
                    char1Wins++;
                else if (random < pred.Character1WinProbability + pred.Character2WinProbability)
                    char2Wins++;
                else
                    draws++;

                // Store round prediction
                var winner = char1Wins > char2Wins ? character1.Name :
                           char2Wins > char1Wins ? character2.Name : "Draw";

                var keyFactor = pred.Factors
                    .OrderByDescending(f => Math.Abs(f.Character1Score - f.Character2Score))
                    .FirstOrDefault()?.Name ?? "Unknown";

                roundPredictions.Add(new RoundPrediction(
                    round,
                    pred.Character1WinProbability,
                    winner,
                    keyFactor));
            }

            // Run remaining simulations for overall statistics
            for (var i = 3; i < matchCount; i++)
            {
                var prediction = await _predictionEngine.PredictMatchAsync(character1, character2, ct);
                if (!prediction.IsSuccess)
                    continue;

                var pred = prediction.Value!;
                var random = Random.Shared.NextDouble();

                if (random < pred.Character1WinProbability)
                    char1Wins++;
                else if (random < pred.Character1WinProbability + pred.Character2WinProbability)
                    char2Wins++;
                else
                    draws++;
            }

            var duration = DateTime.UtcNow - startTime;
            var char1WinRate = (float)char1Wins / matchCount;
            var char2WinRate = (float)char2Wins / matchCount;

            // Calculate statistical confidence
            var confidence = CalculateConfidence(matchCount, char1WinRate, char2WinRate);

            var result = new SimulationResult(
                character1.Id,
                character1.Name,
                character2.Id,
                character2.Name,
                matchCount,
                char1Wins,
                char2Wins,
                draws,
                char1WinRate,
                char2WinRate,
                confidence,
                duration,
                roundPredictions);

            return Result.Success<SimulationResult>(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<SimulationResult>($"Simulation failed: {ex.Message}");
        }
    }

    public async Task<Result<TournamentSimulation>> SimulateTournamentAsync(
        IReadOnlyList<Guid> participantIds,
        TournamentFormat format,
        int simulationsPerMatch = 1000,
        CancellationToken ct = default)
    {
        try
        {
            if (participantIds.Count < 2)
                return Result.Failure<TournamentSimulation>("Tournament needs at least 2 participants");

            var participants = new List<MugenCharacter>();
            foreach (var id in participantIds)
            {
                var characterResult = await _characterRepository.GetByIdAsync(id, ct);
                if (characterResult.IsFailure)
                    return Result.Failure<TournamentSimulation>($"Character {id} not found");

                participants.Add(characterResult.Value!);
            }

            var simulationId = Guid.NewGuid();
            var brackets = new List<SimulatedBracket>();
            var winnerCounts = new Dictionary<Guid, int>();

            // Simulate each round
            var currentParticipants = new List<Guid>(participantIds);
            var round = 1;

            while (currentParticipants.Count > 1)
            {
                var roundName = GetRoundName(round, currentParticipants.Count);
                var matches = new List<SimulatedMatch>();

                // Pair up participants for this round
                for (var i = 0; i < currentParticipants.Count; i += 2)
                {
                    if (i + 1 >= currentParticipants.Count) break;

                    var p1Id = currentParticipants[i];
                    var p2Id = currentParticipants[i + 1];

                    var simulation = await SimulateMatchesAsync(p1Id, p2Id, simulationsPerMatch, ct);
                    if (!simulation.IsSuccess)
                        continue;

                    var result = simulation.Value!;
                    var winnerId = result.Character1WinRate > result.Character2WinRate ?
                                  result.Character1Id : result.Character2Id;
                    var confidence = Math.Max(result.Character1WinRate, result.Character2WinRate);

                    matches.Add(new SimulatedMatch(
                        p1Id, p2Id, winnerId, confidence,
                        result.Character1Wins, result.Character2Wins));

                    winnerCounts[winnerId] = winnerCounts.GetValueOrDefault(winnerId) + 1;
                }

                brackets.Add(new SimulatedBracket(round, roundName, matches));

                // Advance winners to next round
                currentParticipants = matches.Select(m => m.PredictedWinnerId).ToList();
                round++;
            }

            // Determine predicted winner
            var predictedWinnerId = winnerCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            var predictedWinnerName = participants.First(p => p.Id == predictedWinnerId).Name;
            var winnerConfidence = (float)winnerCounts[predictedWinnerId] / (brackets.Sum(b => b.Matches.Count));

            // Generate top tournament paths
            var topPaths = GenerateTournamentPaths(participants, brackets);

            var tournamentSimulation = new TournamentSimulation(
                simulationId,
                participantIds,
                format,
                simulationsPerMatch,
                brackets,
                predictedWinnerId,
                predictedWinnerName,
                winnerConfidence,
                topPaths,
                DateTime.UtcNow);

            return Result.Success<TournamentSimulation>(tournamentSimulation);
        }
        catch (Exception ex)
        {
            return Result.Failure<TournamentSimulation>($"Tournament simulation failed: {ex.Message}");
        }
    }

    public async Task<Result<System.Diagnostics.Process>> LaunchPredictedFinalsAsync(
        Guid simulationId,
        MugenEngine engine = MugenEngine.IkemenGo,
        CancellationToken ct = default)
    {
        try
        {
            // For now, we don't persist simulations to a DB, but we could find the simulation
            // if we had a repository. Assuming for now we just want to launch.
            // Since we don't have a SimulationRepository yet, this is a bit tricky.
            // However, the prompt implies "finishing" it.

            return Result.Failure<System.Diagnostics.Process>("Simulation persistence not yet implemented, cannot retrieve results by ID.");
        }
        catch (Exception ex)
        {
            return Result.Failure<System.Diagnostics.Process>($"Failed to launch predicted finals: {ex.Message}");
        }
    }

    private static float CalculateConfidence(int sampleSize, float rate1, float rate2)
    {
        // Simplified confidence calculation using standard error
        var p = (rate1 + rate2) / 2;
        var standardError = Math.Sqrt(p * (1 - p) / sampleSize);
        var difference = Math.Abs(rate1 - rate2);

        // Confidence is roughly 1 - (difference / (2 * standardError))
        var confidence = Math.Clamp(1 - (difference / (4 * standardError)), 0, 1);
        return (float)confidence;
    }

    private static string GetRoundName(int round, int participantCount)
    {
        return participantCount switch
        {
            2 => "Finals",
            4 => "Semi-Finals",
            8 => "Quarter-Finals",
            _ => $"Round {round}"
        };
    }

    private static IReadOnlyList<TournamentPath> GenerateTournamentPaths(
        IReadOnlyList<MugenCharacter> participants,
        IReadOnlyList<SimulatedBracket> brackets)
    {
        // Simplified path generation - in a real implementation this would analyze all possible paths
        var paths = new List<TournamentPath>();

        if (brackets.Any() && brackets.Last().Matches.Any())
        {
            var finalMatch = brackets.Last().Matches.First();
            var winner = participants.First(p => p.Id == finalMatch.PredictedWinnerId);

            paths.Add(new TournamentPath(
                new[] { finalMatch.Participant1Id, finalMatch.Participant2Id, finalMatch.PredictedWinnerId },
                finalMatch.WinConfidence,
                $"{participants.First(p => p.Id == finalMatch.Participant1Id).Name} defeats {participants.First(p => p.Id == finalMatch.Participant2Id).Name}, wins tournament"));
        }

        return paths;
    }
}

