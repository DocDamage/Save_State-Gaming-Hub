using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.EdgeCases;

namespace SaveState.Core.Services.Ai
{
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

    public class EdgeCaseHandler : IEdgeCaseHandler
    {
        private readonly IInputSanitizer _inputSanitizer;
        private readonly IInjectionDetector _injectionDetector;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IRecoveryCoordinator _recoveryCoordinator;
        private readonly IOutputValidator _outputValidator;
        private readonly ITextTruncator _textTruncator;
        private readonly IEdgeCaseStatisticsCollector _statisticsCollector;
        private readonly IPatternDetector _patternDetector;
        private readonly EdgeCaseConfig _config;

        public EdgeCaseHandler(
            IInputSanitizer inputSanitizer,
            IInjectionDetector injectionDetector,
            IResourceMonitor resourceMonitor,
            IRecoveryCoordinator recoveryCoordinator,
            IOutputValidator outputValidator,
            ITextTruncator textTruncator,
            IEdgeCaseStatisticsCollector statisticsCollector,
            IPatternDetector patternDetector,
            EdgeCaseConfig? config = null)
        {
            _inputSanitizer = inputSanitizer ?? throw new ArgumentNullException(nameof(inputSanitizer));
            _injectionDetector = injectionDetector ?? throw new ArgumentNullException(nameof(injectionDetector));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _recoveryCoordinator = recoveryCoordinator ?? throw new ArgumentNullException(nameof(recoveryCoordinator));
            _outputValidator = outputValidator ?? throw new ArgumentNullException(nameof(outputValidator));
            _textTruncator = textTruncator ?? throw new ArgumentNullException(nameof(textTruncator));
            _statisticsCollector = statisticsCollector ?? throw new ArgumentNullException(nameof(statisticsCollector));
            _patternDetector = patternDetector ?? throw new ArgumentNullException(nameof(patternDetector));
            _config = config ?? new EdgeCaseConfig();
        }

        public SanitizedInput SanitizeInput(string input, SanitizationOptions? options = null)
        {
            options ??= new SanitizationOptions();

            // 1. Basic cleaning
            var result = _inputSanitizer.Sanitize(input, options);
            var sanitized = result.Sanitized;

            // 2. Detect injections
            if (options.DetectInjections)
            {
                var injections = _injectionDetector.DetectInjectionAttempts(sanitized);
                if (injections.Any())
                {
                    result.DetectedEdgeCases.AddRange(injections);
                    _statisticsCollector.IncrementDetectionCount(EdgeCaseType.InjectionAttempt, injections.Count);
                }
            }

            // 3. Recursive patterns
            var recursivePatterns = _patternDetector.DetectRecursivePatterns(sanitized);
            if (recursivePatterns.Any())
            {
                result.DetectedEdgeCases.AddRange(recursivePatterns);
            }

            // 4. Truncation if needed
            if (sanitized.Length > options.MaxLength)
            {
                 result.DetectedEdgeCases.Add(new EdgeCaseDetection
                {
                    Type = EdgeCaseType.TooLongInput,
                    Description = $"Input length ({sanitized.Length}) exceeds maximum ({options.MaxLength})",
                    Severity = 0.4f,
                    AutoRecoverable = options.TruncateIfTooLong,
                    SuggestedAction = "Truncate input"
                });

                if (options.TruncateIfTooLong)
                {
                    sanitized = _textTruncator.TruncateSmart(sanitized, options.MaxLength, options.TruncationMode);
                    result.AppliedTransformations.Add($"truncated_to_{options.MaxLength}");
                    result.WasModified = true;
                    _statisticsCollector.IncrementDetectionCount(EdgeCaseType.TooLongInput);
                }
            }

            result.Sanitized = sanitized;
            result.Metadata["final_length"] = sanitized.Length;

            // Report all edge cases
            foreach (var ec in result.DetectedEdgeCases)
            {
                ReportEdgeCase(ec);
            }

            return result;
        }

        public Task<SanitizedInput> SanitizeInputAsync(string input, SanitizationOptions? options = null)
        {
            return _inputSanitizer.SanitizeAsync(input, options);
        }

        public List<EdgeCaseDetection> DetectEdgeCases(string input)
        {
            var result = SanitizeInput(input, new SanitizationOptions { TruncateIfTooLong = false });
            return result.DetectedEdgeCases;
        }

        public bool ShouldReject(string input, out string reason)
        {
            return _injectionDetector.ShouldReject(input, out reason);
        }

        public Task<RecoveryResult> TryRecoverAsync<T>(Func<Task<T>> operation, RecoveryOptions? options = null)
        {
            return _recoveryCoordinator.TryRecoverAsync(operation, options);
        }

        public string TruncateSmart(string text, int maxLength, TruncationMode mode = TruncationMode.Sentence)
        {
            return _textTruncator.TruncateSmart(text, maxLength, mode);
        }

        public string SanitizeOutput(string output, OutputSanitizationOptions? options = null)
        {
            return _outputValidator.SanitizeOutput(output, options);
        }

        public ResourceUsage GetCurrentResourceUsage()
        {
            return _resourceMonitor.GetCurrentUsage();
        }

        public bool IsSystemUnderPressure()
        {
            return _resourceMonitor.IsUnderPressure();
        }

        public void ReportEdgeCase(EdgeCaseDetection edgeCase)
        {
            _statisticsCollector.ReportEdgeCase(edgeCase);
        }

        public EdgeCaseStatistics GetStatistics()
        {
            return _statisticsCollector.GetStatistics(
                _recoveryCoordinator.GetRecoveryAttempts(),
                _recoveryCoordinator.GetRecoverySuccesses()
            );
        }
    }
}
