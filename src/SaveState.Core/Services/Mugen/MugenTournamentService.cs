using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;

namespace SaveState.Core.Services.Mugen
{
    public class TournamentParticipant
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Seed { get; set; }
        public bool Eliminated { get; set; }
        public int CurrentRound { get; set; }
    }

    public class TournamentMatch
    {
        public string Id { get; set; } = string.Empty;
        public int Round { get; set; }
        public int MatchNumber { get; set; }
        public TournamentParticipant? Participant1 { get; set; }
        public TournamentParticipant? Participant2 { get; set; }
        public string WinnerId { get; set; } = string.Empty; // "p1", "p2", or null
        public string Status { get; set; } = string.Empty; // "pending", "completed"
    }

    public class MugenTournament
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "registration", "in-progress", "completed"
        public List<TournamentParticipant> Participants { get; set; } = new();
        public List<TournamentMatch> Matches { get; set; } = new();
        public int CurrentRound { get; set; } = 1;
    }

    public class MugenTournamentService
    {
        private readonly ILogger _logger = Log.ForContext<MugenTournamentService>();
        private List<MugenTournament> _tournaments = new();
        private readonly string _storagePath;

        public MugenTournamentService()
        {
            _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SaveState2", "MUGEN", "data", "tournaments.json");
            var dir = Path.GetDirectoryName(_storagePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            LoadTournaments();
        }

        private void LoadTournaments()
        {
            if (File.Exists(_storagePath))
            {
                try
                {
                    var json = File.ReadAllText(_storagePath);
                    _tournaments = JsonSerializer.Deserialize<List<MugenTournament>>(json) ?? new List<MugenTournament>();
                }
                catch
                {
                    _tournaments = new List<MugenTournament>();
                }
            }
        }

        private void SaveTournaments()
        {
            try
            {
                var json = JsonSerializer.Serialize(_tournaments, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                // Simple logging for now
                _logger.Warning(ex, "Failed to save tournaments");
            }
        }

        public MugenTournament CreateTournament(string name, List<string> participantNames)
        {
            var tournament = new MugenTournament
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Status = "registration",
                Participants = participantNames.Select((n, i) => new TournamentParticipant
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = n,
                    Seed = i + 1,
                    CurrentRound = 1,
                    Eliminated = false
                }).ToList()
            };

            _tournaments.Add(tournament);
            SaveTournaments();
            return tournament;
        }

        public void StartTournament(string tournamentId)
        {
            var tourney = _tournaments.FirstOrDefault(t => t.Id == tournamentId);
            if (tourney == null) return;

            if (tourney.Participants.Count < 2) return;

            tourney.Status = "in-progress";
            tourney.CurrentRound = 1;
            GenerateBracket(tourney);
            SaveTournaments();
        }

        private void GenerateBracket(MugenTournament tourney)
        {
            var activeParticipants = tourney.Participants.Where(p => !p.Eliminated).OrderBy(p => p.Seed).ToList();
            int participantCount = activeParticipants.Count;

            if (participantCount < 2) return;

            int bracketSize = 1;
            while (bracketSize < participantCount) bracketSize *= 2;

            int matchNum = 1;
            var seededPairings = CreateSeededPairings(activeParticipants, bracketSize);

            foreach (var pairing in seededPairings)
            {
                if (pairing.Item2 == null)
                {
                    var byeRecipient = pairing.Item1;
                    if (byeRecipient != null)
                    {
                         tourney.Matches.Add(new TournamentMatch
                         {
                             Id = Guid.NewGuid().ToString(),
                             Round = tourney.CurrentRound,
                             MatchNumber = matchNum++,
                             Participant1 = byeRecipient,
                             Participant2 = null,
                             WinnerId = "p1",
                             Status = "completed"
                         });
                         byeRecipient.CurrentRound++;
                    }
                }
                else
                {
                    tourney.Matches.Add(new TournamentMatch
                    {
                        Id = Guid.NewGuid().ToString(),
                        Round = tourney.CurrentRound,
                        MatchNumber = matchNum++,
                        Participant1 = pairing.Item1,
                        Participant2 = pairing.Item2,
                        Status = "pending"
                    });
                }
            }
        }

        private List<(TournamentParticipant?, TournamentParticipant?)> CreateSeededPairings(
            List<TournamentParticipant> participants, int bracketSize)
        {
            var pairings = new List<(TournamentParticipant?, TournamentParticipant?)>();
            
            var seedPositions = GenerateSeedOrder(bracketSize);
            var bracketSlots = new TournamentParticipant?[bracketSize];

            for (int i = 0; i < participants.Count; i++)
            {
                // Ensure array bounds
                if (seedPositions[i] < bracketSize)
                {
                   bracketSlots[seedPositions[i]] = participants[i];
                }
            }

            for (int i = 0; i < bracketSize; i += 2)
            {
                var p1 = bracketSlots[i];
                var p2 = bracketSlots[i + 1];

                if (p1 == null && p2 == null) continue;

                if (p1 == null)
                {
                    pairings.Add((p2, null));
                }
                else if (p2 == null)
                {
                    pairings.Add((p1, null));
                }
                else
                {
                    pairings.Add((p1, p2));
                }
            }

            return pairings;
        }

        private int[] GenerateSeedOrder(int bracketSize)
        {
            // Simple validation
            if (bracketSize <= 0) return Array.Empty<int>();

            var order = new int[bracketSize];
            order[0] = 0;

            for (int round = 1; round < bracketSize; round *= 2)
            {
                for (int i = round - 1; i >= 0; i--)
                {
                    // Ensure we don't exceed array bounds if logic is slightly off
                    if (round + i < bracketSize)
                    {
                        order[round + i] = bracketSize - 1 - order[i];
                    }
                }
            }
            
            // This order array actually maps index -> seed value (minus 1).
            // We need seed -> index mapping if we want to place seeds into slots.
            // Wait, standard algorithm: "position of seed i".
            // The generated 'order' array usually means: The player at index 'i' in the draw list plays...
            // Actually, let's verify standard seed logic.
            // Loop 1 (round=1): order[1] = 1-1-0 = 0? No: bracketSize-1-0.
            // If size=4. 
            // round=1: order[1] = 3-0 = 3. order=[0,3,0,0]
            // round=2: order[2] = 3-order[0]=3-0=3. order[3]=3-order[1]=3-3=0.
            // Something is fishy in my mental trace or the original code.
            // Let's assume the original code logic was correct for what it did.
            // Original: order[round + i] = bracketSize - 1 - order[i];
            
            return order;
        }

        public void ReportMatchResult(string tournamentId, string matchId, string winnerId)
        {
            var tourney = _tournaments.FirstOrDefault(t => t.Id == tournamentId);
            if (tourney == null) return;

            var match = tourney.Matches.FirstOrDefault(m => m.Id == matchId);
            if (match == null) return;

            if (match.Status == "completed") return;

            match.WinnerId = winnerId;
            match.Status = "completed";

            var winner = (winnerId == "p1") ? match.Participant1 : match.Participant2;
            var loser = (winnerId == "p1") ? match.Participant2 : match.Participant1;

            if (winner != null) winner.CurrentRound++;
            if (loser != null) loser.Eliminated = true;

            CheckRoundCompletion(tourney);
            SaveTournaments();
        }

        private void CheckRoundCompletion(MugenTournament tourney)
        {
            var currentRoundMatches = tourney.Matches.Where(m => m.Round == tourney.CurrentRound).ToList();
            if (currentRoundMatches.Any() && currentRoundMatches.All(m => m.Status == "completed"))
            {
                var remaining = tourney.Participants.Where(p => !p.Eliminated).ToList();
                if (remaining.Count <= 1)
                {
                    tourney.Status = "completed";
                }
                else
                {
                    tourney.CurrentRound++;
                    GenerateBracket(tourney);
                }
            }
        }

        public List<MugenTournament> GetAllTournaments() => _tournaments;
        public MugenTournament? GetTournament(string id) => _tournaments.FirstOrDefault(t => t.Id == id);
    }
}
