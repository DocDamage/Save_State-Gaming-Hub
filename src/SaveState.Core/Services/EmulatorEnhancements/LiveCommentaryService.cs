using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public enum CommentatorPersonality
    {
        HypeCaster,         // Enthusiastic sports announcer
        DocumentaryNarrator,// Nature documentary calm
        WrestlingAnnouncer, // Dramatic "BY GAWD!" energy
        ChessMaster,        // Analytical gameplay breakdown
        MovieTrailerVoice,  // Epic dramatic narration
        WholesomeCoach,     // Supportive encouragement
        RoastMaster         // Sarcastic burns
    }

    public enum GameEventType
    {
        Death,
        LevelComplete,
        BossEncounter,
        BossDefeat,
        PowerUp,
        ComboAchieved,
        CloseCall,
        PerfectRun,
        Speedrun,
        Collectible,
        SecretFound,
        Idle
    }

    public class CommentaryLine
    {
        public string Text { get; set; } = string.Empty;
        public CommentatorPersonality Personality { get; set; }
        public GameEventType TriggerEvent { get; set; }
        public DateTime Timestamp { get; set; }
        public int Priority { get; set; }  // Higher = more important
    }

    public class CustomCommentator
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public Dictionary<GameEventType, string[]> CustomLines { get; set; } = new();
    }

    public class SessionStats
    {
        public int TotalComments { get; set; }
        public int Deaths { get; set; }
        public int Achievements { get; set; }
        public int Combos { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public Dictionary<GameEventType, int> EventCounts { get; set; } = new();
    }

    public class LiveCommentaryService
    {
        private CommentatorPersonality _currentPersonality = CommentatorPersonality.HypeCaster;
        private readonly List<CommentaryLine> _history = new();
        private readonly Queue<CommentaryLine> _priorityQueue = new();
        private readonly List<CustomCommentator> _customCommentators = new();
        private readonly Dictionary<(CommentatorPersonality, GameEventType), string[]> _lines;
        private readonly ILlmService? _llmService;
        private readonly IAdvancedAiService? _advancedAi;
        private readonly Random _rand = new();
        private int _consecutiveDeaths;
        private DateTime _lastComment = DateTime.MinValue;
        private DateTime _sessionStart = DateTime.Now;
        private SessionStats _stats = new();
        private float _volume = 1.0f;
        private bool _isMuted;
        private string? _currentGameTitle;

        public LiveCommentaryService(ILlmService? llmService = null, IAdvancedAiService? advancedAi = null)
        {
            _llmService = llmService;
            _advancedAi = advancedAi;
            _lines = InitializeCommentaryLines();
        }

        /// <summary>
        /// Set the current game for better contextual commentary
        /// </summary>
        public void SetCurrentGame(string gameTitle)
        {
            _currentGameTitle = gameTitle;
        }

        private Dictionary<(CommentatorPersonality, GameEventType), string[]> InitializeCommentaryLines()
        {
            return new Dictionary<(CommentatorPersonality, GameEventType), string[]>
            {
                // Hype Caster
                [(CommentatorPersonality.HypeCaster, GameEventType.Death)] = new[] {
                    "OH NO! DOWN GOES OUR HERO!",
                    "AND THAT'S A DEVASTATING BLOW!",
                    "THE CROWD GOES SILENT... WHAT A LOSS!"
                },
                [(CommentatorPersonality.HypeCaster, GameEventType.LevelComplete)] = new[] {
                    "YEEEES! WHAT A FINISH!",
                    "ABSOLUTELY INCREDIBLE! THE CROWD IS ON THEIR FEET!",
                    "VICTORY! PURE AND TOTAL VICTORY!"
                },
                [(CommentatorPersonality.HypeCaster, GameEventType.BossDefeat)] = new[] {
                    "THE BOSS IS DOWN! I CAN'T BELIEVE WHAT I'M SEEING!",
                    "KNOCKOUT! TOTAL ANNIHILATION!",
                    "WHAT A LEGENDARY TAKEDOWN!"
                },
                [(CommentatorPersonality.HypeCaster, GameEventType.ComboAchieved)] = new[] {
                    "COMBO CITY! THE HITS KEEP COMING!",
                    "UNSTOPPABLE! ABSOLUTELY UNSTOPPABLE!",
                    "THE COMBO COUNTER IS OFF THE CHARTS!"
                },

                // Wrestling Announcer
                [(CommentatorPersonality.WrestlingAnnouncer, GameEventType.Death)] = new[] {
                    "BAH GAWD! THEY KILLED HIM!",
                    "SOMEBODY STOP THE DAMN MATCH!",
                    "THAT MAN HAD A FAMILY!"
                },
                [(CommentatorPersonality.WrestlingAnnouncer, GameEventType.BossDefeat)] = new[] {
                    "AND THE NEW CHAMPION!",
                    "HE'S DONE IT! BY GOD, HE'S DONE IT!",
                    "THE IMPOSSIBLE HAS HAPPENED!"
                },
                [(CommentatorPersonality.WrestlingAnnouncer, GameEventType.CloseCall)] = new[] {
                    "HE KICKED OUT AT 2! THE RESILIENCE!",
                    "SO CLOSE! INCHES AWAY FROM DISASTER!",
                    "THE HEART OF A CHAMPION!"
                },

                // Documentary Narrator
                [(CommentatorPersonality.DocumentaryNarrator, GameEventType.Death)] = new[] {
                    "And here... we observe a rather unfortunate turn of events.",
                    "Nature, as always, proves unforgiving to the unprepared.",
                    "A moment of silence for our fallen subject."
                },
                [(CommentatorPersonality.DocumentaryNarrator, GameEventType.BossEncounter)] = new[] {
                    "And now we witness the apex predator of this digital ecosystem.",
                    "The creature approaches. Truly magnificent.",
                    "Evolution has crafted a perfect challenge."
                },
                [(CommentatorPersonality.DocumentaryNarrator, GameEventType.SecretFound)] = new[] {
                    "Fascinating. A hidden treasure revealed.",
                    "The keen eye discovers what others miss.",
                    "A secret... waiting millennia to be found."
                },

                // Chess Master
                [(CommentatorPersonality.ChessMaster, GameEventType.Death)] = new[] {
                    "A critical miscalculation. The position was lost three moves ago.",
                    "Overextended. This was inevitable.",
                    "The defensive structure collapsed entirely."
                },
                [(CommentatorPersonality.ChessMaster, GameEventType.ComboAchieved)] = new[] {
                    "Brilliant execution. Each input perfectly calculated.",
                    "A forced sequence - no counterplay possible.",
                    "Frame-perfect. The engine would approve."
                },
                [(CommentatorPersonality.ChessMaster, GameEventType.LevelComplete)] = new[] {
                    "Checkmate. As anticipated.",
                    "The endgame technique was flawless.",
                    "Precisely converted. Final analysis: excellent."
                },

                // Movie Trailer Voice
                [(CommentatorPersonality.MovieTrailerVoice, GameEventType.BossEncounter)] = new[] {
                    "IN A WORLD... where evil reigns supreme...",
                    "ONE HERO... STANDS ALONE...",
                    "THIS SUMMER... THE BATTLE BEGINS."
                },
                [(CommentatorPersonality.MovieTrailerVoice, GameEventType.BossDefeat)] = new[] {
                    "AGAINST ALL ODDS... VICTORY.",
                    "THE LEGEND... IS BORN.",
                    "A HERO... EMERGES."
                },

                // Wholesome Coach
                [(CommentatorPersonality.WholesomeCoach, GameEventType.Death)] = new[] {
                    "Hey, it's okay! You're learning!",
                    "Shake it off, champ. You've got this.",
                    "Every pro was once a beginner. Keep going!"
                },
                [(CommentatorPersonality.WholesomeCoach, GameEventType.LevelComplete)] = new[] {
                    "I'M SO PROUD OF YOU!",
                    "See? I knew you could do it!",
                    "That's my player! Amazing work!"
                },
                [(CommentatorPersonality.WholesomeCoach, GameEventType.Idle)] = new[] {
                    "Take your time, no pressure!",
                    "Need a break? Self-care is important!",
                    "I believe in you whenever you're ready!"
                },

                // Roast Master
                [(CommentatorPersonality.RoastMaster, GameEventType.Death)] = new[] {
                    "Ah yes, speedrunning the game over screen. Classic.",
                    "Have you tried... not walking into death?",
                    "I've seen better gameplay from a screen saver."
                },
                [(CommentatorPersonality.RoastMaster, GameEventType.Idle)] = new[] {
                    "Hello? Anyone home? The controller is lonely.",
                    "Loading your brain... please wait...",
                    "Taking a nap on company time, I see."
                },
                [(CommentatorPersonality.RoastMaster, GameEventType.LevelComplete)] = new[] {
                    "Oh, you finally figured it out. Congratulations.",
                    "Even a broken clock is right twice a day.",
                    "Took long enough. Touch grass tier time."
                },
            };
        }

        public void SetPersonality(CommentatorPersonality personality) => _currentPersonality = personality;
        public CommentatorPersonality GetPersonality() => _currentPersonality;
        public void SetVolume(float volume) => _volume = Math.Clamp(volume, 0, 1);
        public float GetVolume() => _volume;
        public void SetMuted(bool muted) => _isMuted = muted;
        public bool IsMuted => _isMuted;
        public SessionStats GetSessionStats() => _stats;

        public async Task<CommentaryLine?> OnEventAsync(GameEventType eventType, Dictionary<string, object>? context = null)
        {
            if (_isMuted) return null;

            // Track stats
            _stats.EventCounts.TryAdd(eventType, 0);
            _stats.EventCounts[eventType]++;
            _stats.SessionDuration = DateTime.Now - _sessionStart;

            switch (eventType)
            {
                case GameEventType.Death: _stats.Deaths++; _consecutiveDeaths++; break;
                case GameEventType.LevelComplete:
                case GameEventType.BossDefeat:
                    _stats.Achievements++;
                    _consecutiveDeaths = 0;
                    break;
                case GameEventType.ComboAchieved: _stats.Combos++; break;
            }

            // Rate limiting
            var timeSinceLast = (DateTime.Now - _lastComment).TotalSeconds;
            if (timeSinceLast < 2 && eventType != GameEventType.Death && eventType != GameEventType.BossDefeat)
                return null;

            // Special case for consecutive deaths
            if (_consecutiveDeaths >= 5)
            {
                _consecutiveDeaths = 0;
                return CreateLine("Maybe take a short break? Hydrate! Touch some grass!", eventType, priority: 10);
            }

            // Try LLM-generated commentary first if available and important event
            if (_llmService?.IsAvailable == true && IsPriorityEvent(eventType) && timeSinceLast > 5)
            {
                try
                {
                    var llmComment = await GenerateLlmCommentaryAsync(eventType, context);
                    if (!string.IsNullOrEmpty(llmComment))
                    {
                        return CreateLine(llmComment, eventType, priority: 8);
                    }
                }
                catch { /* Fall through to static lines */ }
            }

            // Use static lines
            var key = (_currentPersonality, eventType);
            if (!_lines.TryGetValue(key, out var options) || options.Length == 0)
            {
                key = (CommentatorPersonality.HypeCaster, eventType);
                if (!_lines.TryGetValue(key, out options) || options.Length == 0)
                    return null;
            }

            var text = options[_rand.Next(options.Length)];
            return CreateLine(text, eventType, priority: GetEventPriority(eventType));
        }

        // REMOVED: Synchronous wrapper removed to eliminate deadlock risk
        // Use OnEventAsync directly instead

        private bool IsPriorityEvent(GameEventType type) =>
            type is GameEventType.BossDefeat or GameEventType.PerfectRun or GameEventType.SecretFound;

        private int GetEventPriority(GameEventType type) => type switch
        {
            GameEventType.BossDefeat => 10,
            GameEventType.PerfectRun => 9,
            GameEventType.BossEncounter => 8,
            GameEventType.LevelComplete => 7,
            GameEventType.CloseCall => 6,
            GameEventType.SecretFound => 6,
            GameEventType.ComboAchieved => 5,
            GameEventType.Death => 4,
            GameEventType.PowerUp => 3,
            GameEventType.Collectible => 2,
            GameEventType.Idle => 1,
            _ => 5
        };

        private async Task<string?> GenerateLlmCommentaryAsync(GameEventType eventType, Dictionary<string, object>? context)
        {
            // Try AdvancedAiService first for player-aware, memory-enhanced commentary
            if (_advancedAi != null)
            {
                try
                {
                    var commentaryContext = new CommentaryContext
                    {
                        GameTitle = _currentGameTitle,
                        PlayerAction = eventType.ToString(),
                        Score = context?.TryGetValue("score", out var s) == true ? Convert.ToInt32(s) : null,
                        Combo = context?.TryGetValue("combo", out var c) == true ? Convert.ToInt32(c) : null,
                        SessionDuration = _stats.SessionDuration
                    };

                    var result = await _advancedAi.GenerateCommentaryAsync(eventType.ToString(), commentaryContext);
                    if (!string.IsNullOrEmpty(result))
                        return result;
                }
                catch { /* Fall through to basic LLM */ }
            }

            // Fallback to direct LLM call (works with any provider)
            if (_llmService == null) return null;

            var personalityDesc = _currentPersonality switch
            {
                CommentatorPersonality.HypeCaster => "an extremely enthusiastic sports announcer who LOVES using caps and exclamations",
                CommentatorPersonality.WrestlingAnnouncer => "a WWE-style announcer who says things like 'BAH GAWD' and 'BY GOD'",
                CommentatorPersonality.DocumentaryNarrator => "David Attenborough narrating a nature documentary, calm and insightful",
                CommentatorPersonality.ChessMaster => "an analytical chess grandmaster breaking down the game state",
                CommentatorPersonality.MovieTrailerVoice => "an epic movie trailer voice-over artist with dramatic pauses",
                CommentatorPersonality.WholesomeCoach => "an incredibly supportive and encouraging coach who believes in the player",
                CommentatorPersonality.RoastMaster => "a sarcastic comedian roasting the player's gameplay",
                _ => "an enthusiastic commentator"
            };

            var contextStr = context != null ? string.Join(", ", context.Select(x => $"{x.Key}: {x.Value}")) : "";
            var prompt = $"Event: {eventType}. Context: {contextStr}. Generate one short, punchy commentary line (max 15 words) in the style of {personalityDesc}.";

            var systemPrompt = "You are a video game commentator. Give only the commentary line, no quotes or labels.";

            return await _llmService.CompleteAsync(prompt, systemPrompt);
        }

        private CommentaryLine CreateLine(string text, GameEventType eventType, int priority = 5)
        {
            var line = new CommentaryLine
            {
                Text = text,
                Personality = _currentPersonality,
                TriggerEvent = eventType,
                Timestamp = DateTime.Now,
                Priority = priority
            };
            _history.Add(line);
            _stats.TotalComments++;
            _lastComment = DateTime.Now;
            return line;
        }

        // Custom commentator support
        public void AddCustomCommentator(CustomCommentator commentator)
        {
            _customCommentators.Add(commentator);
        }

        public List<CustomCommentator> GetCustomCommentators() => _customCommentators;

        public void RemoveCustomCommentator(string id)
        {
            _customCommentators.RemoveAll(c => c.Id == id);
        }

        public List<CommentaryLine> GetHistory(int count = 20) =>
            _history.TakeLast(count).ToList();

        public void ClearHistory()
        {
            _history.Clear();
            _stats = new SessionStats();
            _sessionStart = DateTime.Now;
        }

        public void StartNewSession()
        {
            _stats = new SessionStats();
            _sessionStart = DateTime.Now;
            _consecutiveDeaths = 0;
        }
    }
}
