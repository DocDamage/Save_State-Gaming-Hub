using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Services;

/// <summary>
/// In-memory rate limiter implementation using sliding window algorithm.
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimiter> _logger;
    private readonly RateLimitingOptions _options;
    private readonly Dictionary<string, RateLimitRule> _rules;

    public RateLimiter(IMemoryCache cache, ILogger<RateLimiter> logger, IOptions<RateLimitingOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _rules = InitializeRateLimitRules();
    }

    public async Task<bool> IsAllowedAsync(string key, string operation, CancellationToken cancellationToken = default)
    {
        var rule = GetRuleForOperation(operation);
        if (rule == null)
            return true; // No rate limit for this operation

        var cacheKey = $"{operation}:{key}";
        var now = DateTimeOffset.UtcNow;

        // Get or create rate limit data for this key
        var rateLimitData = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = rule.WindowDuration;
            return Task.FromResult(new RateLimitData
            {
                Requests = new List<DateTimeOffset>(),
                WindowStart = now
            });
        });

        if (rateLimitData == null)
            return true;

        // Clean old requests outside the window
        rateLimitData.Requests.RemoveAll(r => now - r > rule.WindowDuration);

        // Check if under limit
        var isAllowed = rateLimitData.Requests.Count < rule.MaxRequests;

        if (isAllowed)
        {
            _logger.LogDebug("Rate limit check passed for operation {Operation} with key {Key}: {CurrentRequests}/{MaxRequests}",
                operation, key, rateLimitData.Requests.Count, rule.MaxRequests);
        }
        else
        {
            _logger.LogWarning("Rate limit exceeded for operation {Operation} with key {Key}: {CurrentRequests}/{MaxRequests}",
                operation, key, rateLimitData.Requests.Count, rule.MaxRequests);
        }

        return isAllowed;
    }

    public async Task RecordOperationAsync(string key, string operation, CancellationToken cancellationToken = default)
    {
        var rule = GetRuleForOperation(operation);
        if (rule == null)
            return; // No rate limit tracking for this operation

        var cacheKey = $"{operation}:{key}";
        var now = DateTimeOffset.UtcNow;

        var rateLimitData = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = rule.WindowDuration;
            return Task.FromResult(new RateLimitData
            {
                Requests = new List<DateTimeOffset>(),
                WindowStart = now
            });
        });

        if (rateLimitData != null)
        {
            rateLimitData.Requests.Add(now);
            // Keep only requests within the window
            rateLimitData.Requests.RemoveAll(r => now - r > rule.WindowDuration);
        }
    }

    public async Task<int> GetRemainingOperationsAsync(string key, string operation, CancellationToken cancellationToken = default)
    {
        var rule = GetRuleForOperation(operation);
        if (rule == null)
            return -1; // No limit

        var cacheKey = $"{operation}:{key}";
        var now = DateTimeOffset.UtcNow;

        var rateLimitData = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = rule.WindowDuration;
            return Task.FromResult(new RateLimitData
            {
                Requests = new List<DateTimeOffset>(),
                WindowStart = now
            });
        });

        if (rateLimitData == null)
            return rule.MaxRequests;

        // Clean old requests
        rateLimitData.Requests.RemoveAll(r => now - r > rule.WindowDuration);

        return Math.Max(0, rule.MaxRequests - rateLimitData.Requests.Count);
    }

    public Task<Result<DateTimeOffset>> GetResetTimeAsync(string key, string operation, CancellationToken cancellationToken = default)
    {
        var rule = GetRuleForOperation(operation);
        if (rule == null)
            return Task.FromResult(Result.Failure<DateTimeOffset>($"No rate limit rule found for operation: {operation}", ErrorType.Validation));

        var cacheKey = $"{operation}:{key}";
        var rateLimitData = _cache.Get<RateLimitData>(cacheKey);

        if (rateLimitData == null || rateLimitData.Requests.Count == 0)
            return Task.FromResult(Result.Failure<DateTimeOffset>("No rate limit data found for the specified key and operation", ErrorType.NotFound));

        // Reset time is when the oldest request in the current window expires
        var oldestRequest = rateLimitData.Requests.Min();
        return Task.FromResult(Result.Success<DateTimeOffset>(oldestRequest + rule.WindowDuration));
    }

    private RateLimitRule? GetRuleForOperation(string operation)
    {
        return _rules.GetValueOrDefault(operation);
    }

    private Dictionary<string, RateLimitRule> InitializeRateLimitRules()
    {
        if (!_options.Enabled)
        {
            return new Dictionary<string, RateLimitRule>(); // No rate limiting
        }

        return new Dictionary<string, RateLimitRule>
        {
            // Game import operations - moderate rate limiting
            ["ImportGame"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.ImportGame.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.ImportGame.WindowMinutes)
            },

            // Game launch operations - higher rate limiting
            ["LaunchGame"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.LaunchGame.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.LaunchGame.WindowMinutes)
            },

            // Metadata API calls - stricter rate limiting
            ["GetGameMetadata"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.GetGameMetadata.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.GetGameMetadata.WindowMinutes)
            },

            // Search operations - higher rate limiting
            ["SearchGames"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.SearchGames.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.SearchGames.WindowMinutes)
            },

            // File operations - moderate rate limiting
            ["ScanDirectory"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.ScanDirectory.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.ScanDirectory.WindowMinutes)
            },

            // AI operations - stricter rate limiting due to API costs
            ["ProcessAiRequest"] = new RateLimitRule
            {
                MaxRequests = _options.Operations.ProcessAiRequest.MaxRequests,
                WindowDuration = TimeSpan.FromMinutes(_options.Operations.ProcessAiRequest.WindowMinutes)
            }
        };
    }

    private class RateLimitData
    {
        public List<DateTimeOffset> Requests { get; set; } = new();
        public DateTimeOffset WindowStart { get; set; }
    }

    private class RateLimitRule
    {
        public int MaxRequests { get; set; }
        public TimeSpan WindowDuration { get; set; }
    }
}

