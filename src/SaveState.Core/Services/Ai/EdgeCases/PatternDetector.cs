using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IPatternDetector
    {
        List<EdgeCaseDetection> DetectRecursivePatterns(string text);
    }

    public class PatternDetector : IPatternDetector
    {
        public List<EdgeCaseDetection> DetectRecursivePatterns(string text)
        {
            var detections = new List<EdgeCaseDetection>();
            if (string.IsNullOrEmpty(text)) return detections;

            // Check for deeply nested structures
            var maxNesting = 0;
            var currentNesting = 0;
            foreach (var c in text)
            {
                if (c == '(' || c == '[' || c == '{')
                    currentNesting++;
                else if (c == ')' || c == ']' || c == '}')
                    currentNesting = Math.Max(0, currentNesting - 1);
                maxNesting = Math.Max(maxNesting, currentNesting);
            }

            if (maxNesting > 50)
            {
                detections.Add(new EdgeCaseDetection
                {
                    Type = EdgeCaseType.RecursiveReference,
                    Description = $"Deeply nested structure detected (depth: {maxNesting})",
                    Severity = 0.6f,
                    AutoRecoverable = false
                });
            }

            // Check for self-referencing patterns
            try
            {
               if (Regex.IsMatch(text, @"(.{20,})\1{3,}", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
               {
                   detections.Add(new EdgeCaseDetection
                   {
                       Type = EdgeCaseType.RecursiveReference,
                       Description = "Repeated pattern detected (possible recursion)",
                       Severity = 0.5f,
                       AutoRecoverable = true
                   });
               }
            }
            catch(RegexMatchTimeoutException)
            {
                // Ignore timeout here as it is non-critical
            }

            return detections;
        }
    }
}
