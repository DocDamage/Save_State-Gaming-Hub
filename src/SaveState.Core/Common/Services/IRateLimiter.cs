namespace SaveState.Core.Common.Services;

/// <summary>
/// Interface for rate limiting operations to prevent abuse and ensure fair usage.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if the operation is allowed based on rate limits.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit (e.g., user ID, IP address)</param>
    /// <param name="operation">Name of the operation being rate limited</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation is allowed, false if rate limited</returns>
    Task<bool> IsAllowedAsync(string key, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful operation for rate limiting purposes.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit</param>
    /// <param name="operation">Name of the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordOperationAsync(string key, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining operations allowed for the key and operation.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit</param>
    /// <param name="operation">Name of the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of remaining operations, or -1 if no limit</returns>
    Task<int> GetRemainingOperationsAsync(string key, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time when the rate limit will reset for the key and operation.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit</param>
    /// <param name="operation">Name of the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Time when rate limit resets, or null if no limit</returns>
    Task<DateTimeOffset?> GetResetTimeAsync(string key, string operation, CancellationToken cancellationToken = default);
}
