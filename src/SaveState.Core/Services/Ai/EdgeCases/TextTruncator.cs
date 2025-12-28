using System.Linq;
using System.Linq;
using System.Collections.Generic;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface ITextTruncator
    {
        string TruncateSmart(string text, int maxLength, TruncationMode mode = TruncationMode.Sentence);
    }

    public class TextTruncator : ITextTruncator
    {
        public string TruncateSmart(string text, int maxLength, TruncationMode mode = TruncationMode.Sentence)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            switch (mode)
            {
                case TruncationMode.Hard:
                    return text.Substring(0, maxLength);

                case TruncationMode.Word:
                    return TruncateAtWordBoundary(text, maxLength);

                case TruncationMode.Sentence:
                    return TruncateAtSentenceBoundary(text, maxLength);

                case TruncationMode.Paragraph:
                    return TruncateAtParagraphBoundary(text, maxLength);

                case TruncationMode.Semantic:
                    return TruncateAtSemanticBoundary(text, maxLength);

                default:
                    return text.Substring(0, maxLength);
            }
        }

        private string TruncateAtWordBoundary(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;

            var truncated = text.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > maxLength * 0.8)
                return truncated.Substring(0, lastSpace) + "...";

            return truncated + "...";
        }

        private string TruncateAtSentenceBoundary(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;

            var truncated = text.Substring(0, maxLength);

            // Find last sentence ending
            var sentenceEndings = new[] { ". ", "! ", "? ", ".\n", "!\n", "?\n" };
            var lastEnding = -1;

            foreach (var ending in sentenceEndings)
            {
                var pos = truncated.LastIndexOf(ending);
                if (pos > lastEnding)
                    lastEnding = pos;
            }

            if (lastEnding > maxLength * 0.5)
                return truncated.Substring(0, lastEnding + 1).Trim();

            return TruncateAtWordBoundary(text, maxLength);
        }

        private string TruncateAtParagraphBoundary(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;

            var truncated = text.Substring(0, maxLength);
            var lastParagraph = truncated.LastIndexOf("\n\n");

            if (lastParagraph > maxLength * 0.5)
                return truncated.Substring(0, lastParagraph).Trim();

            return TruncateAtSentenceBoundary(text, maxLength);
        }

        private string TruncateAtSemanticBoundary(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;

            var truncated = text.Substring(0, maxLength);

            // Look for section markers
            var sectionPatterns = new[] { "\n## ", "\n### ", "\n---", "\n***", "\n\n\n" };
            var lastSection = -1;

             foreach (var pattern in sectionPatterns)
            {
                var pos = truncated.LastIndexOf(pattern);
                if (pos > lastSection)
                    lastSection = pos;
            }

            if (lastSection > maxLength * 0.3)
                return truncated.Substring(0, lastSection).Trim();

            return TruncateAtParagraphBoundary(text, maxLength);
        }
    }
}
