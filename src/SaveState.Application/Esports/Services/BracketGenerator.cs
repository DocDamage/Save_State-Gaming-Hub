using SaveState.Core.Esports.Models;

namespace SaveState.Application.Esports.Services;

/// <summary>
/// Interface for generating tournament brackets.
/// </summary>
public interface IBracketGenerator
{
    /// <summary>
    /// Generates a single elimination bracket.
    /// </summary>
    Bracket GenerateSingleElimination(List<Participant> participants, bool randomizeSeeds = false);

    /// <summary>
    /// Generates a double elimination bracket.
    /// </summary>
    Bracket GenerateDoubleElimination(List<Participant> participants, bool randomizeSeeds = false);

    /// <summary>
    /// Generates a round robin bracket.
    /// </summary>
    Bracket GenerateRoundRobin(List<Participant> participants);

    /// <summary>
    /// Generates a Swiss system bracket.
    /// </summary>
    Bracket GenerateSwiss(List<Participant> participants, int rounds = 0);
}

/// <summary>
/// Implementation of bracket generation algorithms.
/// </summary>
public sealed class BracketGenerator : IBracketGenerator
{
    private readonly Random _random = new();

    /// <inheritdoc />
    public Bracket GenerateSingleElimination(List<Participant> participants, bool randomizeSeeds = false)
    {
        var bracket = new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = new List<BracketRound>(),
            Matches = new List<Match>()
        };

        var orderedParticipants = OrderParticipants(participants, randomizeSeeds);
        var participantCount = orderedParticipants.Count;
        
        // Calculate number of rounds needed
        var rounds = (int)Math.Ceiling(Math.Log2(participantCount));
        bracket.TotalRounds = rounds;

        // Calculate byes needed
        var bracketSize = (int)Math.Pow(2, rounds);
        var byeCount = bracketSize - participantCount;

        // Create first round matches with byes
        var firstRoundMatches = new List<Match>();
        var currentRoundMatches = new List<Match>();
        int participantIndex = 0;

        for (int i = 0; i < bracketSize / 2; i++)
        {
            var match = new Match
            {
                Id = Guid.NewGuid(),
                Round = 1,
                MatchNumber = i + 1,
                Status = MatchStatus.Scheduled,
                IsWinnersBracket = true,
                Games = new List<MatchGame>()
            };

            // Assign participants or byes
            if (i < byeCount)
            {
                // This match has a bye - only one participant
                match.Player1 = orderedParticipants[participantIndex++];
                match.Winner = match.Player1;
                match.Status = MatchStatus.Completed;
            }
            else
            {
                // Normal match with two participants
                match.Player1 = orderedParticipants[participantIndex++];
                if (participantIndex < participantCount)
                {
                    match.Player2 = orderedParticipants[participantIndex++];
                }
            }

            firstRoundMatches.Add(match);
            bracket.Matches.Add(match);
        }

