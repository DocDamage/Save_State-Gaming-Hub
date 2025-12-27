using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Mugen
{
    public enum BattleMode { Versus, Tournament, Survival, BossRush, Tag, TimeTrial }
    
    public enum MoveType { Normal, Overhead, Low, Throw, Projectile, Invincible, Armor, Launcher, Super }

    public class BattleStats
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Special { get; set; }
    }

    public class Move
    {
        public string Name { get; set; } = string.Empty;
        public MoveType Type { get; set; }
        public int Startup { get; set; }      // Startup frames
        public int Active { get; set; }       // Active frames
        public int Recovery { get; set; }     // Recovery frames
        public int Advantage { get; set; }    // On block
        public int Damage { get; set; }
        public int MeterCost { get; set; }
        public bool CanCancel { get; set; }
    }

    public class BattleCharacter
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceGame { get; set; } = string.Empty;
        public BattleStats Stats { get; set; } = new();
        public string AIPattern { get; set; } = string.Empty;
        public int AILevel { get; set; } = 5;  // 1-10
        public int ColorPalette { get; set; } = 1;  // 1-12
        public List<Move> Moveset { get; set; } = new();
        public int WinCount { get; set; }
        public int LossCount { get; set; }
    }

    public class Combo
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Inputs { get; set; } = new();
        public int Damage { get; set; }
        public int Hits { get; set; }
        public int Difficulty { get; set; }  // 1-10
    }

    public class BattleRound
    {
        public int RoundNumber { get; set; }
        public string Winner { get; set; } = string.Empty;
        public int P1HealthRemaining { get; set; }
        public int P2HealthRemaining { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> HighlightMoves { get; set; } = new();
        public string? LlmCommentary { get; set; }
    }

    public class BattleMatch
    {
        public string Id { get; set; } = string.Empty;
        public BattleCharacter Player1 { get; set; } = new();
        public BattleCharacter Player2 { get; set; } = new();
        public string Stage { get; set; } = string.Empty;
        public BattleMode Mode { get; set; }
        public string Winner { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<BattleRound> Rounds { get; set; } = new();
        public int P1Score { get; set; }
        public int P2Score { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public int MaxCombo { get; set; }
        public bool IsPerfect { get; set; }
        public string? LlmPrediction { get; set; }
        public string? LlmPostMatchAnalysis { get; set; }
    }

    public class TournamentBracket
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<BattleCharacter> Participants { get; set; } = new();
        public List<BattleMatch> Matches { get; set; } = new();
        public int CurrentRound { get; set; }
        public string? Champion { get; set; }
        public bool IsComplete { get; set; }
    }

    public class CrossGameBattleService
    {
        private List<BattleCharacter> _roster = new();
        private List<BattleMatch> _history = new();
        private TournamentBracket? _currentTournament;
        private readonly string _dataPath;
        private readonly ILlmService? _llmService;
        private readonly Random _rand = new();

        // Stage options
        public static readonly string[] Stages = { 
            "Training Stage", "City Arena", "Forest Clearing", "Space Station",
            "Volcano Summit", "Underwater Temple", "Sky Platform", "Dark Realm"
        };

        public CrossGameBattleService(ILlmService? llmService = null)
        {
            _llmService = llmService;
            _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                "SaveState2", "MUGEN", "data");
            if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
            LoadHistory();
        }

        public List<BattleCharacter> GetRoster() => _roster;
        public List<BattleMatch> GetHistory() => _history;
        public TournamentBracket? GetCurrentTournament() => _currentTournament;

        public void AddToBattleRoster(MugenFighter fighter, string sourceGame = "MUGEN")
        {
            if (_roster.Any(x => x.Name == fighter.Name)) return;

            var stats = DeriveStats(fighter, sourceGame);
            var character = new BattleCharacter
            {
                Id = Guid.NewGuid().ToString(),
                Name = fighter.Name,
                SourceGame = sourceGame,
                Stats = stats,
                AIPattern = DetermineAIPattern(stats),
                AILevel = 5,
                Moveset = GenerateMoveset(fighter.Name)
            };

            _roster.Add(character);
        }

        private List<Move> GenerateMoveset(string characterName)
        {
            int seed = characterName.GetHashCode();
            var r = new Random(seed);
            
            return new List<Move>
            {
                new Move { Name = "Light Punch", Type = MoveType.Normal, Startup = 4, Active = 3, Recovery = 9, Advantage = 2, Damage = 30, CanCancel = true },
                new Move { Name = "Heavy Punch", Type = MoveType.Normal, Startup = 8, Active = 4, Recovery = 18, Advantage = -2, Damage = 80, CanCancel = false },
                new Move { Name = "Low Kick", Type = MoveType.Low, Startup = 5, Active = 3, Recovery = 11, Advantage = 0, Damage = 35, CanCancel = true },
                new Move { Name = "Overhead Slam", Type = MoveType.Overhead, Startup = 24, Active = 3, Recovery = 15, Advantage = 4, Damage = 60, CanCancel = false },
                new Move { Name = "Throw", Type = MoveType.Throw, Startup = 5, Active = 2, Recovery = 30, Advantage = 0, Damage = 100, CanCancel = false },
                new Move { Name = "Launcher", Type = MoveType.Launcher, Startup = 12, Active = 4, Recovery = 25, Advantage = -8, Damage = 50, CanCancel = true },
                new Move { Name = "Fireball", Type = MoveType.Projectile, Startup = 14, Active = 60, Recovery = 30, Advantage = 10, Damage = 70, MeterCost = 0, CanCancel = false },
                new Move { Name = "Dragon Punch", Type = MoveType.Invincible, Startup = 3, Active = 12, Recovery = 35, Advantage = -20, Damage = 120, MeterCost = 0, CanCancel = false },
                new Move { Name = "Super Move", Type = MoveType.Super, Startup = 8, Active = 30, Recovery = 40, Advantage = -30, Damage = 300, MeterCost = 1000, CanCancel = false },
            };
        }

        private BattleStats DeriveStats(MugenFighter fighter, string sourceGame)
        {
            var baseStats = GetGenreBasedStats(sourceGame);
            int nameHash = GetStableHash(fighter.Name);
            var rand = new Random(nameHash);
            
            return new BattleStats
            {
                Health = Clamp(baseStats.Health + rand.Next(-15, 16), 40, 120),
                Attack = Clamp(baseStats.Attack + rand.Next(-15, 16), 40, 120),
                Defense = Clamp(baseStats.Defense + rand.Next(-15, 16), 40, 120),
                Speed = Clamp(baseStats.Speed + rand.Next(-15, 16), 40, 120),
                Special = Clamp(baseStats.Special + rand.Next(-15, 16), 40, 120)
            };
        }

        private BattleStats GetGenreBasedStats(string sourceGame)
        {
            var game = sourceGame.ToLowerInvariant();
            if (game.Contains("street fighter") || game.Contains("tekken")) 
                return new BattleStats { Health = 85, Attack = 90, Defense = 75, Speed = 85, Special = 80 };
            if (game.Contains("mario") || game.Contains("sonic")) 
                return new BattleStats { Health = 75, Attack = 70, Defense = 70, Speed = 95, Special = 75 };
            if (game.Contains("dragon ball") || game.Contains("anime")) 
                return new BattleStats { Health = 80, Attack = 85, Defense = 70, Speed = 85, Special = 95 };
            return new BattleStats { Health = 80, Attack = 80, Defense = 80, Speed = 80, Special = 80 };
        }

        private string DetermineAIPattern(BattleStats stats)
        {
            if (stats.Attack > stats.Defense + 10 && stats.Speed > 80) return "aggressive";
            if (stats.Defense > stats.Attack + 10 && stats.Health > 85) return "defensive";
            if (stats.Special > 90) return "zoner";
            return "balanced";
        }

        // Battle simulation with rounds
        public BattleMatch SimulateMatch(BattleCharacter p1, BattleCharacter p2, string stage, BattleMode mode, int bestOf = 3)
        {
            var match = new BattleMatch
            {
                Id = Guid.NewGuid().ToString(),
                Player1 = p1,
                Player2 = p2,
                Stage = stage,
                Mode = mode,
                Timestamp = DateTime.Now,
                Rounds = new()
            };

            int p1Wins = 0, p2Wins = 0;
            int roundsNeeded = (bestOf / 2) + 1;
            int roundNum = 0;

            while (p1Wins < roundsNeeded && p2Wins < roundsNeeded)
            {
                roundNum++;
                var round = SimulateRound(p1, p2, roundNum);
                match.Rounds.Add(round);

                if (round.Winner == p1.Name) p1Wins++;
                else p2Wins++;
            }

            match.P1Score = p1Wins;
            match.P2Score = p2Wins;
            match.Winner = p1Wins > p2Wins ? p1.Name : p2.Name;
            match.TotalDuration = TimeSpan.FromSeconds(match.Rounds.Sum(r => r.Duration.TotalSeconds));
            match.MaxCombo = _rand.Next(3, 15);
            match.IsPerfect = match.Rounds.Any(r => 
                (r.Winner == p1.Name && r.P1HealthRemaining == 100) ||
                (r.Winner == p2.Name && r.P2HealthRemaining == 100));

            // Update win/loss records
            if (match.Winner == p1.Name) { p1.WinCount++; p2.LossCount++; }
            else { p2.WinCount++; p1.LossCount++; }

            _history.Add(match);
            SaveHistory();
            return match;
        }

        private BattleRound SimulateRound(BattleCharacter p1, BattleCharacter p2, int roundNum)
        {
            double p1Power = CalculatePower(p1);
            double p2Power = CalculatePower(p2);
            double total = p1Power + p2Power;

            bool p1Wins = _rand.NextDouble() < (p1Power / total);
            int winnerHealth = _rand.Next(10, 100);

            return new BattleRound
            {
                RoundNumber = roundNum,
                Winner = p1Wins ? p1.Name : p2.Name,
                P1HealthRemaining = p1Wins ? winnerHealth : 0,
                P2HealthRemaining = p1Wins ? 0 : winnerHealth,
                Duration = TimeSpan.FromSeconds(_rand.Next(30, 99)),
                HighlightMoves = new List<string> { p1.Moveset[_rand.Next(p1.Moveset.Count)].Name }
            };
        }

        private double CalculatePower(BattleCharacter c)
        {
            double base_ = (c.Stats.Attack + c.Stats.Speed + c.Stats.Health + c.Stats.Defense + c.Stats.Special) / 5.0;
            double aiBonus = c.AILevel * 2;
            return base_ + aiBonus + _rand.NextDouble() * 20;
        }

        // Tournament system
        public TournamentBracket CreateTournament(string name, List<BattleCharacter> participants)
        {
            // Pad to power of 2
            int size = 2;
            while (size < participants.Count) size *= 2;
            
            var padded = participants.ToList();
            while (padded.Count < size) padded.Add(new BattleCharacter { Name = "BYE", Id = "bye-" + Guid.NewGuid() });

            _currentTournament = new TournamentBracket
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Participants = padded,
                CurrentRound = 1
            };

            return _currentTournament;
        }

        public BattleMatch? PlayNextTournamentMatch(string stage)
        {
            if (_currentTournament == null || _currentTournament.IsComplete) return null;

            var remaining = _currentTournament.Participants
                .Where(p => !p.Name.StartsWith("BYE"))
                .Where(p => !_currentTournament.Matches
                    .Where(m => m.Winner != p.Name)
                    .Select(m => m.Player1.Name == p.Name ? m.Player1.Name : m.Player2.Name)
                    .Contains(p.Name))
                .ToList();

            if (remaining.Count < 2)
            {
                _currentTournament.IsComplete = true;
                _currentTournament.Champion = remaining.FirstOrDefault()?.Name;
                return null;
            }

            var p1 = remaining[0];
            var p2 = remaining[1];

            var match = SimulateMatch(p1, p2, stage, BattleMode.Tournament);
            _currentTournament.Matches.Add(match);

            return match;
        }

        // Survival mode
        public List<BattleMatch> PlaySurvival(BattleCharacter player, int maxOpponents = 10)
        {
            var results = new List<BattleMatch>();
            var currentPlayer = player;
            
            for (int i = 0; i < maxOpponents && currentPlayer.Stats.Health > 0; i++)
            {
                var opponent = _roster[_rand.Next(_roster.Count)];
                if (opponent.Id == player.Id) continue;

                var match = SimulateMatch(currentPlayer, opponent, Stages[_rand.Next(Stages.Length)], BattleMode.Survival, 1);
                results.Add(match);

                if (match.Winner != player.Name) break;
            }

            return results;
        }

        private int GetStableHash(string input)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in input) hash = hash * 31 + c;
                return Math.Abs(hash);
            }
        }

        private int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        // LLM-powered features
        public async Task<string?> GetMatchPredictionAsync(BattleCharacter p1, BattleCharacter p2, string stage)
        {
            if (_llmService?.IsAvailable != true) return null;

            var prompt = $@"Predict this fighting game match (15 words max):
{p1.Name} ({p1.SourceGame}) - ATK:{p1.Stats.Attack} DEF:{p1.Stats.Defense} SPD:{p1.Stats.Speed} - Style:{p1.AIPattern} - Record:{p1.WinCount}W/{p1.LossCount}L
vs
{p2.Name} ({p2.SourceGame}) - ATK:{p2.Stats.Attack} DEF:{p2.Stats.Defense} SPD:{p2.Stats.Speed} - Style:{p2.AIPattern} - Record:{p2.WinCount}W/{p2.LossCount}L
Stage: {stage}

Give prediction with winner and key factor.";

            return await _llmService.CompleteAsync(prompt, 
                "You are a fighting game analyst. Be concise and insightful.");
        }

        public async Task<string?> GetRoundCommentaryAsync(BattleRound round, BattleCharacter p1, BattleCharacter p2)
        {
            if (_llmService?.IsAvailable != true) return null;

            var isClose = Math.Abs(round.P1HealthRemaining - round.P2HealthRemaining) < 20;
            var isPerfect = round.Winner == p1.Name ? round.P1HealthRemaining == 100 : round.P2HealthRemaining == 100;
            
            var context = isPerfect ? "PERFECT round" : (isClose ? "extremely close round" : "round");
            var highlight = round.HighlightMoves.FirstOrDefault() ?? "unknown move";

            var prompt = $"Round {round.RoundNumber}: {round.Winner} wins this {context} using {highlight}! Write exciting 12-word commentary.";

            return await _llmService.CompleteAsync(prompt,
                "You are an enthusiastic fighting game commentator. Be hype!");
        }

        public async Task<string?> GetPostMatchAnalysisAsync(BattleMatch match)
        {
            if (_llmService?.IsAvailable != true) return null;

            var prompt = $@"Analyze this match result (20 words max):
Winner: {match.Winner}
Score: {match.P1Score}-{match.P2Score}
Max Combo: {match.MaxCombo} hits
Perfect Round: {(match.IsPerfect ? "Yes" : "No")}
Duration: {match.TotalDuration.TotalSeconds:F0}s
Rounds: {match.Rounds.Count}

Give brief analysis of what decided the match.";

            return await _llmService.CompleteAsync(prompt,
                "You are a fighting game analyst providing post-match breakdowns.");
        }

        public async Task<string?> GetTournamentCommentaryAsync(TournamentBracket tournament)
        {
            if (_llmService?.IsAvailable != true || tournament == null) return null;

            var remaining = tournament.Participants.Count - tournament.Matches.Count;
            var status = tournament.IsComplete 
                ? $"Champion: {tournament.Champion}" 
                : $"{remaining} fighters remain";

            var prompt = $"Tournament '{tournament.Name}' update: {tournament.Matches.Count} matches complete. {status}. Write 15-word hype commentary.";

            return await _llmService.CompleteAsync(prompt,
                "You are a tournament announcer building excitement.");
        }

        public async Task<string?> GenerateCharacterIntroAsync(BattleCharacter character)
        {
            if (_llmService?.IsAvailable != true) return null;

            var prompt = $@"Write a 20-word fighting game character intro for:
Name: {character.Name}
Origin: {character.SourceGame}
Style: {character.AIPattern}
Stats: ATK {character.Stats.Attack}, DEF {character.Stats.Defense}, SPD {character.Stats.Speed}
Record: {character.WinCount} wins, {character.LossCount} losses

Make it dramatic and intimidating!";

            return await _llmService.CompleteAsync(prompt,
                "You are a fighting game announcer introducing fighters dramatically.");
        }

        private void LoadHistory()
        {
            var path = Path.Combine(_dataPath, "battle_history.json");
            if (File.Exists(path))
            {
                try { _history = JsonSerializer.Deserialize<List<BattleMatch>>(File.ReadAllText(path)) ?? new(); }
                catch { _history = new(); }
            }
        }

        private void SaveHistory()
        {
            var path = Path.Combine(_dataPath, "battle_history.json");
            File.WriteAllText(path, JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void ClearHistory() { _history.Clear(); SaveHistory(); }
    }
}

