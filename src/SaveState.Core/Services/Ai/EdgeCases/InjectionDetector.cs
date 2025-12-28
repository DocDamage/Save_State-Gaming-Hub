using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IInjectionDetector
    {
        List<EdgeCaseDetection> DetectInjectionAttempts(string text);
        bool ShouldReject(string input, out string reason);
    }

    public class InjectionDetector : IInjectionDetector
    {
        private readonly EdgeCaseConfig _config;

        // Injection detection patterns
        private static readonly string[] InjectionPatterns = new[]
        {
            @"ignore\s+(all\s+)?previous\s+instructions?",
            @"forget\s+(everything|all|your)",
            @"you\s+are\s+now\s+a?",
            @"pretend\s+(to\s+be|you're?)",
            @"disregard\s+(all|the|your)",
            @"new\s+instructions?:",
            @"\[system\]|\[sys\]|\[admin\]",
            @"<\|.*\|>",
            @"(?:```|''').*?(?:system|assistant|user).*?(?:```|''')",
            @"act\s+as\s+if\s+you",
            @"roleplay\s+as",
            @"switch\s+to\s+.+\s+mode",
            @"enable\s+(jailbreak|developer|debug)",
            @"bypass\s+(safety|filter|restriction)"
        };

        public InjectionDetector(EdgeCaseConfig? config = null)
        {
            _config = config ?? new EdgeCaseConfig();
        }

        public List<EdgeCaseDetection> DetectInjectionAttempts(string text)
        {
            var detections = new List<EdgeCaseDetection>();
            if (string.IsNullOrEmpty(text)) return detections;

            var textLower = text.ToLowerInvariant();

            foreach (var pattern in InjectionPatterns)
            {
                try
                {
                    var matches = Regex.Matches(textLower, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        detections.Add(new EdgeCaseDetection
                        {
                            Type = EdgeCaseType.InjectionAttempt,
                            Description = $"Potential injection: '{match.Value}'",
                            Location = $"Position {match.Index}",
                            Severity = 0.9f,
                            AutoRecoverable = false,
                            SuggestedAction = "Block or sanitize request"
                        });
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Pattern took too long - could be ReDoS attack
                    detections.Add(new EdgeCaseDetection
                    {
                        Type = EdgeCaseType.InjectionAttempt,
                        Description = "Regex timeout - possible ReDoS attack",
                        Severity = 0.95f,
                        AutoRecoverable = false
                    });
                    break;
                }
            }

            return detections;
        }

        public bool ShouldReject(string input, out string reason)
        {
            reason = string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                reason = "Input is empty";
                return true;
            }

            // Check for critical injection attempts
            foreach (var pattern in InjectionPatterns.Take(5)) // Check most severe patterns
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    reason = "Potential injection attack detected";
                    return true;
                }
            }

            // Check for excessive length that can't be truncated
            if (input.Length > _config.AbsoluteMaxLength)
            {
                reason = $"Input exceeds absolute maximum length ({_config.AbsoluteMaxLength})";
                return true;
            }

            // Check for binary content
            var nonPrintableRatio = input.Count(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                                   / (float)Math.Max(1, input.Length);
            if (nonPrintableRatio > 0.1)
            {
                reason = "Input appears to contain binary data";
                return true;
            }

            return false;
        }
    }
}
