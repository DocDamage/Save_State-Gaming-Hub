using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Enhanced Output Validation with:
    /// - Multi-pass validation with different validators
    /// - Severity scoring for issues
    /// - Automatic repair for common issues
    /// - Safety/toxicity filtering
    /// - Lore consistency checking
    /// - Format validation
    /// - Length constraints
    /// - Hallucination detection
    /// - Performance tracking
    /// </summary>
    public enum ValidationSeverity
    {
        Critical = 0,   // Must fix before returning
        High = 1,       // Should fix if possible
        Medium = 2,     // Recommend fixing
        Low = 3,        // Minor issue
        Info = 4        // Just informational
    }

    public enum ValidatorType
    {
        Safety,
        LoreConsistency,
        FormatCheck,
        LengthCheck,
        ToneCheck,
        HallucinationDetection,
        GrammarCheck,
        RepetitionCheck,
        ContentPolicy,
        Custom
    }

    public class ValidationIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ValidatorType ValidatorType { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? StartIndex { get; set; }
        public int? EndIndex { get; set; }
        public string? SuggestedFix { get; set; }
        public bool AutoFixable { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    public class EnhancedValidationResult
    {
        public bool IsValid { get; set; }
        public bool HasCriticalIssues => Issues.Any(i => i.Severity == ValidationSeverity.Critical);
        public List<ValidationIssue> Issues { get; set; } = new();
        public string OriginalContent { get; set; } = string.Empty;
        public string? RepairedContent { get; set; }
        public bool WasRepaired { get; set; }
        public float OverallScore { get; set; }
        public Dictionary<ValidatorType, float> ValidatorScores { get; set; } = new();
        public TimeSpan ValidationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class EnhancedValidationContext
    {
        public string? ExpectedTone { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public List<string>? MustIncludeTerms { get; set; }
        public List<string>? MustExcludeTerms { get; set; }
        public Dictionary<string, bool>? ActiveFlags { get; set; }
        public List<string>? CanonicalFacts { get; set; }
        public float MinConfidence { get; set; } = 0.7f;
        public string? FormatType { get; set; }
        public bool AllowAutoRepair { get; set; } = true;
        public List<ValidatorType>? EnabledValidators { get; set; }
        public string? OriginalQuery { get; set; }
    }

    public interface IValidator
    {
        ValidatorType Type { get; }
        Task<(float score, List<ValidationIssue> issues)> ValidateAsync(string content, EnhancedValidationContext context);
        Task<string?> TryRepairAsync(string content, List<ValidationIssue> issues);
    }

    public interface IEnhancedOutputValidator
    {
        Task<EnhancedValidationResult> ValidateAsync(string content, EnhancedValidationContext? context = null);
        Task<EnhancedValidationResult> ValidateAndRepairAsync(string content, EnhancedValidationContext? context = null, int maxRepairAttempts = 3);
        void RegisterValidator(IValidator validator);
        void EnableValidator(ValidatorType type, bool enabled);
        ValidationStatistics GetStatistics();
    }

    public class ValidationStatistics
    {
        public int TotalValidations { get; set; }
        public int PassedValidations { get; set; }
        public int FailedValidations { get; set; }
        public int RepairedValidations { get; set; }
        public Dictionary<ValidatorType, int> IssuesByValidator { get; set; } = new();
        public Dictionary<ValidationSeverity, int> IssuesBySeverity { get; set; } = new();
        public TimeSpan AverageValidationTime { get; set; }
        public float AverageScore { get; set; }
    }

    public class EnhancedOutputValidator : IEnhancedOutputValidator
    {
        private readonly List<IValidator> _validators = new();
        private readonly HashSet<ValidatorType> _enabledValidators = new();
        private readonly EnhancedValidatorConfig _config;
        private readonly object _statsLock = new();
        
        // Statistics
        private int _totalValidations = 0;
        private int _passedValidations = 0;
        private int _failedValidations = 0;
        private int _repairedValidations = 0;
        private readonly ConcurrentDictionary<ValidatorType, int> _issuesByValidator = new();
        private readonly ConcurrentDictionary<ValidationSeverity, int> _issuesBySeverity = new();
        private long _totalValidationMs = 0;
        private float _totalScore = 0;

        public EnhancedOutputValidator(EnhancedValidatorConfig? config = null)
        {
            _config = config ?? new EnhancedValidatorConfig();
            RegisterDefaultValidators();
        }

        public async Task<EnhancedValidationResult> ValidateAsync(string content, EnhancedValidationContext? context = null)
        {
            var startTime = DateTime.UtcNow;
            context ??= new EnhancedValidationContext();

            var result = new EnhancedValidationResult
            {
                OriginalContent = content
            };

            // Edge case: Empty content
            if (string.IsNullOrWhiteSpace(content))
            {
                result.Issues.Add(new ValidationIssue
                {
                    ValidatorType = ValidatorType.FormatCheck,
                    Severity = ValidationSeverity.Critical,
                    Message = "Content is empty or whitespace only"
                });
                result.IsValid = false;
                return FinalizeResult(result, startTime);
            }

            // Determine which validators to run
            var validatorsToRun = context.EnabledValidators != null
                ? _validators.Where(v => context.EnabledValidators.Contains(v.Type))
                : _validators.Where(v => _enabledValidators.Contains(v.Type));

            // Run validators in parallel
            var validationTasks = validatorsToRun.Select(async v =>
            {
                try
                {
                    return await v.ValidateAsync(content, context);
                }
                catch (Exception ex)
                {
                    return (0f, new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            ValidatorType = v.Type,
                            Severity = ValidationSeverity.Low,
                            Message = $"Validator failed: {ex.Message}"
                        }
                    });
                }
            });

            var results = await Task.WhenAll(validationTasks);

            foreach (var (score, issues) in results)
            {
                if (issues.Any())
                {
                    var validatorType = issues.First().ValidatorType;
                    result.ValidatorScores[validatorType] = score;
                    result.Issues.AddRange(issues);
                }
            }

            // Calculate overall score
            result.OverallScore = result.ValidatorScores.Any() 
                ? result.ValidatorScores.Values.Average() 
                : 1.0f;

            // Determine validity
            result.IsValid = !result.HasCriticalIssues && result.OverallScore >= _config.MinPassingScore;

            return FinalizeResult(result, startTime);
        }

        public async Task<EnhancedValidationResult> ValidateAndRepairAsync(
            string content, EnhancedValidationContext? context = null, int maxRepairAttempts = 3)
        {
            context ??= new EnhancedValidationContext();
            var currentContent = content;
            EnhancedValidationResult? result = null;

            for (int attempt = 0; attempt < maxRepairAttempts; attempt++)
            {
                result = await ValidateAsync(currentContent, context);

                if (result.IsValid)
                {
                    break;
                }

                if (!context.AllowAutoRepair)
                {
                    break;
                }

                // Try to repair
                var repairableIssues = result.Issues
                    .Where(i => i.AutoFixable && i.Severity <= ValidationSeverity.High)
                    .ToList();

                if (!repairableIssues.Any())
                {
                    break;
                }

                var repaired = await TryRepairContent(currentContent, repairableIssues);
                
                if (repaired == null || repaired == currentContent)
                {
                    break;
                }

                currentContent = repaired;
                result.WasRepaired = true;
            }

            if (result != null)
            {
                result.RepairedContent = result.WasRepaired ? currentContent : null;
                
                if (result.WasRepaired)
                {
                    Interlocked.Increment(ref _repairedValidations);
                }
            }

            return result ?? new EnhancedValidationResult { OriginalContent = content };
        }

        public void RegisterValidator(IValidator validator)
        {
            _validators.Add(validator);
            _enabledValidators.Add(validator.Type);
        }

        public void EnableValidator(ValidatorType type, bool enabled)
        {
            if (enabled)
                _enabledValidators.Add(type);
            else
                _enabledValidators.Remove(type);
        }

        public ValidationStatistics GetStatistics()
        {
            return new ValidationStatistics
            {
                TotalValidations = _totalValidations,
                PassedValidations = _passedValidations,
                FailedValidations = _failedValidations,
                RepairedValidations = _repairedValidations,
                IssuesByValidator = new Dictionary<ValidatorType, int>(_issuesByValidator),
                IssuesBySeverity = new Dictionary<ValidationSeverity, int>(_issuesBySeverity),
                AverageValidationTime = _totalValidations > 0 
                    ? TimeSpan.FromMilliseconds(_totalValidationMs / _totalValidations) 
                    : TimeSpan.Zero,
                AverageScore = _totalValidations > 0 ? _totalScore / _totalValidations : 0
            };
        }

        // ============ Private Methods ============

        private async Task<string?> TryRepairContent(string content, List<ValidationIssue> issues)
        {
            var currentContent = content;

            // Group issues by validator type and repair
            var issuesByValidator = issues.GroupBy(i => i.ValidatorType);

            foreach (var group in issuesByValidator)
            {
                var validator = _validators.FirstOrDefault(v => v.Type == group.Key);
                if (validator != null)
                {
                    var repaired = await validator.TryRepairAsync(currentContent, group.ToList());
                    if (repaired != null)
                    {
                        currentContent = repaired;
                    }
                }
            }

            // Apply simple text-based repairs for issues with suggested fixes
            foreach (var issue in issues.Where(i => !string.IsNullOrEmpty(i.SuggestedFix) && 
                                                    i.StartIndex.HasValue && i.EndIndex.HasValue))
            {
                if (issue.StartIndex!.Value >= 0 && issue.EndIndex!.Value <= currentContent.Length)
                {
                    var before = currentContent.Substring(0, issue.StartIndex.Value);
                    var after = currentContent.Substring(issue.EndIndex.Value);
                    currentContent = before + issue.SuggestedFix + after;
                }
            }

            return currentContent;
        }

        private EnhancedValidationResult FinalizeResult(EnhancedValidationResult result, DateTime startTime)
        {
            result.ValidationTime = DateTime.UtcNow - startTime;

            // Update statistics
            Interlocked.Increment(ref _totalValidations);
            Interlocked.Add(ref _totalValidationMs, (long)result.ValidationTime.TotalMilliseconds);

            lock (_statsLock)
            {
                _totalScore += result.OverallScore;
            }

            if (result.IsValid)
                Interlocked.Increment(ref _passedValidations);
            else
                Interlocked.Increment(ref _failedValidations);

            foreach (var issue in result.Issues)
            {
                _issuesByValidator.AddOrUpdate(issue.ValidatorType, 1, (_, c) => c + 1);
                _issuesBySeverity.AddOrUpdate(issue.Severity, 1, (_, c) => c + 1);
            }

            return result;
        }

        private void RegisterDefaultValidators()
        {
            RegisterValidator(new SafetyValidator());
            RegisterValidator(new LengthValidator());
            RegisterValidator(new ToneValidator());
            RegisterValidator(new RepetitionValidator());
            RegisterValidator(new FormatValidator());
            RegisterValidator(new HallucinationValidator());
        }
    }

    // ============ Built-in Validators ============

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
                if (System.Text.RegularExpressions.Regex.IsMatch(content, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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
                result = System.Text.RegularExpressions.Regex.Replace(result, @"```\w*\n?", "");
                result = System.Text.RegularExpressions.Regex.Replace(result, @"#{1,6}\s+", "");
                result = System.Text.RegularExpressions.Regex.Replace(result, @"\*{1,2}([^*]+)\*{1,2}", "$1");
            }

            return Task.FromResult<string?>(result);
        }
    }

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

    public class EnhancedValidatorConfig
    {
        public float MinPassingScore { get; set; } = 0.6f;
        public int MaxRepairAttempts { get; set; } = 3;
        public bool EnableParallelValidation { get; set; } = true;
    }
}
