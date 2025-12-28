using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
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

    public enum TruncationMode
    {
        Hard,           // Cut at exact length
        Word,           // Cut at word boundary
        Sentence,       // Cut at sentence boundary
        Paragraph,      // Cut at paragraph boundary
        Semantic        // Try to find logical break point
    }

    public class EdgeCaseConfig
    {
        public int AbsoluteMaxLength { get; set; } = 100000;
        public long MemoryPressureThresholdBytes { get; set; } = 1024 * 1024 * 512; // 512MB
        public int MaxConcurrentOperations { get; set; } = 100;
        public int MaxRecentDetections { get; set; } = 1000;
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
}
