namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Manages tournament brackets, including seed generation and winner advancement.
/// </summary>
public class TournamentBracketManager
{
    /// <summary>
    /// Generates the initial matches for a single elimination tournament.
    /// </summary>
    public static List<TournamentMatchEntity> GenerateSingleEliminationMatches(
        Guid tournamentId,
        IReadOnlyList<TournamentParticipant> participants)
    {
        var count = participants.Count;
        if (count < 2) return new List<TournamentMatchEntity>();

        // Calculate next power of 2
        var size = 1;
        while (size < count) size *= 2;

        var matches = new List<TournamentMatchEntity>();
        var rounds = (int)Math.Log2(size);

        // Sort participants by seed
        var sortedParticipants = participants.OrderBy(p => p.Seed).ToList();

        // Round 1 matches
        var matchCountInRound = size / 2;
        for (var i = 0; i < matchCountInRound; i++)
        {
            var p1Index = i;
            var p2Index = size - 1 - i;

            var p1Id = p1Index < count ? sortedParticipants[p1Index].CharacterId : (Guid?)null;
            var p2Id = p2Index < count ? sortedParticipants[p2Index].CharacterId : (Guid?)null;

            matches.Add(TournamentMatchEntity.Create(tournamentId, 1, i + 1, p1Id, p2Id));
        }

        // Remaining rounds (empty matches)
        for (var round = 2; round <= rounds; round++)
        {
            matchCountInRound /= 2;
            for (var i = 0; i < matchCountInRound; i++)
            {
                matches.Add(TournamentMatchEntity.Create(tournamentId, round, i + 1, null, null));
            }
        }

        return matches;
    }

    /// <summary>
    /// Advances a winner to their next match in a single elimination bracket.
    /// </summary>
    public static void AdvanceWinner(MugenTournament tournament, TournamentMatchEntity completedMatch)
    {
        if (completedMatch.Status != MatchStatus.Completed || completedMatch.WinnerId == null)
            return;

        // Current round and match number
        var round = completedMatch.Round;
        var matchNum = completedMatch.MatchNumber;

        // Determine next match round and number
        var nextRound = round + 1;
        var nextMatchNum = (matchNum + 1) / 2;
        var isPlayer1 = matchNum % 2 != 0;

        // Find the next match
        var nextMatch = tournament.Matches.FirstOrDefault(m => m.Round == nextRound && m.MatchNumber == nextMatchNum);

        if (nextMatch != null)
        {
            if (isPlayer1)
                nextMatch.SetPlayer1(completedMatch.WinnerId.Value);
            else
                nextMatch.SetPlayer2(completedMatch.WinnerId.Value);
        }
        else if (isLastMatch(tournament, completedMatch))
        {
            // Tournament completed - requires ITimeProvider from caller
            // For now, use SystemTimeProvider as a fallback since this is a static method
            tournament.Complete(completedMatch.WinnerId.Value, SystemTimeProvider.Instance);
        }
    }

    private static bool isLastMatch(MugenTournament tournament, TournamentMatchEntity match)
    {
        var maxRound = tournament.Matches.Max(m => m.Round);
        return match.Round == maxRound && match.MatchNumber == 1;
    }
}
