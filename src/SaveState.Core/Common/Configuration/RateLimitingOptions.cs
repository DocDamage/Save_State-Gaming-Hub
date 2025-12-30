using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Common.Configuration;

/// <summary>
/// Configuration options for rate limiting behavior.
/// </summary>
public class RateLimitingOptions : IValidatableObject
{
    public const string Section = "RateLimiting";

    /// <summary>
    /// Whether rate limiting is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Global rate limit settings.
    /// </summary>
    public GlobalLimits Global { get; set; } = new();

    /// <summary>
    /// Operation-specific rate limit settings.
    /// </summary>
    public OperationLimits Operations { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Global != null)
        {
            var globalResults = Global.Validate(new ValidationContext(Global));
            results.AddRange(globalResults);
        }

        if (Operations != null)
        {
            var operationResults = Operations.Validate(new ValidationContext(Operations));
            results.AddRange(operationResults);
        }

        return results;
    }

    /// <summary>
    /// Global rate limiting configuration.
    /// </summary>
    public class GlobalLimits : IValidatableObject
    {
        /// <summary>
        /// Maximum requests per minute globally.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 1000;

        /// <summary>
        /// Maximum burst requests allowed.
        /// </summary>
        public int BurstLimit { get; set; } = 100;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (MaxRequestsPerMinute <= 0)
                results.Add(new ValidationResult("Global max requests per minute must be positive", new[] { nameof(MaxRequestsPerMinute) }));

            if (BurstLimit <= 0)
                results.Add(new ValidationResult("Global burst limit must be positive", new[] { nameof(BurstLimit) }));

            if (BurstLimit > MaxRequestsPerMinute)
                results.Add(new ValidationResult("Burst limit cannot exceed max requests per minute", new[] { nameof(BurstLimit) }));

            return results;
        }
    }

    /// <summary>
    /// Operation-specific rate limiting configuration.
    /// </summary>
    public class OperationLimits : IValidatableObject
    {
        /// <summary>
        /// Rate limits for game import operations.
        /// </summary>
        public OperationLimit ImportGame { get; set; } = new() { MaxRequests = 10, WindowMinutes = 1 };

        /// <summary>
        /// Rate limits for game launch operations.
        /// </summary>
        public OperationLimit LaunchGame { get; set; } = new() { MaxRequests = 30, WindowMinutes = 5 };

        /// <summary>
        /// Rate limits for metadata retrieval operations.
        /// </summary>
        public OperationLimit GetGameMetadata { get; set; } = new() { MaxRequests = 50, WindowMinutes = 10 };

        /// <summary>
        /// Rate limits for game search operations.
        /// </summary>
        public OperationLimit SearchGames { get; set; } = new() { MaxRequests = 100, WindowMinutes = 5 };

        /// <summary>
        /// Rate limits for directory scanning operations.
        /// </summary>
        public OperationLimit ScanDirectory { get; set; } = new() { MaxRequests = 5, WindowMinutes = 1 };

        /// <summary>
        /// Rate limits for AI processing operations.
        /// </summary>
        public OperationLimit ProcessAiRequest { get; set; } = new() { MaxRequests = 20, WindowMinutes = 1 };

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            var limits = new[]
            {
                (ImportGame, "ImportGame"),
                (LaunchGame, "LaunchGame"),
                (GetGameMetadata, "GetGameMetadata"),
                (SearchGames, "SearchGames"),
                (ScanDirectory, "ScanDirectory"),
                (ProcessAiRequest, "ProcessAiRequest")
            };

            foreach (var (limit, name) in limits)
            {
                if (limit != null)
                {
                    var limitResults = limit.Validate(new ValidationContext(limit));
                    foreach (var result in limitResults)
                    {
                        results.Add(new ValidationResult($"{name}: {result.ErrorMessage}", result.MemberNames));
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Configuration for a specific operation's rate limit.
    /// </summary>
    public class OperationLimit : IValidatableObject
    {
        /// <summary>
        /// Maximum number of requests allowed in the time window.
        /// </summary>
        public int MaxRequests { get; set; } = 10;

        /// <summary>
        /// Time window in minutes for the rate limit.
        /// </summary>
        public int WindowMinutes { get; set; } = 1;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (MaxRequests <= 0)
                results.Add(new ValidationResult("Max requests must be positive", new[] { nameof(MaxRequests) }));

            if (WindowMinutes <= 0)
                results.Add(new ValidationResult("Window minutes must be positive", new[] { nameof(WindowMinutes) }));

            return results;
        }
    }
}
