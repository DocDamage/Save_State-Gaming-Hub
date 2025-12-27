using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Verify lore consistency across outputs.
    /// </summary>
    public class LoreCheckResult
    {
        public bool IsConsistent { get; set; }
        public List<string> Contradictions { get; set; } = new();
        public List<string> UnverifiedClaims { get; set; } = new();
        public float ConfidenceScore { get; set; }
    }

    public interface ILoreChecksum
    {
        Task<LoreCheckResult> Verify(string output, IEnumerable<string> canonicalFacts);
        Task<LoreCheckResult> VerifyWithMemory(string output, ICanonicalMemory memory, List<string> topics);
        List<string> ExtractClaims(string text);
    }

    public class LoreChecksum : ILoreChecksum
    {
        public async Task<LoreCheckResult> Verify(string output, IEnumerable<string> canonicalFacts)
        {
            var result = new LoreCheckResult { IsConsistent = true, ConfidenceScore = 1.0f };
            var claims = ExtractClaims(output);
            var facts = canonicalFacts.ToList();

            foreach (var claim in claims)
            {
                var claimLower = claim.ToLowerInvariant();
                bool verified = false;
                bool contradicted = false;

                foreach (var fact in facts)
                {
                    var factLower = fact.ToLowerInvariant();

                    // Check for support
                    if (ClaimsAlign(claimLower, factLower))
                    {
                        verified = true;
                        break;
                    }

                    // Check for contradiction
                    if (ClaimsContradict(claimLower, factLower))
                    {
                        contradicted = true;
                        result.Contradictions.Add($"'{claim}' contradicts '{fact}'");
                        result.IsConsistent = false;
                        break;
                    }
                }

                if (!verified && !contradicted)
                {
                    result.UnverifiedClaims.Add(claim);
                }
            }

            // Adjust confidence
            if (result.Contradictions.Count > 0)
                result.ConfidenceScore = 0;
            else if (result.UnverifiedClaims.Count > claims.Count / 2)
                result.ConfidenceScore = 0.5f;

            return await Task.FromResult(result);
        }

        public async Task<LoreCheckResult> VerifyWithMemory(string output, ICanonicalMemory memory, List<string> topics)
        {
            var loreContext = await memory.BuildLoreContext(topics, 30);
            var facts = loreContext.Split('\n')
                .Where(l => l.TrimStart().StartsWith("•"))
                .Select(l => l.TrimStart().TrimStart('•').Trim())
                .ToList();

            return await Verify(output, facts);
        }

        public List<string> ExtractClaims(string text)
        {
            var claims = new List<string>();
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim();
                if (trimmed.Length < 10) continue;

                // Look for declarative statements
                var claimIndicators = new[] { " is ", " are ", " was ", " were ", " has ", " have ", " did " };
                if (claimIndicators.Any(ind => trimmed.Contains(ind)))
                {
                    claims.Add(trimmed);
                }
            }

            return claims;
        }

        private bool ClaimsAlign(string claim, string fact)
        {
            // Simple overlap check
            var claimWords = claim.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3).ToHashSet();
            var factWords = fact.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3).ToHashSet();

            var overlap = claimWords.Intersect(factWords).Count();
            return overlap >= 3 && !ClaimsContradict(claim, fact);
        }

        private bool ClaimsContradict(string claim, string fact)
        {
            var negations = new[] { ("is", "is not"), ("was", "was not"), ("are", "are not"), ("can", "cannot") };
            
            foreach (var (pos, neg) in negations)
            {
                if ((claim.Contains($" {pos} ") && fact.Contains($" {neg} ")) ||
                    (claim.Contains($" {neg} ") && fact.Contains($" {pos} ")))
                {
                    // Check if they share key terms
                    var claimTerms = claim.Replace(pos, "").Replace(neg, "")
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 3).ToHashSet();
                    var factTerms = fact.Replace(pos, "").Replace(neg, "")
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 3).ToHashSet();

                    if (claimTerms.Intersect(factTerms).Count() >= 2)
                        return true;
                }
            }

            return false;
        }
    }
}
