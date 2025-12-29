using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class ResilientMetadataService : IMetadataService
{
    private readonly IMetadataService _inner;
    private readonly ILogger<ResilientMetadataService> _logger;

    private readonly AsyncRetryPolicy<GameMetadata> _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy<GameMetadata> _circuitBreakerPolicy;
    private readonly AsyncPolicy<GameMetadata> _resiliencePolicy;

    public ResilientMetadataService(
        IMetadataService inner,
        ILogger<ResilientMetadataService> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Retry policy: 3 attempts with exponential backoff
        _retryPolicy = Policy<GameMetadata>
            .Handle<Exception>(ex => IsRetryableException(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, attempt, context) =>
                {
                    _logger.LogWarning(exception.Exception,
                        "Metadata request failed, retrying in {RetryDelay}s (attempt {Attempt}/3)",
                        timeSpan.TotalSeconds, attempt);
                });

        // Circuit breaker: Break after 5 failures within 60 seconds
        _circuitBreakerPolicy = Policy<GameMetadata>
            .Handle<Exception>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(60));

        // Combine policies: Retry -> Circuit Breaker -> Fallback
        _resiliencePolicy = Policy.WrapAsync(
            _retryPolicy,
            _circuitBreakerPolicy,
            Policy<GameMetadata>
                .Handle<Exception>()
                .FallbackAsync(GameMetadata.Empty)
        );
    }

    public async Task<GameMetadata> GetGameMetadataAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Empty title provided to GetGameMetadataAsync");
            return GameMetadata.Empty;
        }

        try
        {
            var result = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await _inner.GetGameMetadataAsync(title, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

            _logger.LogDebug("Successfully retrieved metadata for '{Title}'", title);
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open, cannot retrieve metadata for '{Title}'", title);
            return GameMetadata.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving metadata for '{Title}'", title);
            return GameMetadata.Empty;
        }
    }

    public async Task<Result<byte[]>> GetCoverImageAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Empty title provided to GetCoverImageAsync");
            return Result<byte[]>.Failure("Title is required", ErrorType.Validation);
        }

        try
        {
            // Use a separate retry policy for images (more lenient)
            var imageRetryPolicy = Policy<Result<byte[]>>
                .Handle<Exception>(ex => IsRetryableException(ex))
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt),
                    onRetry: (exception, timeSpan, attempt, context) =>
                    {
                        _logger.LogWarning(exception.Exception,
                            "Image download failed, retrying in {RetryDelay}s (attempt {Attempt}/2)",
                            timeSpan.TotalSeconds, attempt);
                    });

            var result = await imageRetryPolicy.ExecuteAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var innerResult = await _inner.GetCoverImageAsync(title, ct).ConfigureAwait(false);
                if (innerResult.IsFailure)
                {
                    return innerResult;
                }
                return innerResult.Value != null
                    ? Result<byte[]>.Success(innerResult.Value)
                    : Result<byte[]>.Failure("No cover image available", ErrorType.NotFound);
            }).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully downloaded cover image for '{Title}'", title);
            }
            else
            {
                _logger.LogDebug("No cover image available for '{Title}': {Error}", title, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download cover image for '{Title}'", title);
            return Result<byte[]>.Failure($"Image download failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static bool IsRetryableException(Exception exception)
    {
        // Consider network-related exceptions as retryable
        return exception is HttpRequestException ||
               exception is TimeoutException ||
               exception is OperationCanceledException ||
               (exception is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested);
    }

    // Circuit breaker state for monitoring
    public CircuitState CircuitBreakerState => _circuitBreakerPolicy.CircuitState;
}
