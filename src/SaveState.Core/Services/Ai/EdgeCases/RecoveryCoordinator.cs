using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Linq;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IRecoveryCoordinator
    {
        Task<RecoveryResult> TryRecoverAsync<T>(Func<Task<T>> operation, RecoveryOptions? options = null);
        bool ShouldRetryException(Exception ex, RecoveryOptions options);
        int GetRecoveryAttempts();
        int GetRecoverySuccesses();
    }

    public class RecoveryCoordinator : IRecoveryCoordinator
    {
        private int _recoveryAttempts = 0;
        private int _recoverySuccesses = 0;

        public int GetRecoveryAttempts() => _recoveryAttempts;
        public int GetRecoverySuccesses() => _recoverySuccesses;

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

        public bool ShouldRetryException(Exception ex, RecoveryOptions options)
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
    }
}
