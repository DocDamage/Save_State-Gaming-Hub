using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IInputSanitizer
    {
        SanitizedInput Sanitize(string input, SanitizationOptions? options = null);
        Task<SanitizedInput> SanitizeAsync(string input, SanitizationOptions? options = null);
    }

    public class InputSanitizer : IInputSanitizer
    {
        private static readonly char[] ZeroWidthChars = new[]
        {
            '\u200B', '\u200C', '\u200D', '\u2060', '\uFEFF',
            '\u00AD', '\u034F', '\u061C', '\u115F', '\u1160',
            '\u17B4', '\u17B5', '\u180E', '\u2000', '\u2001',
            '\u2002', '\u2003', '\u2004', '\u2005', '\u2006',
            '\u2007', '\u2008', '\u2009', '\u200A', '\u2028',
            '\u2029', '\u202F', '\u205F', '\u3000'
        };

        public SanitizedInput Sanitize(string input, SanitizationOptions? options = null)
        {
            options ??= new SanitizationOptions();
            var result = new SanitizedInput { Original = input };
            var edgeCases = new List<EdgeCaseDetection>();

            // Edge case: Null input
            if (input == null)
            {
                edgeCases.Add(new EdgeCaseDetection
                {
                    Type = EdgeCaseType.UnexpectedNull,
                    Description = "Input was null",
                    Severity = 0.8f,
                    AutoRecoverable = true,
                    SuggestedAction = "Use empty string"
                });
                result.Sanitized = string.Empty;
                result.WasModified = true;
                result.DetectedEdgeCases = edgeCases;
                return result;
            }

            var sanitized = input;

            // Edge case: Empty input
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                edgeCases.Add(new EdgeCaseDetection
                {
                    Type = EdgeCaseType.EmptyInput,
                    Description = "Input is empty or whitespace only",
                    Severity = 0.5f,
                    AutoRecoverable = false
                });
            }

            // Remove zero-width characters
            if (options.RemoveZeroWidthChars)
            {
                var beforeLength = sanitized.Length;
                sanitized = RemoveZeroWidthCharacters(sanitized);
                if (sanitized.Length != beforeLength)
                {
                    result.AppliedTransformations.Add("removed_zero_width_chars");
                    result.WasModified = true;
                }
            }

            // Remove control characters
            if (options.RemoveControlCharacters)
            {
                var beforeLength = sanitized.Length;
                sanitized = RemoveControlCharacters(sanitized);
                if (sanitized.Length != beforeLength)
                {
                    result.AppliedTransformations.Add("removed_control_chars");
                    result.WasModified = true;
                }
            }

            // Normalize whitespace
            if (options.NormalizeWhitespace)
            {
                var before = sanitized;
                sanitized = NormalizeWhitespace(sanitized);
                if (before != sanitized)
                {
                    result.AppliedTransformations.Add("normalized_whitespace");
                    result.WasModified = true;
                }
            }

            // Strip HTML
            if (options.StripHtml)
            {
                var before = sanitized;
                sanitized = StripHtml(sanitized, options.AllowedHtmlTags);
                if (before != sanitized)
                {
                    result.AppliedTransformations.Add("stripped_html");
                    result.WasModified = true;
                }
            }

            // Normalize Unicode
            if (options.NormalizeUnicode)
            {
                var before = sanitized;
                sanitized = NormalizeUnicode(sanitized);
                if (before != sanitized)
                {
                    result.AppliedTransformations.Add("normalized_unicode");
                    result.WasModified = true;
                }
            }

            result.Sanitized = sanitized;
            result.DetectedEdgeCases = edgeCases;
            return result;
        }

        public Task<SanitizedInput> SanitizeAsync(string input, SanitizationOptions? options = null)
        {
            return Task.Run(() => Sanitize(input, options));
        }

        private string RemoveZeroWidthCharacters(string text)
        {
            return new string(text.Where(c => !ZeroWidthChars.Contains(c)).ToArray());
        }

        private string RemoveControlCharacters(string text)
        {
            return new string(text.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
        }

        private string NormalizeWhitespace(string text)
        {
            // Replace multiple spaces/tabs with single space
            text = Regex.Replace(text, @"[ \t]+", " ");
            // Replace multiple newlines with double newline
            text = Regex.Replace(text, @"(\r?\n){3,}", "\n\n");
            return text.Trim();
        }

        private string StripHtml(string text, HashSet<string>? allowedTags)
        {
            if (allowedTags == null || !allowedTags.Any())
            {
                // Remove all HTML
                return Regex.Replace(text, @"<[^>]+>", "");
            }

            // Remove only non-allowed tags
            var pattern = @"<(?!/?(" + string.Join("|", allowedTags) + @")\b)[^>]+>";
            return Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
        }

        private string NormalizeUnicode(string text)
        {
             return text.Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
