using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Common;

/// <summary>
/// MediatR pipeline behavior for automatic logging of command and query handlers.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        using (_logger.BeginCorrelationScope())
        {
            _logger.LogInformation(
                "Handling {RequestName}",
                requestName);
                
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await next();
                
                stopwatch.Stop();
                
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
                    
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex,
                    "Failed to handle {RequestName} after {ElapsedMs}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
                    
                throw;
            }
        }
    }
}