        // Create first round
        if (firstRoundMatches.Count > 0)
        {
            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = 1,
                Name = GetRoundName(1, rounds),
                Type = BracketType.Winners,
                Matches = firstRoundMatches,
                IsComplete = false
            });
        }

        currentRoundMatches = firstRoundMatches;

        // Generate subsequent rounds
        for (int round = 2; round <= rounds; round++)
        {
            var nextRoundMatches = new List<Match>();
            var matchesInRound = (int)Math.Pow(2, rounds - round);

            for (int i = 0; i < matchesInRound; i++)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = i + 1,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = true,
                    Games = new List<MatchGame>()
                };

                // Link previous matches
                if (currentRoundMatches.Count > i * 2)
                {
                    match.Player1 = currentRoundMatches[i * 2].Winner;
                    currentRoundMatches[i * 2].NextMatchWin = match.Id;
                }
                if (currentRoundMatches.Count > i * 2 + 1)
                {
                    match.Player2 = currentRoundMatches[i * 2 + 1].Winner;
                    currentRoundMatches[i * 2 + 1].NextMatchWin = match.Id;
                }

                nextRoundMatches.Add(match);
                bracket.Matches.Add(match);
            }

            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = GetRoundName(round, rounds),
                Type = round == rounds ? BracketType.GrandFinals : BracketType.Winners,
                Matches = nextRoundMatches,
                IsComplete = false
            });

            currentRoundMatches = nextRoundMatches;
        }

        // Set champion from final match
        if (currentRoundMatches.Count > 0)
        {
            bracket.Champion = currentRoundMatches[0].Winner;
        }

        return bracket;
    }

    /// <inheritdoc />
    public Bracket GenerateDoubleElimination(List<Participant> participants, bool randomizeSeeds = false)
    {
        var bracket = new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = new List<BracketRound>(),
            Matches = new List<Match>()
        };

        var orderedParticipants = OrderParticipants(participants, randomizeSeeds);
        var participantCount = orderedParticipants.Count;
        
        // Calculate winners bracket rounds
        var winnersRounds = (int)Math.Ceiling(Math.Log2(participantCount));
        var bracketSize = (int)Math.Pow(2, winnersRounds);
        var byeCount = bracketSize - participantCount;

        // Generate Winners Bracket
        var winnersMatches = GenerateWinnersBracket(orderedParticipants, byeCount, bracket);
        
        // Generate Losers Bracket
        var losersMatches = GenerateLosersBracket(winnersMatches, winnersRounds, bracket);

        // Generate Grand Finals
        var grandFinals = GenerateGrandFinals(winnersMatches, losersMatches, winnersRounds, bracket);

        bracket.TotalRounds = winnersRounds * 2;

        // Set champion
        if (grandFinals != null && grandFinals.Winner != null)
        {
            bracket.Champion = grandFinals.Winner;
        }

        return bracket;
    }

    private List<Match> GenerateWinnersBracket(List<Participant> participants, int byeCount, Bracket bracket)
    {
        var matches = new List<Match>();
        var participantIndex = 0;
        var rounds = (int)Math.Ceiling(Math.Log2(participants.Count + byeCount));

        // First round
        var firstRoundMatches = new List<Match>();
        var bracketSize = (int)Math.Pow(2, rounds);

        for (int i = 0; i < bracketSize / 2; i++)
        {
            var match = new Match
            {
                Id = Guid.NewGuid(),
                Round = 1,
                MatchNumber = i + 1,
                Status = MatchStatus.Scheduled,
                IsWinnersBracket = true,
                Games = new List<MatchGame>()
            };

            if (i < byeCount)
            {
                match.Player1 = participants[participantIndex++];
                match.Winner = match.Player1;
                match.Status = MatchStatus.Completed;
            }
            else
            {
                match.Player1 = participants[participantIndex++];
                if (participantIndex < participants.Count)
                {
                    match.Player2 = participants[participantIndex++];
                }
            }

            firstRoundMatches.Add(match);
            bracket.Matches.Add(match);
        }

        bracket.Rounds.Add(new BracketRound
        {
            RoundNumber = 1,
            Name = "Winners Round 1",
            Type = BracketType.Winners,
            Matches = firstRoundMatches,
            IsComplete = false
        });

        var currentRoundMatches = firstRoundMatches;

        // Generate subsequent winners bracket rounds
        for (int round = 2; round <= rounds; round++)
        {
            var nextRoundMatches = new List<Match>();
            var matchesInRound = (int)Math.Pow(2, rounds - round);

            for (int i = 0; i < matchesInRound; i++)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = i + 1,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = true,
                    Games = new List<MatchGame>()
                };

                if (currentRoundMatches.Count > i * 2)
                {
                    currentRoundMatches[i * 2].NextMatchWin = match.Id;
                }
                if (currentRoundMatches.Count > i * 2 + 1)
                {
                    currentRoundMatches[i * 2 + 1].NextMatchWin = match.Id;
                }

                nextRoundMatches.Add(match);
                bracket.Matches.Add(match);
            }

            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Winners Round {round}",
                Type = BracketType.Winners,
                Matches = nextRoundMatches,
                IsComplete = false
            });

            currentRoundMatches = nextRoundMatches;
        }

        return currentRoundMatches;
    }

    private Match? GenerateLosersBracket(List<Match> winnersMatches, int winnersRounds, Bracket bracket)
    {
        // This is a simplified losers bracket implementation
        // In a full implementation, losers from each winners round would drop down
        var losersMatches = new List<Match>();
        var previousLosersRound = new List<Match>();

        for (int round = 1; round < winnersRounds; round++)
        {
            var currentLosersRound = new List<Match>();
            var matchesInRound = (int)Math.Pow(2, winnersRounds - round - 1);

            for (int i = 0; i < matchesInRound; i++)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = i + 1,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = false,
                    Games = new List<MatchGame>()
                };

                currentLosersRound.Add(match);
                bracket.Matches.Add(match);
            }

            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Losers Round {round}",
                Type = BracketType.Losers,
                Matches = currentLosersRound,
                IsComplete = false
            });

            previousLosersRound = currentLosersRound;
        }

        return previousLosersRound.FirstOrDefault();
    }

    private Match? GenerateGrandFinals(List<Match> winnersMatches, Match? losersFinalist, int winnersRounds, Bracket bracket)
    {
        var grandFinals = new Match
        {
            Id = Guid.NewGuid(),
            Round = winnersRounds + 1,
            MatchNumber = 1,
            Status = MatchStatus.Scheduled,
            IsWinnersBracket = true,
            Games = new List<MatchGame>()
        };

        bracket.Rounds.Add(new BracketRound
        {
            RoundNumber = winnersRounds + 1,
            Name = "Grand Finals",
            Type = BracketType.GrandFinals,
            Matches = new List<Match> { grandFinals },
            IsComplete = false
        });

        bracket.Matches.Add(grandFinals);

        return grandFinals;
    }

    /// <inheritdoc />
    public Bracket GenerateRoundRobin(List<Participant> participants)
    {
        var bracket = new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = new List<BracketRound>(),
            Matches = new List<Match>(),
            TotalRounds = participants.Count - 1
        };

        var orderedParticipants = OrderParticipants(participants, false);
        var participantCount = orderedParticipants.Count;

        // Generate all matches using circle method
        // If odd number of participants, add a bye
        var hasBye = participantCount % 2 != 0;
        var effectiveCount = hasBye ? participantCount + 1 : participantCount;
        var rounds = effectiveCount - 1;

        for (int round = 1; round <= rounds; round++)
        {
            var roundMatches = new List<Match>();

            for (int i = 0; i < effectiveCount / 2; i++)
            {
                var player1Index = i;
                var player2Index = effectiveCount - 1 - i;

                // Skip if either is a bye
                if (hasBye && (player1Index == participantCount || player2Index == participantCount))
                    continue;

                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = i + 1,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = true,
                    Player1 = orderedParticipants[player1Index],
                    Player2 = orderedParticipants[player2Index],
                    Games = new List<MatchGame>()
                };

                roundMatches.Add(match);
                bracket.Matches.Add(match);
            }

            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Round {round}",
                Type = BracketType.Winners,
                Matches = roundMatches,
                IsComplete = false
            });

            // Rotate participants for next round (keep first participant fixed)
            if (round < rounds)
            {
                var last = orderedParticipants[orderedParticipants.Count - 1];
                for (int i = orderedParticipants.Count - 1; i > 1; i--)
                {
                    orderedParticipants[i] = orderedParticipants[i - 1];
                }
                orderedParticipants[1] = last;
            }
        }

        return bracket;
    }

    /// <inheritdoc />
    public Bracket GenerateSwiss(List<Participant> participants, int rounds = 0)
    {
        var bracket = new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = new List<BracketRound>(),
            Matches = new List<Match>()
        };

        var orderedParticipants = OrderParticipants(participants, false);
        var participantCount = orderedParticipants.Count;

        // Default to log2(n) rounds if not specified
        if (rounds <= 0)
        {
            rounds = (int)Math.Ceiling(Math.Log2(participantCount));
        }

        bracket.TotalRounds = rounds;

        // Swiss system: players are paired each round based on their current record
        // Round 1: Random or seeded pairing
        for (int round = 1; round <= rounds; round++)
        {
            var roundMatches = new List<Match>();
            var matchesInRound = participantCount / 2;

            for (int i = 0; i < matchesInRound; i++)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = i + 1,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = true,
                    Games = new List<MatchGame>()
                };

                // Round 1: Use initial seeding
                // Later rounds: Would pair by record (players with same wins play each other)
                if (round == 1)
                {
                    var player1Index = i * 2;
                    var player2Index = i * 2 + 1;

                    if (player1Index < participantCount)
                        match.Player1 = orderedParticipants[player1Index];
                    if (player2Index < participantCount)
                        match.Player2 = orderedParticipants[player2Index];
                }
                // For subsequent rounds, we'd need to track records and pair accordingly
                // This is a simplified implementation

                roundMatches.Add(match);
                bracket.Matches.Add(match);
            }

            bracket.Rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Swiss Round {round}",
                Type = BracketType.Winners,
                Matches = roundMatches,
                IsComplete = false
            });
        }

        return bracket;
    }

    private List<Participant> OrderParticipants(List<Participant> participants, bool randomize)
    {
        var ordered = new List<Participant>(participants);

        if (randomize)
        {
            // Shuffle using Fisher-Yates
            for (int i = ordered.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
            }
        }
        else
        {
            // Sort by seed if available, otherwise by registration time
            ordered = ordered
                .OrderBy(p => p.Seed ?? int.MaxValue)
                .ThenBy(p => p.RegisteredAt)
                .ToList();
        }

        return ordered;
    }

    private static string GetRoundName(int round, int totalRounds)
    {
        var roundsFromEnd = totalRounds - round + 1;
        
        return roundsFromEnd switch
        {
            1 => "Finals",
            2 => "Semi-finals",
            3 => "Quarter-finals",
            4 => "Round of 16",
            5 => "Round of 32",
            6 => "Round of 64",
            _ => $"Round {round}"
        };
    }
}
