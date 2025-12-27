using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Telemetry
{
    /// <summary>
    /// Detects hallucinations in AI-generated content.
    /// Hallucinations are statements that contradict known facts or make up information.
    /// </summary>
    public interface IHallucinationDetector
    {
        /// <summary>
        /// Check content for potential hallucinations
        /// </summary>
        Task<HallucinationCheckResult> CheckAsync(string content, HallucinationContext context);

        /// <summary>
        /// Register a known fact for checking
        /// </summary>
        void RegisterFact(KnownFact fact);

        /// <summary>
        /// Register a detection pattern
        /// </summary>
        void RegisterPattern(HallucinationPattern pattern);

        /// <summary>
        /// Get detection statistics
        /// </summary>
        HallucinationStatistics GetStatistics();
    }

    /// <summary>
    /// Context for hallucination checking
    /// </summary>
    public class HallucinationContext
    {
        public string? GameId { get; set; }
        public string? CurrentScene { get; set; }
        public List<string> ActiveCharacters { get; set; } = new();
        public List<string> DeceasedCharacters { get; set; } = new();
        public Dictionary<string, object> WorldState { get; set; } = new();
    }

    /// <summary>
    /// Result of hallucination check
    /// </summary>
    public class HallucinationCheckResult
    {
        public bool HasHallucinations { get; set; }
        public List<DetectedHallucination> Hallucinations { get; set; } = new();
        public double ConfidenceScore { get; set; } = 1.0;
        public string? SuggestedCorrection { get; set; }
    }

    /// <summary>
    /// A detected hallucination
    /// </summary>
    public class DetectedHallucination
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OffendingText { get; set; } = string.Empty;
        public HallucinationSeverity Severity { get; set; }
        public string? CorrectInformation { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Severity of hallucination
    /// </summary>
    public enum HallucinationSeverity
    {
        Minor,          // Small inconsistency
        Moderate,       // Noticeable but not breaking
        Major,          // Significant contradiction
        Critical,       // Fundamental impossibility
        Resurrection    // Dead character as alive
    }

    /// <summary>
    /// A known fact for verification
    /// </summary>
    public class KnownFact
    {
        public string FactId { get; set; } = Guid.NewGuid().ToString();
        public string Category { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public bool IsNegatable { get; set; } = true; // Can be contradicted
        public string? NegationPattern { get; set; }
    }

    /// <summary>
    /// Pattern for detecting hallucinations
    /// </summary>
    public class HallucinationPattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string RegexPattern { get; set; } = string.Empty;
        public HallucinationSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistics for hallucination detection
    /// </summary>
    public class HallucinationStatistics
    {
        public long TotalChecks { get; set; }
        public long HallucinationsDetected { get; set; }
        public double DetectionRate => TotalChecks > 0 ? (double)HallucinationsDetected / TotalChecks * 100 : 0;
        public Dictionary<string, long> ByType { get; set; } = new();
        public Dictionary<HallucinationSeverity, long> BySeverity { get; set; } = new();
        public List<string> MostCommonPatterns { get; set; } = new();
    }

    /// <summary>
    /// Default implementation of hallucination detector
    /// </summary>
    public class HallucinationDetector : IHallucinationDetector
    {
        private readonly ConcurrentDictionary<string, KnownFact> _facts = new();
        private readonly ConcurrentDictionary<string, HallucinationPattern> _patterns = new();
        
        private long _totalChecks = 0;
        private long _hallucinationsDetected = 0;
        private readonly ConcurrentDictionary<string, long> _byType = new();
        private readonly ConcurrentDictionary<HallucinationSeverity, long> _bySeverity = new();
        private readonly ConcurrentDictionary<string, long> _patternHits = new();

        public HallucinationDetector()
        {
            RegisterDefaultPatterns();
        }

        public Task<HallucinationCheckResult> CheckAsync(string content, HallucinationContext context)
        {
            System.Threading.Interlocked.Increment(ref _totalChecks);

            var result = new HallucinationCheckResult();
            var contentLower = content.ToLowerInvariant();

            // Check for resurrection (dead character as alive)
            foreach (var deceased in context.DeceasedCharacters)
            {
                var aliveIndicators = new[] { "says", "tells you", "greets", "arrives", "is here", "speaks" };
                foreach (var indicator in aliveIndicators)
                {
                    if (contentLower.Contains(deceased.ToLowerInvariant()) && 
                        contentLower.Contains(indicator))
                    {
                        result.Hallucinations.Add(new DetectedHallucination
                        {
                            Type = "Resurrection",
                            Description = $"Deceased character '{deceased}' referenced as alive",
                            OffendingText = FindSentenceContaining(content, deceased),
                            Severity = HallucinationSeverity.Resurrection,
                            CorrectInformation = $"{deceased} is deceased and cannot perform actions",
                            Confidence = 0.9
                        });
                    }
                }
            }

            // Check against known facts
            foreach (var fact in _facts.Values)
            {
                var keywordMatch = fact.Keywords.Any(k => 
                    contentLower.Contains(k.ToLowerInvariant()));

                if (keywordMatch && fact.IsNegatable && fact.NegationPattern != null)
                {
                    if (Regex.IsMatch(content, fact.NegationPattern, RegexOptions.IgnoreCase))
                    {
                        result.Hallucinations.Add(new DetectedHallucination
                        {
                            Type = "FactContradiction",
                            Description = $"Content contradicts known fact: {fact.Statement}",
                            OffendingText = FindPatternMatch(content, fact.NegationPattern),
                            Severity = HallucinationSeverity.Major,
                            CorrectInformation = fact.Statement,
                            Confidence = 0.8
                        });
                    }
                }
            }

            // Check registered patterns
            foreach (var pattern in _patterns.Values)
            {
                if (Regex.IsMatch(content, pattern.RegexPattern, RegexOptions.IgnoreCase))
                {
                    result.Hallucinations.Add(new DetectedHallucination
                    {
                        Type = pattern.Name,
                        Description = pattern.Description,
                        OffendingText = FindPatternMatch(content, pattern.RegexPattern),
                        Severity = pattern.Severity,
                        Confidence = 0.7
                    });

                    _patternHits.AddOrUpdate(pattern.Name, 1, (_, c) => c + 1);
                }
            }

            // Check for impossible knowledge
            var impossiblePatterns = new[]
            {
                @"as (a|an) (AI|language model|assistant)",
                @"I (don't|do not) have access to",
                @"beyond my (training|knowledge)",
                @"I (can't|cannot) actually"
            };

            foreach (var pattern in impossiblePatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    result.Hallucinations.Add(new DetectedHallucination
                    {
                        Type = "MetaLeak",
                        Description = "AI revealed its nature instead of staying in character",
                        OffendingText = FindPatternMatch(content, pattern),
                        Severity = HallucinationSeverity.Critical,
                        Confidence = 0.95
                    });
                }
            }

            // Set result
            result.HasHallucinations = result.Hallucinations.Any();
            
            if (result.HasHallucinations)
            {
                System.Threading.Interlocked.Increment(ref _hallucinationsDetected);
                
                foreach (var h in result.Hallucinations)
                {
                    _byType.AddOrUpdate(h.Type, 1, (_, c) => c + 1);
                    _bySeverity.AddOrUpdate(h.Severity, 1, (_, c) => c + 1);
                }

                result.ConfidenceScore = 1.0 - result.Hallucinations.Average(h => h.Confidence);
            }

            return Task.FromResult(result);
        }

        public void RegisterFact(KnownFact fact)
        {
            _facts[fact.FactId] = fact;
        }

        public void RegisterPattern(HallucinationPattern pattern)
        {
            _patterns[pattern.PatternId] = pattern;
        }

        public HallucinationStatistics GetStatistics()
        {
            var topPatterns = _patternHits
                .OrderByDescending(p => p.Value)
                .Take(5)
                .Select(p => p.Key)
                .ToList();

            return new HallucinationStatistics
            {
                TotalChecks = _totalChecks,
                HallucinationsDetected = _hallucinationsDetected,
                ByType = new Dictionary<string, long>(_byType),
                BySeverity = new Dictionary<HallucinationSeverity, long>(_bySeverity),
                MostCommonPatterns = topPatterns
            };
        }

        private string FindSentenceContaining(string content, string term)
        {
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return sentences.FirstOrDefault(s => s.Contains(term, StringComparison.OrdinalIgnoreCase))?.Trim() 
                   ?? term;
        }

        private string FindPatternMatch(string content, string pattern)
        {
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Return match with context
                var start = Math.Max(0, match.Index - 20);
                var length = Math.Min(match.Length + 40, content.Length - start);
                return "..." + content.Substring(start, length) + "...";
            }
            return pattern;
        }

        private void RegisterDefaultPatterns()
        {
            // Fabricated statistics
            RegisterPattern(new HallucinationPattern
            {
                Name = "FabricatedStatistics",
                RegexPattern = @"\d{1,3}(\.\d+)?%\s+(of|more|less|fewer)",
                Severity = HallucinationSeverity.Moderate,
                Description = "Potentially fabricated statistics without source"
            });

            // Invented dates
            RegisterPattern(new HallucinationPattern
            {
                Name = "InventedDates",
                RegexPattern = @"(in|on|during)\s+\d{4}\s+(the|when|a)",
                Severity = HallucinationSeverity.Minor,
                Description = "Specific date may be invented"
            });

            // Confident uncertainty
            RegisterPattern(new HallucinationPattern
            {
                Name = "ConfidentUncertainty",
                RegexPattern = @"(definitely|certainly|absolutely|always)\s+(might|maybe|perhaps|possibly)",
                Severity = HallucinationSeverity.Minor,
                Description = "Contradictory certainty levels"
            });

            // Future knowledge
            RegisterPattern(new HallucinationPattern
            {
                Name = "FutureKnowledge",
                RegexPattern = @"(will|going to)\s+(definitely|certainly)\s+(happen|occur|be)",
                Severity = HallucinationSeverity.Moderate,
                Description = "Claiming certain knowledge of future events"
            });
        }
    }
}
