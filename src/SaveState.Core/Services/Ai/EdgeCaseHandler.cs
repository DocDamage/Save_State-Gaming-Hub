using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Comprehensive Edge Case Handler with:
    /// - Input sanitization and normalization
    /// - Injection attack detection
    /// - Resource exhaustion prevention
    /// - Graceful degradation strategies
    /// - Error recovery patterns
    /// - Defensive output processing
    /// - Smart truncation
    /// - Character encoding handling
    /// - Rate burst protection
    /// - Memory pressure management
    /// </summary>
    public enum EdgeCaseType
    {
        EmptyInput,
        TooLongInput,
        MalformedInput,
        InjectionAttempt,
        UnsupportedCharacters,
        RecursiveReference,
        ResourceExhaustion,
        RateLimitBurst,
        CircularDependency,
        InvalidFormat,
        MemoryPressure,
        ConcurrencyViolation,
        DataCorruption,
        TimeoutRisk,
        UnexpectedNull
    }

    public class EdgeCaseDetection
    {
        public EdgeCaseType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public float Severity { get; set; } // 0-1
        public bool AutoRecoverable { get; set; }
        public string? SuggestedAction { get; set; }
    }

    public class SanitizedInput
    {
        public string Original { get; set; } = string.Empty;
        public string Sanitized { get; set; } = string.Empty;
        public bool WasModified { get; set; }
        public List<EdgeCaseDetection> DetectedEdgeCases { get; set; } = new();
        public List<string> AppliedTransformations { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RecoveryResult
    {
        public bool Success { get; set; }
        public string StrategyUsed { get; set; } = string.Empty;
        public string? RecoveredValue { get; set; }
        public string? ErrorMessage { get; set; }
        public int AttemptsUsed { get; set; }
    }

    public class ResourceUsage
    {
        public long MemoryBytes { get; set; }
        public int ActiveRequests { get; set; }
        public int QueuedRequests { get; set; }
        public float CpuEstimate { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public interface IEdgeCaseHandler
    {
        SanitizedInput SanitizeInput(string input, SanitizationOptions? options = null);
        Task<SanitizedInput> SanitizeInputAsync(string input, SanitizationOptions? options = null);
        List<EdgeCaseDetection> DetectEdgeCases(string input);
        bool ShouldReject(string input, out string reason);
        Task<RecoveryResult> TryRecoverAsync<T>(Func<Task<T>> operation, RecoveryOptions? options = null);
        string TruncateSmart(string text, int maxLength, TruncationMode mode = TruncationMode.Sentence);
        string SanitizeOutput(string output, OutputSanitizationOptions? options = null);
        ResourceUsage GetCurrentResourceUsage();
        bool IsSystemUnderPressure();
        void ReportEdgeCase(EdgeCaseDetection edgeCase);
        EdgeCaseStatistics GetStatistics();
    }

    public enum TruncationMode
    {
        Hard,           // Cut at exact length
        Word,           // Cut at word boundary
        Sentence,       // Cut at sentence boundary
        Paragraph,      // Cut at paragraph boundary
        Semantic        // Try to find logical break point
    }

    public class SanitizationOptions
    {
        public int MaxLength { get; set; } = 50000;
        public bool RemoveControlCharacters { get; set; } = true;
        public bool NormalizeWhitespace { get; set; } = true;
        public bool DetectInjections { get; set; } = true;
        public bool NormalizeUnicode { get; set; } = true;
        public bool RemoveZeroWidthChars { get; set; } = true;
        public bool TruncateIfTooLong { get; set; } = true;
        public TruncationMode TruncationMode { get; set; } = TruncationMode.Sentence;
        public HashSet<string>? AllowedHtmlTags { get; set; }
        public bool StripHtml { get; set; } = true;
    }

    public class RecoveryOptions
    {
        public int MaxAttempts { get; set; } = 3;
        public int InitialDelayMs { get; set; } = 100;
        public float BackoffMultiplier { get; set; } = 2.0f;
        public int MaxDelayMs { get; set; } = 10000;
        public bool RetryOnTimeout { get; set; } = true;
        public bool RetryOnTransientError { get; set; } = true;
        public Func<Exception, bool>? ShouldRetry { get; set; }
        public Func<int, Task>? OnRetry { get; set; }
        public string? FallbackValue { get; set; }
    }

    public class OutputSanitizationOptions
    {
        public int MaxLength { get; set; } = 100000;
        public bool RemoveAiSelfReferences { get; set; } = true;
        public bool EnsureCompleteSentences { get; set; } = true;
        public bool FilterProfanity { get; set; } = false;
        public bool RemoveSystemMessages { get; set; } = true;
        public List<string>? MustNotContain { get; set; }
        public bool NormalizeFormatting { get; set; } = true;
    }

    public class EdgeCaseStatistics
    {
        public int TotalDetections { get; set; }
        public Dictionary<EdgeCaseType, int> DetectionsByType { get; set; } = new();
        public int InjectionAttemptsBlocked { get; set; }
        public int TruncationsApplied { get; set; }
        public int RecoveriesAttempted { get; set; }
        public int RecoveriesSuccessful { get; set; }
        public float AverageSeverity { get; set; }
    }

    public class EdgeCaseHandler : IEdgeCaseHandler
    {
        private readonly EdgeCaseConfig _config;
        private readonly ConcurrentQueue<EdgeCaseDetection> _recentDetections = new();
        private readonly ConcurrentDictionary<EdgeCaseType, int> _detectionCounts = new();
        private readonly SemaphoreSlim _resourceLock = new(1, 1);
        private int _activeOperations = 0;
        private int _recoveryAttempts = 0;
        private int _recoverySuccesses = 0;

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

        // Zero-width and invisible characters
        private static readonly char[] ZeroWidthChars = new[]
        {
            '\u200B', '\u200C', '\u200D', '\u2060', '\uFEFF',
            '\u00AD', '\u034F', '\u061C', '\u115F', '\u1160',
            '\u17B4', '\u17B5', '\u180E', '\u2000', '\u2001',
            '\u2002', '\u2003', '\u2004', '\u2005', '\u2006',
            '\u2007', '\u2008', '\u2009', '\u200A', '\u2028',
            '\u2029', '\u202F', '\u205F', '\u3000'
        };

        public EdgeCaseHandler(EdgeCaseConfig? config = null)
        {
            _config = config ?? new EdgeCaseConfig();
        }

        public SanitizedInput SanitizeInput(string input, SanitizationOptions? options = null)
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

            // Detect injections
            if (options.DetectInjections)
            {
                var injections = DetectInjectionAttempts(sanitized);
                if (injections.Any())
                {
                    edgeCases.AddRange(injections);
                    IncrementDetectionCount(EdgeCaseType.InjectionAttempt, injections.Count);
                }
            }

            // Check length
            if (sanitized.Length > options.MaxLength)
            {
                edgeCases.Add(new EdgeCaseDetection
                {
                    Type = EdgeCaseType.TooLongInput,
                    Description = $"Input length ({sanitized.Length}) exceeds maximum ({options.MaxLength})",
                    Severity = 0.4f,
                    AutoRecoverable = options.TruncateIfTooLong,
                    SuggestedAction = "Truncate input"
                });

                if (options.TruncateIfTooLong)
                {
                    sanitized = TruncateSmart(sanitized, options.MaxLength, options.TruncationMode);
                    result.AppliedTransformations.Add($"truncated_to_{options.MaxLength}");
                    result.WasModified = true;
                }
            }

            // Check for potential recursive patterns
            var recursivePatterns = DetectRecursivePatterns(sanitized);
            if (recursivePatterns.Any())
            {
                edgeCases.AddRange(recursivePatterns);
            }

            result.Sanitized = sanitized;
            result.DetectedEdgeCases = edgeCases;
            result.Metadata["original_length"] = input.Length;
            result.Metadata["sanitized_length"] = sanitized.Length;
            result.Metadata["edge_cases_found"] = edgeCases.Count;

            // Record statistics
            foreach (var ec in edgeCases)
            {
                ReportEdgeCase(ec);
            }

            return result;
        }

        public Task<SanitizedInput> SanitizeInputAsync(string input, SanitizationOptions? options = null)
        {
            return Task.Run(() => SanitizeInput(input, options));
        }

        public List<EdgeCaseDetection> DetectEdgeCases(string input)
        {
            var result = SanitizeInput(input, new SanitizationOptions { TruncateIfTooLong = false });
            return result.DetectedEdgeCases;
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

        public async Task<RecoveryResult> TryRecoverAsync<T>(Func<Task<T>> operation, RecoveryOptions? options = null)
        {
            options ??= new RecoveryOptions();
            Interlocked.Increment(ref _recoveryAttempts);

            var result = new RecoveryResult();
            var delay = options.InitialDelayMs;

            for (int attempt = 1; attempt <= options.MaxAttempts; attempt++)
            {
                result.AttemptsUsed = attempt;

                try
                {
                    var value = await operation();
                    result.Success = true;
                    result.RecoveredValue = value?.ToString();
                    result.StrategyUsed = attempt == 1 ? "first_attempt" : $"retry_{attempt - 1}";
                    Interlocked.Increment(ref _recoverySuccesses);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    result.ErrorMessage = "Operation was cancelled";
                    return result; // Don't retry cancellations
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;

                    // Check if we should retry this exception
                    var shouldRetry = options.ShouldRetry?.Invoke(ex) ?? ShouldRetryException(ex, options);
                    
                    if (!shouldRetry || attempt >= options.MaxAttempts)
                    {
                        // Use fallback if available
                        if (options.FallbackValue != null)
                        {
                            result.Success = true;
                            result.RecoveredValue = options.FallbackValue;
                            result.StrategyUsed = "fallback";
                            return result;
                        }
                        return result;
                    }

                    // Notify observer
                    if (options.OnRetry != null)
                    {
                        await options.OnRetry(attempt);
                    }

                    // Wait with backoff
                    await Task.Delay(delay);
                    delay = Math.Min((int)(delay * options.BackoffMultiplier), options.MaxDelayMs);
                }
            }

            return result;
        }

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
                result = TruncateSmart(result, options.MaxLength, TruncationMode.Sentence);
            }

            return result.Trim();
        }

        public ResourceUsage GetCurrentResourceUsage()
        {
            return new ResourceUsage
            {
                MemoryBytes = GC.GetTotalMemory(false),
                ActiveRequests = _activeOperations,
                QueuedRequests = 0, // Would need to be connected to actual queue
                CpuEstimate = 0, // Would need OS-level integration
                Timestamp = DateTime.UtcNow
            };
        }

        public bool IsSystemUnderPressure()
        {
            var usage = GetCurrentResourceUsage();
            
            // Check memory pressure
            if (usage.MemoryBytes > _config.MemoryPressureThresholdBytes)
                return true;

            // Check active operations
            if (usage.ActiveRequests > _config.MaxConcurrentOperations)
                return true;

            return false;
        }

        public void ReportEdgeCase(EdgeCaseDetection edgeCase)
        {
            _recentDetections.Enqueue(edgeCase);
            while (_recentDetections.Count > _config.MaxRecentDetections)
            {
                _recentDetections.TryDequeue(out _);
            }

            _detectionCounts.AddOrUpdate(edgeCase.Type, 1, (_, c) => c + 1);
        }

        public EdgeCaseStatistics GetStatistics()
        {
            var detections = _recentDetections.ToArray();
            return new EdgeCaseStatistics
            {
                TotalDetections = detections.Length,
                DetectionsByType = new Dictionary<EdgeCaseType, int>(_detectionCounts),
                InjectionAttemptsBlocked = _detectionCounts.GetValueOrDefault(EdgeCaseType.InjectionAttempt, 0),
                TruncationsApplied = _detectionCounts.GetValueOrDefault(EdgeCaseType.TooLongInput, 0),
                RecoveriesAttempted = _recoveryAttempts,
                RecoveriesSuccessful = _recoverySuccesses,
                AverageSeverity = detections.Any() ? detections.Average(d => d.Severity) : 0
            };
        }

        // ============ Private Helper Methods ============

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
            // Normalize to NFC form
            return text.Normalize(System.Text.NormalizationForm.FormC);
        }

        private List<EdgeCaseDetection> DetectInjectionAttempts(string text)
        {
            var detections = new List<EdgeCaseDetection>();
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

        private List<EdgeCaseDetection> DetectRecursivePatterns(string text)
        {
            var detections = new List<EdgeCaseDetection>();

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

            return detections;
        }

        private bool ShouldRetryException(Exception ex, RecoveryOptions options)
        {
            // Timeout exceptions
            if (ex is TimeoutException && options.RetryOnTimeout)
                return true;

            // Transient HTTP errors
            var message = ex.Message.ToLowerInvariant();
            if (options.RetryOnTransientError)
            {
                var transientIndicators = new[] { "timeout", "temporarily", "retry", "429", "503", "504", "connection" };
                if (transientIndicators.Any(i => message.Contains(i)))
                    return true;
            }

            return false;
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

            if (lastSection > maxLength * 0.4)
                return truncated.Substring(0, lastSection).Trim();

            return TruncateAtParagraphBoundary(text, maxLength);
        }

        private string EnsureCompleteSentences(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var trimmed = text.Trim();
            var sentenceEndings = new[] { '.', '!', '?', '"' };

            if (sentenceEndings.Contains(trimmed.LastOrDefault()))
                return trimmed;

            // Find last complete sentence
            for (int i = trimmed.Length - 1; i >= 0; i--)
            {
                if (sentenceEndings.Contains(trimmed[i]))
                {
                    // Make sure it's not an abbreviation
                    if (i > 2 && trimmed[i] == '.')
                    {
                        var prevChar = trimmed[i - 1];
                        if (char.IsUpper(prevChar) || char.IsDigit(prevChar))
                            continue; // Probably abbreviation
                    }
                    return trimmed.Substring(0, i + 1);
                }
            }

            return trimmed + "."; // Add period as fallback
        }

        private string NormalizeOutputFormatting(string text)
        {
            // Fix double spaces
            text = Regex.Replace(text, @"  +", " ");
            
            // Fix space before punctuation
            text = Regex.Replace(text, @"\s+([.,!?])", "$1");
            
            // Ensure space after punctuation
            text = Regex.Replace(text, @"([.,!?])([A-Za-z])", "$1 $2");
            
            // Fix multiple punctuation
            text = Regex.Replace(text, @"([.!?])\1+", "$1");

            return text;
        }

        private void IncrementDetectionCount(EdgeCaseType type, int count = 1)
        {
            _detectionCounts.AddOrUpdate(type, count, (_, c) => c + count);
        }
    }

    public class EdgeCaseConfig
    {
        public int AbsoluteMaxLength { get; set; } = 500000;
        public long MemoryPressureThresholdBytes { get; set; } = 500_000_000; // 500MB
        public int MaxConcurrentOperations { get; set; } = 100;
        public int MaxRecentDetections { get; set; } = 1000;
    }
}
