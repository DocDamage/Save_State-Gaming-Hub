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

    public class EnhancedValidatorConfig
    {
        public float MinPassingScore { get; set; } = 0.6f;
        public int MaxRepairAttempts { get; set; } = 3;
        public bool EnableParallelValidation { get; set; } = true;
    }
}
