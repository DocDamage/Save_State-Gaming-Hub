using System;

namespace SaveState.Core.Services.Ai.Uncertainty
{
    /// <summary>
    /// Wraps low-confidence outputs in lore-appropriate framing.
    /// Low confidence: "The records are fragmented..."
    /// Medium: "Legend speaks of..."
    /// High: Direct statement
    /// </summary>
    public class WrappedOutput
    {
        public string OriginalOutput { get; set; } = string.Empty;
        public string FinalOutput { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string ConfidenceLevel { get; set; } = "medium";
        public bool WasWrapped { get; set; }
    }

    public interface IUncertaintyWrapper
    {
        WrappedOutput Wrap(string output, ConfidenceAssessment assessment);
        string GetWrapper(string confidenceLevel, string category);
    }

    public class UncertaintyWrapper : IUncertaintyWrapper
    {
        private readonly Random _random = new();

        public WrappedOutput Wrap(string output, ConfidenceAssessment assessment)
        {
            var result = new WrappedOutput
            {
                OriginalOutput = output,
                Confidence = assessment.OverallConfidence,
                ConfidenceLevel = assessment.ConfidenceLevel
            };

            if (assessment.ConfidenceLevel == "high")
            {
                // No wrapping needed
                result.FinalOutput = output;
                result.WasWrapped = false;
                return result;
            }

            // Determine category from content
            var category = InferCategory(output);
            var wrapper = GetWrapper(assessment.ConfidenceLevel, category);

            result.FinalOutput = $"{wrapper}\n\n{output}";
            result.WasWrapped = true;

            // Add uncertainty notices if very low confidence
            if (assessment.ConfidenceLevel == "very_low")
            {
                result.FinalOutput += "\n\n[Note: This information could not be fully verified.]";
            }

            return result;
        }

        public string GetWrapper(string confidenceLevel, string category)
        {
            return (confidenceLevel, category) switch
            {
                ("low" or "very_low", "lore") => PickRandom(
                    "The ancient texts are fragmented on this matter...",
                    "The records speak only in whispers of this...",
                    "What little is known suggests...",
                    "The archives hold only partial truths here..."
                ),
                ("low" or "very_low", "history") => PickRandom(
                    "History's pages are worn thin here...",
                    "The chronicles from that era are incomplete...",
                    "Scholars still debate the specifics, but...",
                    "Time has obscured much, yet we know..."
                ),
                ("low" or "very_low", "character") => PickRandom(
                    "Little is certain about them, but it is said...",
                    "Their true nature remains mysterious...",
                    "What tales remain suggest...",
                    "The truth of their story is uncertain..."
                ),
                ("medium", "lore") => PickRandom(
                    "Legend speaks of this...",
                    "The old stories tell us...",
                    "According to the histories...",
                    "It is widely held that..."
                ),
                ("medium", "history") => PickRandom(
                    "The histories record...",
                    "As the chronicles tell it...",
                    "Historical accounts suggest...",
                    "It is generally understood that..."
                ),
                ("medium", "character") => PickRandom(
                    "Those who knew them say...",
                    "By most accounts...",
                    "It is commonly believed...",
                    "Stories suggest..."
                ),
                ("medium", _) => PickRandom(
                    "Evidence suggests...",
                    "It appears that...",
                    "Based on available information...",
                    "Current understanding indicates..."
                ),
                ("low" or "very_low", _) => PickRandom(
                    "The details remain unclear, but...",
                    "Information is limited, however...",
                    "While uncertain, it seems...",
                    "The truth is obscured, yet..."
                ),
                _ => ""
            };
        }

        private string InferCategory(string output)
        {
            var lower = output.ToLowerInvariant();

            if (ContainsAny(lower, "ancient", "kingdom", "realm", "magic", "prophecy", "artifact"))
                return "lore";
            if (ContainsAny(lower, "year", "century", "war", "battle", "founded", "built"))
                return "history";
            if (ContainsAny(lower, "he ", "she ", "they ", "was born", "lived", "died"))
                return "character";

            return "general";
        }

        private bool ContainsAny(string text, params string[] terms)
        {
            foreach (var term in terms)
            {
                if (text.Contains(term)) return true;
            }
            return false;
        }

        private string PickRandom(params string[] options)
        {
            return options[_random.Next(options.Length)];
        }
    }
}
