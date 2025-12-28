using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IOutputValidator
    {
        string SanitizeOutput(string output, OutputSanitizationOptions? options = null);
        string EnsureCompleteSentences(string text);
        string NormalizeOutputFormatting(string text);
    }

    public class OutputValidator : IOutputValidator
    {
        private readonly ITextTruncator _truncator;

        public OutputValidator(ITextTruncator truncator)
        {
            _truncator = truncator ?? throw new ArgumentNullException(nameof(truncator));
        }

        public string SanitizeOutput(string output, OutputSanitizationOptions? options = null)
        {
            if (string.IsNullOrEmpty(output)) return output;

            options ??= new OutputSanitizationOptions();
            var result = output;

            // Remove AI self-references
            if (options.RemoveAiSelfReferences)
            {
                var aiPatterns = new[]
                {
                    @"as an ai[,\s]",
                    @"as a language model[,\s]",
                    @"i don't have (access|the ability)",
                    @"i cannot (browse|access|see)",
                    @"my training data",
                    @"i was trained"
                };

                foreach (var pattern in aiPatterns)
                {
                    result = Regex.Replace(result, pattern, "", RegexOptions.IgnoreCase);
                }
            }

            // Remove system messages
            if (options.RemoveSystemMessages)
            {
                result = Regex.Replace(result, @"\[system[^\]]*\].*?(?=\[|$)", "",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                result = Regex.Replace(result, @"<\|.*?\|>", "");
            }

            // Check must-not-contain
            if (options.MustNotContain != null)
            {
                foreach (var forbidden in options.MustNotContain)
                {
                    result = result.Replace(forbidden, "[REDACTED]", StringComparison.OrdinalIgnoreCase);
                }
            }

            // Ensure complete sentences
            if (options.EnsureCompleteSentences && !string.IsNullOrEmpty(result))
            {
                result = EnsureCompleteSentences(result);
            }

            // Normalize formatting
            if (options.NormalizeFormatting)
            {
                result = NormalizeOutputFormatting(result);
            }

            // Truncate if needed
            if (result.Length > options.MaxLength)
            {
                result = _truncator.TruncateSmart(result, options.MaxLength, TruncationMode.Sentence);
            }

            return result.Trim();
        }

        public string EnsureCompleteSentences(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var trimmed = text.Trim();
            var lastChar = trimmed.Last();

            // If it ends with punctuation, it's likely complete
            if (lastChar == '.' || lastChar == '!' || lastChar == '?' || lastChar == '"' || lastChar == '\'')
                return trimmed;

            // Otherwise, try to cut back to last sentence
            var lastSentenceEnd = -1;
            var endings = new[] { ". ", "! ", "? ", ".\n", "!\n", "?\n" };

            foreach (var ending in endings)
            {
                var pos = trimmed.LastIndexOf(ending);
                if (pos > lastSentenceEnd)
                    lastSentenceEnd = pos;
            }

            if (lastSentenceEnd > 0)
            {
                return trimmed.Substring(0, lastSentenceEnd + 1).Trim();
            }

            // If only one sentence fragment, append ellipsis
            return trimmed + "...";
        }

        public string NormalizeOutputFormatting(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Fix multiple newlines
            var normalized = Regex.Replace(text, @"(\r?\n){3,}", "\n\n");

            // Fix spacing around punctuation
            normalized = Regex.Replace(normalized, @"\s+([.!?,;:])", "$1");

            // Ensure space after punctuation
            normalized = Regex.Replace(normalized, @"([.!?,;:])(?=[a-zA-Z])", "$1 ");

            return normalized.Trim();
        }
    }
}
