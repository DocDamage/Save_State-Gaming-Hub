using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Multi-pass validation.
    /// Pass 1: Rule compliance
    /// Pass 2: Lore consistency
    /// Pass 3: Tone appropriateness
    /// Pass 4: Safety check
    /// </summary>
    public enum ValidationPass
    {
        RuleCompliance,
        LoreConsistency,
        ToneAppropriateness,
        SafetyCheck
    }

    public class CritiqueResult
    {
        public ValidationPass Pass { get; set; }
        public bool Passed { get; set; }
        public float Confidence { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public class OutputCritique
    {
        public string OriginalOutput { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public List<CritiqueResult> PassResults { get; set; } = new();
        public string? RevisionRequired { get; set; }
        public float OverallScore { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public interface IOutputCritiquer
    {
        Task<OutputCritique> CritiqueAsync(string output, CritiqueContext context);
        Task<CritiqueResult> ValidateRuleCompliance(string output, CritiqueContext context);
        Task<CritiqueResult> ValidateLoreConsistency(string output, CritiqueContext context);
        Task<CritiqueResult> ValidateToneAppropriateness(string output, CritiqueContext context);
        Task<CritiqueResult> ValidateSafety(string output);
    }

    public class CritiqueContext
    {
        public string? ExpectedTone { get; set; }
        public List<string>? CanonicalFacts { get; set; }
        public Dictionary<string, bool>? ActiveFlags { get; set; }
        public string? RequestType { get; set; }
        public float MinConfidence { get; set; } = 0.7f;
    }

    public class OutputCritiquer : IOutputCritiquer
    {
        private readonly HashSet<string> _unsafePatterns = new()
        {
            "kill yourself", "harm yourself", "real world violence",
            "personal information", "credit card", "social security"
        };

        public async Task<OutputCritique> CritiqueAsync(string output, CritiqueContext context)
        {
            var critique = new OutputCritique { OriginalOutput = output };

            // Pass 1: Rule compliance
            var ruleResult = await ValidateRuleCompliance(output, context);
            critique.PassResults.Add(ruleResult);

            // Pass 2: Lore consistency
            var loreResult = await ValidateLoreConsistency(output, context);
            critique.PassResults.Add(loreResult);

            // Pass 3: Tone appropriateness
            var toneResult = await ValidateToneAppropriateness(output, context);
            critique.PassResults.Add(toneResult);

            // Pass 4: Safety check
            var safetyResult = await ValidateSafety(output);
            critique.PassResults.Add(safetyResult);

            // Calculate overall
            critique.IsApproved = critique.PassResults.All(r => r.Passed);
            critique.OverallScore = critique.PassResults.Average(r => r.Passed ? r.Confidence : 0);

            // Build summary
            var failed = critique.PassResults.Where(r => !r.Passed).ToList();
            if (failed.Count > 0)
            {
                critique.Summary = $"Failed {failed.Count} validation pass(es): " +
                    string.Join(", ", failed.Select(f => f.Pass.ToString()));
                critique.RevisionRequired = string.Join("; ", failed.SelectMany(f => f.Suggestions).Take(3));
            }
            else
            {
                critique.Summary = "All validation passes successful";
            }

            return critique;
        }

        public Task<CritiqueResult> ValidateRuleCompliance(string output, CritiqueContext context)
        {
            var result = new CritiqueResult
            {
                Pass = ValidationPass.RuleCompliance,
                Passed = true,
                Confidence = 0.9f
            };

            // Check for flag violations
            if (context.ActiveFlags != null)
            {
                var outputLower = output.ToLowerInvariant();

                foreach (var (flag, value) in context.ActiveFlags)
                {
                    // Check for dead NPC being referenced as alive
                    if (flag.EndsWith("_DEAD") && value)
                    {
                        var npcName = flag.Replace("_DEAD", "").Replace("_", " ").ToLowerInvariant();
                        if (outputLower.Contains($"{npcName} says") || outputLower.Contains($"{npcName} walks"))
                        {
                            result.Passed = false;
                            result.Issues.Add($"References deceased character: {npcName}");
                            result.Suggestions.Add($"Remove or revise references to {npcName} - character is dead");
                        }
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task<CritiqueResult> ValidateLoreConsistency(string output, CritiqueContext context)
        {
            var result = new CritiqueResult
            {
                Pass = ValidationPass.LoreConsistency,
                Passed = true,
                Confidence = 0.8f
            };

            if (context.CanonicalFacts == null || context.CanonicalFacts.Count == 0)
            {
                return Task.FromResult(result);
            }

            var outputLower = output.ToLowerInvariant();

            // Simple contradiction check
            foreach (var fact in context.CanonicalFacts)
            {
                var factLower = fact.ToLowerInvariant();

                // Check for negation patterns
                if (factLower.Contains("is not") && outputLower.Contains(factLower.Replace("is not", "is")))
                {
                    result.Passed = false;
                    result.Issues.Add($"Contradicts lore: {fact}");
                    result.Suggestions.Add("Revise to align with established lore");
                }
            }

            return Task.FromResult(result);
        }

        public Task<CritiqueResult> ValidateToneAppropriateness(string output, CritiqueContext context)
        {
            var result = new CritiqueResult
            {
                Pass = ValidationPass.ToneAppropriateness,
                Passed = true,
                Confidence = 0.85f
            };

            if (string.IsNullOrEmpty(context.ExpectedTone))
            {
                return Task.FromResult(result);
            }

            var outputLower = output.ToLowerInvariant();
            var expectedTone = context.ExpectedTone.ToLowerInvariant();

            // Tone mismatch detection
            if (expectedTone == "serious")
            {
                var jokeIndicators = new[] { "lol", "haha", "just kidding", "😂", "rofl" };
                if (jokeIndicators.Any(j => outputLower.Contains(j)))
                {
                    result.Passed = false;
                    result.Issues.Add("Humor in serious context");
                    result.Suggestions.Add("Remove comedic elements for serious scene");
                }
            }
            else if (expectedTone == "tense" || expectedTone == "urgent")
            {
                if (output.Length > 500 && !output.Contains("!"))
                {
                    result.Confidence = 0.6f;
                    result.Suggestions.Add("Consider more punchy, urgent language");
                }
            }

            return Task.FromResult(result);
        }

        public Task<CritiqueResult> ValidateSafety(string output)
        {
            var result = new CritiqueResult
            {
                Pass = ValidationPass.SafetyCheck,
                Passed = true,
                Confidence = 0.95f
            };

            var outputLower = output.ToLowerInvariant();

            foreach (var pattern in _unsafePatterns)
            {
                if (outputLower.Contains(pattern))
                {
                    result.Passed = false;
                    result.Issues.Add($"Unsafe content detected: {pattern}");
                    result.Suggestions.Add("Remove harmful content");
                    break;
                }
            }

            return Task.FromResult(result);
        }
    }
}
