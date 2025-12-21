using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Player
{
    /// <summary>
    /// Updates player model from actions.
    /// - Decision pattern analysis
    /// - Time-between-actions metrics
    /// - Dialog choice analysis
    /// </summary>
    public enum ActionCategory
    {
        Combat,
        Dialogue,
        Exploration,
        Quest,
        Inventory,
        Movement,
        MoralChoice,
        Social,
        System,
        Unknown
    }

    public class PlayerAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty;
        public ActionCategory Category { get; set; } = ActionCategory.Unknown;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Target { get; set; }
        public string? Context { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TimeSpan? TimeSinceLastAction { get; set; }
    }

    public class BehaviorPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int OccurrenceCount { get; set; }
        public float Strength { get; set; }
        public List<string> ActionSequence { get; set; } = new();
    }

    public interface IBehaviorTracker
    {
        void TrackAction(PlayerAction action);
        PlayerAction? GetLastAction();
        IEnumerable<PlayerAction> GetRecentActions(int count);
        float GetAverageTimeBetweenActions();
        Dictionary<ActionCategory, float> GetCategoryDistribution();
        IEnumerable<BehaviorPattern> DetectPatterns(int minOccurrences = 3);
        DialogueAnalysis AnalyzeDialogueChoices();
    }

    public class DialogueAnalysis
    {
        public float AggressiveRatio { get; set; }
        public float PeacefulRatio { get; set; }
        public float HumorousRatio { get; set; }
        public float CuriousRatio { get; set; }
        public int TotalDialogueChoices { get; set; }
        public string DominantStyle { get; set; } = "neutral";
    }

    public class BehaviorTracker : IBehaviorTracker
    {
        private readonly List<PlayerAction> _actions = new();
        private readonly Dictionary<string, List<PlayerAction>> _actionsByType = new();
        private readonly int _maxHistory = 1000;

        public void TrackAction(PlayerAction action)
        {
            // Calculate time since last action
            if (_actions.Count > 0)
            {
                action.TimeSinceLastAction = action.Timestamp - _actions.Last().Timestamp;
            }

            _actions.Add(action);

            // Index by type
            var type = action.ActionType.ToLowerInvariant();
            if (!_actionsByType.ContainsKey(type))
            {
                _actionsByType[type] = new List<PlayerAction>();
            }
            _actionsByType[type].Add(action);

            // Prune old actions
            while (_actions.Count > _maxHistory)
            {
                var oldest = _actions[0];
                _actions.RemoveAt(0);
                
                var oldType = oldest.ActionType.ToLowerInvariant();
                if (_actionsByType.TryGetValue(oldType, out var typeList))
                {
                    typeList.Remove(oldest);
                }
            }
        }

        public PlayerAction? GetLastAction()
        {
            return _actions.LastOrDefault();
        }

        public IEnumerable<PlayerAction> GetRecentActions(int count)
        {
            return _actions.TakeLast(count);
        }

        public float GetAverageTimeBetweenActions()
        {
            var timings = _actions
                .Where(a => a.TimeSinceLastAction.HasValue)
                .Select(a => a.TimeSinceLastAction!.Value.TotalSeconds)
                .ToList();

            return timings.Count > 0 ? (float)timings.Average() : 0;
        }

        public Dictionary<ActionCategory, float> GetCategoryDistribution()
        {
            var total = _actions.Count;
            if (total == 0)
            {
                return new Dictionary<ActionCategory, float>();
            }

            return _actions
                .GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => (float)g.Count() / total);
        }

        public IEnumerable<BehaviorPattern> DetectPatterns(int minOccurrences = 3)
        {
            var patterns = new Dictionary<string, BehaviorPattern>();
            
            // Look for 2-3 action sequences
            for (int windowSize = 2; windowSize <= 3; windowSize++)
            {
                for (int i = 0; i <= _actions.Count - windowSize; i++)
                {
                    var sequence = _actions.Skip(i).Take(windowSize)
                        .Select(a => a.ActionType.ToLowerInvariant())
                        .ToList();
                    
                    var patternKey = string.Join("→", sequence);
                    
                    if (!patterns.ContainsKey(patternKey))
                    {
                        patterns[patternKey] = new BehaviorPattern
                        {
                            PatternId = patternKey,
                            Name = patternKey,
                            ActionSequence = sequence
                        };
                    }
                    patterns[patternKey].OccurrenceCount++;
                }
            }

            // Filter and calculate strength
            var totalSequences = _actions.Count - 1;
            return patterns.Values
                .Where(p => p.OccurrenceCount >= minOccurrences)
                .Select(p =>
                {
                    p.Strength = (float)p.OccurrenceCount / totalSequences;
                    return p;
                })
                .OrderByDescending(p => p.OccurrenceCount);
        }

        public DialogueAnalysis AnalyzeDialogueChoices()
        {
            var dialogueActions = _actions
                .Where(a => a.Category == ActionCategory.Dialogue)
                .ToList();

            var total = dialogueActions.Count;
            if (total == 0)
            {
                return new DialogueAnalysis();
            }

            int aggressive = 0, peaceful = 0, humorous = 0, curious = 0;

            foreach (var action in dialogueActions)
            {
                if (action.Metadata.TryGetValue("tone", out var tone))
                {
                    var t = tone.ToString()?.ToLowerInvariant() ?? "";
                    if (t.Contains("aggress") || t.Contains("threat")) aggressive++;
                    else if (t.Contains("peace") || t.Contains("calm")) peaceful++;
                    else if (t.Contains("humor") || t.Contains("joke")) humorous++;
                    else if (t.Contains("curious") || t.Contains("question")) curious++;
                }

                if (action.Metadata.TryGetValue("chose_peaceful", out var cp) && (bool)cp)
                    peaceful++;
                if (action.Metadata.TryGetValue("chose_aggressive", out var ca) && (bool)ca)
                    aggressive++;
                if (action.Metadata.TryGetValue("chose_humor", out var ch) && (bool)ch)
                    humorous++;
            }

            var analysis = new DialogueAnalysis
            {
                AggressiveRatio = (float)aggressive / total,
                PeacefulRatio = (float)peaceful / total,
                HumorousRatio = (float)humorous / total,
                CuriousRatio = (float)curious / total,
                TotalDialogueChoices = total
            };

            // Determine dominant style
            var max = Math.Max(Math.Max(analysis.AggressiveRatio, analysis.PeacefulRatio),
                              Math.Max(analysis.HumorousRatio, analysis.CuriousRatio));

            if (max == analysis.AggressiveRatio) analysis.DominantStyle = "aggressive";
            else if (max == analysis.PeacefulRatio) analysis.DominantStyle = "peaceful";
            else if (max == analysis.HumorousRatio) analysis.DominantStyle = "humorous";
            else if (max == analysis.CuriousRatio) analysis.DominantStyle = "curious";

            return analysis;
        }

        /// <summary>
        /// Determine if player is rushing or exploring
        /// </summary>
        public string GetPlayStyle()
        {
            var avgTime = GetAverageTimeBetweenActions();
            var distribution = GetCategoryDistribution();
            var explorationRatio = distribution.TryGetValue(ActionCategory.Exploration, out var e) ? e : 0;
            var combatRatio = distribution.TryGetValue(ActionCategory.Combat, out var c) ? c : 0;

            if (avgTime < 2.0f && combatRatio > 0.4f)
                return "Aggressive Rusher";
            if (avgTime < 3.0f && explorationRatio < 0.2f)
                return "Focused Achiever";
            if (explorationRatio > 0.3f && avgTime > 5.0f)
                return "Thorough Explorer";
            if (distribution.TryGetValue(ActionCategory.Dialogue, out var d) && d > 0.3f)
                return "Social Player";

            return "Balanced";
        }
    }
}
