using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Built-in validators for the EnhancedOutputValidator.
    /// Each validator implements IValidator and handles specific validation types.
    /// </summary>
    
    /// <summary>
    /// Validates content for safety issues including toxic language and sensitive topics.
    /// </summary>
    public class SafetyValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.Safety;

        private static readonly string[] ToxicPatterns = new[]
        {
            @"\b(kill\s+yourself|harm\s+yourself)\b",
            @"\b(hate\s+(all|every)\s+\w+)\b",
            @"explicit\s+sexual",
            @"\b(slur|n-word|f-word)\b"
        };

        private static readonly string[] SensitiveTopics = new[]
        {
            "suicide", "self-harm", "real violence", "real person harm"
        };

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            var contentLower = content.ToLowerInvariant();

            // Check for toxic patterns
            foreach (var pattern in ToxicPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.Critical,
                        Message = "Content contains potentially harmful language",
                        AutoFixable = false
                    });
                    score -= 0.5f;
                }
            }

            // Check for sensitive topics
            foreach (var topic in SensitiveTopics)
            {
                if (contentLower.Contains(topic))
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.High,
                        Message = $"Content touches on sensitive topic: {topic}",
                        AutoFixable = false
                    });
                    score -= 0.3f;
                }
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            // Safety issues typically can't be auto-repaired
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// Validates content length against configured constraints.
    /// </summary>
    public class LengthValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.LengthCheck;

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            // Check minimum length
            if (context.MinLength.HasValue && content.Length < context.MinLength.Value)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.Medium,
                    Message = $"Content too short (min: {context.MinLength}, actual: {content.Length})",
                    AutoFixable = false
                });
                score -= 0.3f;
            }

            // Check maximum length
            if (context.MaxLength.HasValue && content.Length > context.MaxLength.Value)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.Medium,
                    Message = $"Content too long (max: {context.MaxLength}, actual: {content.Length})",
                    SuggestedFix = content.Substring(0, context.MaxLength.Value - 3) + "...",
                    StartIndex = context.MaxLength.Value - 3,
                    EndIndex = content.Length,
                    AutoFixable = true
                });
                score -= 0.2f;
            }

            // Check for extremely short or long content (general limits)
            if (content.Length < 5)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.High,
                    Message = "Response is suspiciously short",
                    AutoFixable = false
                });
                score -= 0.4f;
            }

            if (content.Length > 10000)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.High,
                    Message = "Response is excessively long",
                    AutoFixable = true
                });
                score -= 0.3f;
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            var tooLongIssue = issues.FirstOrDefault(i => i.Message.Contains("too long"));
            if (tooLongIssue?.SuggestedFix != null)
            {
                return Task.FromResult<string?>(tooLongIssue.SuggestedFix);
            }
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// Validates content tone against expected tone (formal, casual, dramatic, etc.).
    /// </summary>
    public class ToneValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.ToneCheck;

        private readonly Dictionary<string, string[]> ToneKeywords = new()
        {
            ["formal"] = new[] { "therefore", "furthermore", "nevertheless", "accordingly", "consequently" },
            ["casual"] = new[] { "hey", "cool", "awesome", "gonna", "wanna", "kinda" },
            ["dramatic"] = new[] { "epic", "legendary", "mighty", "thunder", "destiny", "fate" },
            ["humorous"] = new[] { "haha", "lol", "funny", "joke", "hilarious", "rofl" },
            ["serious"] = new[] { "critical", "important", "warn", "danger", "severe", "urgent" }
        };

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            if (string.IsNullOrEmpty(context.ExpectedTone))
            {
                return Task.FromResult((score, issues));
            }

            var contentLower = content.ToLowerInvariant();
            var expectedTone = context.ExpectedTone.ToLowerInvariant();

            if (!ToneKeywords.TryGetValue(expectedTone, out var expectedKeywords))
            {
                return Task.FromResult((score, issues));
            }

            // Check for expected tone keywords
            var matchedExpectedKeywords = expectedKeywords.Count(k => contentLower.Contains(k));
            
            // Check for conflicting tone keywords
            foreach (var (tone, keywords) in ToneKeywords)
            {
                if (tone == expectedTone) continue;
                
                var conflictingMatches = keywords.Count(k => contentLower.Contains(k));
                if (conflictingMatches > matchedExpectedKeywords)
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.Low,
                        Message = $"Tone mismatch: expected '{expectedTone}' but detected '{tone}'",
                        AutoFixable = false
                    });
                    score -= 0.2f;
                }
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// Detects and optionally repairs repeated content (sentences, words, patterns).
    /// </summary>
    public class RepetitionValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.RepetitionCheck;

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            // Check for repeated sentences
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s.Length > 10)
                .ToList();

            var duplicates = sentences.GroupBy(s => s)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var dup in duplicates)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.Medium,
                    Message = $"Repeated sentence detected ({dup.Count()} times): '{dup.Key.Substring(0, Math.Min(50, dup.Key.Length))}...'",
                    AutoFixable = true
                });
                score -= 0.1f * dup.Count();
            }

            // Check for repeated words in sequence
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < words.Length; i++)
            {
                if (words[i].ToLowerInvariant() == words[i - 1].ToLowerInvariant() &&
                    words[i].Length > 3)
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.Low,
                        Message = $"Repeated word: '{words[i]}'",
                        AutoFixable = true
                    });
                    score -= 0.05f;
                }
            }

            // Check for excessive repetition patterns (AI hallucination symptom)
            var threeGrams = new List<string>();
            for (int i = 0; i <= words.Length - 3; i++)
            {
                var gram = $"{words[i]} {words[i + 1]} {words[i + 2]}".ToLowerInvariant();
                threeGrams.Add(gram);
            }

            var repeatedGrams = threeGrams.GroupBy(g => g)
                .Where(g => g.Count() >= 3)
                .ToList();

            if (repeatedGrams.Any())
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.High,
                    Message = "Excessive pattern repetition detected (possible hallucination)",
                    AutoFixable = false
                });
                score -= 0.4f;
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            var result = content;

            // Remove repeated consecutive words
            var words = result.Split(' ');
            var filtered = new List<string> { words[0] };
            
            for (int i = 1; i < words.Length; i++)
            {
                if (words[i].ToLowerInvariant() != words[i - 1].ToLowerInvariant())
                {
                    filtered.Add(words[i]);
                }
            }

            return Task.FromResult<string?>(string.Join(" ", filtered));
        }
    }

    /// <summary>
    /// Validates content format (brackets, quotes, incomplete sentences, format types).
    /// </summary>
    public class FormatValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.FormatCheck;

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            // Check for unbalanced brackets/quotes
            var openParens = content.Count(c => c == '(');
            var closeParens = content.Count(c => c == ')');
            if (openParens != closeParens)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.Low,
                    Message = $"Unbalanced parentheses: {openParens} open, {closeParens} close",
                    AutoFixable = false
                });
                score -= 0.1f;
            }

            var quotes = content.Count(c => c == '"');
            if (quotes % 2 != 0)
            {
                issues.Add(new ValidationIssue
                {
                    ValidatorType = Type,
                    Severity = ValidationSeverity.Low,
                    Message = "Unbalanced quotes",
                    AutoFixable = false
                });
                score -= 0.1f;
            }

            // Check for incomplete sentences (ends mid-thought)
            if (!string.IsNullOrEmpty(content) && 
                !content.EndsWith(".") && !content.EndsWith("!") && 
                !content.EndsWith("?") && !content.EndsWith("\""))
            {
                var lastWord = content.Split(' ').LastOrDefault()?.ToLowerInvariant() ?? "";
                var incompleteIndicators = new[] { "and", "but", "or", "the", "a", "an", "to", "of", "with" };
                
                if (incompleteIndicators.Contains(lastWord))
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.Medium,
                        Message = "Response appears to be cut off mid-sentence",
                        AutoFixable = false
                    });
                    score -= 0.3f;
                }
            }

            // Check for format type compliance
            if (!string.IsNullOrEmpty(context.FormatType))
            {
                switch (context.FormatType.ToLowerInvariant())
                {
                    case "json":
                        if (!content.TrimStart().StartsWith("{") && !content.TrimStart().StartsWith("["))
                        {
                            issues.Add(new ValidationIssue
                            {
                                ValidatorType = Type,
                                Severity = ValidationSeverity.High,
                                Message = "Expected JSON format but content doesn't appear to be valid JSON",
                                AutoFixable = false
                            });
                            score -= 0.5f;
                        }
                        break;
                    case "markdown":
                        // Markdown is fairly permissive, just check for basics
                        break;
                    case "plain":
                        // Check for unexpected formatting
                        if (content.Contains("```") || content.Contains("##"))
                        {
                            issues.Add(new ValidationIssue
                            {
                                ValidatorType = Type,
                                Severity = ValidationSeverity.Low,
                                Message = "Plain text expected but found markdown formatting",
                                AutoFixable = true
                            });
                            score -= 0.1f;
                        }
                        break;
                }
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            var result = content;

            // Remove markdown from plain text
            if (issues.Any(i => i.Message.Contains("markdown formatting")))
            {
                result = Regex.Replace(result, @"```\w*\n?", "");
                result = Regex.Replace(result, @"#{1,6}\s+", "");
                result = Regex.Replace(result, @"\*{1,2}([^*]+)\*{1,2}", "$1");
            }

            return Task.FromResult<string?>(result);
        }
    }

    /// <summary>
    /// Detects AI hallucination indicators and content contradictions.
    /// </summary>
    public class HallucinationValidator : IValidator
    {
        public ValidatorType Type => ValidatorType.HallucinationDetection;

        private static readonly string[] HallucinationIndicators = new[]
        {
            "as an ai",
            "as a language model",
            "i don't have access to",
            "i cannot browse",
            "as of my knowledge cutoff",
            "i was trained on",
            "my training data"
        };

        public Task<(float score, List<ValidationIssue> issues)> ValidateAsync(
            string content, EnhancedValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            float score = 1.0f;

            var contentLower = content.ToLowerInvariant();

            // Check for AI self-reference (breaks immersion)
            foreach (var indicator in HallucinationIndicators)
            {
                if (contentLower.Contains(indicator))
                {
                    issues.Add(new ValidationIssue
                    {
                        ValidatorType = Type,
                        Severity = ValidationSeverity.High,
                        Message = "Response breaks character by referencing AI nature",
                        AutoFixable = false
                    });
                    score -= 0.4f;
                    break;
                }
            }

            // Check for contradictions with canonical facts
            if (context.CanonicalFacts != null)
            {
                foreach (var fact in context.CanonicalFacts)
                {
                    var factTerms = fact.ToLowerInvariant().Split(' ')
                        .Where(t => t.Length > 3).ToList();
                    
                    // Simple negation detection
                    if (factTerms.Count > 2)
                    {
                        var matchedTerms = factTerms.Count(t => contentLower.Contains(t));
                        var hasNegation = contentLower.Contains("not " + factTerms.First()) ||
                                         contentLower.Contains("never " + factTerms.First()) ||
                                         contentLower.Contains("no " + factTerms.First());
                        
                        if (matchedTerms >= factTerms.Count / 2 && hasNegation)
                        {
                            issues.Add(new ValidationIssue
                            {
                                ValidatorType = Type,
                                Severity = ValidationSeverity.High,
                                Message = $"Possible contradiction with canonical fact: '{fact}'",
                                AutoFixable = false
                            });
                            score -= 0.3f;
                        }
                    }
                }
            }

            // Check for must-include terms
            if (context.MustIncludeTerms != null)
            {
                foreach (var term in context.MustIncludeTerms)
                {
                    if (!contentLower.Contains(term.ToLowerInvariant()))
                    {
                        issues.Add(new ValidationIssue
                        {
                            ValidatorType = Type,
                            Severity = ValidationSeverity.Medium,
                            Message = $"Required term missing: '{term}'",
                            AutoFixable = false
                        });
                        score -= 0.15f;
                    }
                }
            }

            // Check for must-exclude terms
            if (context.MustExcludeTerms != null)
            {
                foreach (var term in context.MustExcludeTerms)
                {
                    if (contentLower.Contains(term.ToLowerInvariant()))
                    {
                        issues.Add(new ValidationIssue
                        {
                            ValidatorType = Type,
                            Severity = ValidationSeverity.High,
                            Message = $"Forbidden term found: '{term}'",
                            AutoFixable = false
                        });
                        score -= 0.3f;
                    }
                }
            }

            return Task.FromResult((Math.Max(0, score), issues));
        }

        public Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
