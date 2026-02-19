using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces rate limiting on requests.
/// </summary>
public class RateLimitingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RateLimitingBehavior<TRequest, TResponse>> _logger;
    private readonly RateLimitingOptions _options;

    public RateLimitingBehavior(
        IRateLimiter rateLimiter,
        ITimeProvider timeProvider,
        ILogger<RateLimitingBehavior<TRequest, TResponse>> logger,
        IOptions<RateLimitingOptions> options)
    {
        _rateLimiter = rateLimiter;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip rate limiting if disabled
        if (!_options.Enabled)
        {
            return await next();
        }

        var operationName = GetOperationName(request);
        var rateLimitKey = GetRateLimitKey(request);

        // Check rate limit
        var isAllowed = await _rateLimiter.IsAllowedAsync(rateLimitKey, operationName, cancellationToken);

        if (!isAllowed)
        {
            var resetTimeResult = await _rateLimiter.GetResetTimeAsync(rateLimitKey, operationName, cancellationToken);
            var remaining = await _rateLimiter.GetRemainingOperationsAsync(rateLimitKey, operationName, cancellationToken);

            var resetTime = resetTimeResult.IsSuccess
                ? resetTimeResult.Value
                : new DateTimeOffset(_timeProvider.UtcNow, TimeSpan.Zero).AddMinutes(1);

            _logger.LogWarning("Rate limit exceeded for operation {Operation} with key {Key}. Reset time: {ResetTime}, Remaining: {Remaining}",
                operationName, rateLimitKey, resetTime, remaining);

            throw new RateLimitExceededException(operationName, resetTime);
        }

        // Record the operation
        await _rateLimiter.RecordOperationAsync(rateLimitKey, operationName, cancellationToken);

        // Continue with the request
        var response = await next();

        return response;
    }

    private static string GetOperationName(TRequest request)
    {
        // Extract operation name from request type
        var requestType = request.GetType();
        var operationName = requestType.Name;

        // Remove common suffixes
        if (operationName.EndsWith("Command"))
            operationName = operationName[..^7];
        else if (operationName.EndsWith("Query"))
            operationName = operationName[..^5];

        return operationName;
    }

    private static string GetRateLimitKey(TRequest request)
    {
        // Use machine name and user name for identification in desktop context
        // in place of a dedicated ICurrentUserService which is not yet available
        return $"{Environment.MachineName}_{Environment.UserName}";
    }
}

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    public string Operation { get; }
    public DateTimeOffset ResetTime { get; }

    public RateLimitExceededException(string operation, DateTimeOffset resetTime)
        : base($"Rate limit exceeded for operation '{operation}'. Try again after {resetTime}.")
    {
        Operation = operation;
        ResetTime = resetTime;
    }
}
