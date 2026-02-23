using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Controls.Loading;
using SaveState.Presentation.Services.Animation;

namespace SaveState.Presentation.Extensions;

/// <summary>
/// Extension methods for easy loading state management in ViewModels.
/// 
/// These extensions provide a fluent API for wrapping async operations
/// with loading indicators, skeleton screens, and progress tracking.
/// 
/// Usage Example:
/// <code>
/// public class GameLibraryViewModel : ObservableObject
/// {
///     public async Task LoadGamesAsync()
///     {
///         var games = await this.WithLoadingAsync(
///             async () => await _gameService.GetGamesAsync(),
///             loadingMessage: "Loading games...");
///         
///         Games = games;
///     }
/// }
/// </code>
/// </summary>
public static class ViewModelLoadingExtensions
{
    /// <summary>
    /// Wraps an async operation with a loading state indicator.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model to track loading state on.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="loadingMessage">Optional message to display during loading.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithLoadingAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        string? loadingMessage = null,
        ILogger? logger = null)
    {
        if (viewModel is ILoadingStateAware stateAware)
        {
            stateAware.IsLoading = true;
            stateAware.LoadingMessage = loadingMessage;
        }

        try
        {
            var result = await action();
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during loading operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (viewModel is ILoadingStateAware stateAware)
            {
                stateAware.IsLoading = false;
                stateAware.LoadingMessage = null;
            }
        }
    }

    /// <summary>
    /// Wraps an async operation with a loading state indicator (void return).
    /// </summary>
    /// <param name="viewModel">The view model to track loading state on.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="loadingMessage">Optional message to display during loading.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    public static async Task WithLoadingAsync(
        this ObservableObject viewModel,
        Func<Task> action,
        string? loadingMessage = null,
        ILogger? logger = null)
    {
        if (viewModel is ILoadingStateAware stateAware)
        {
            stateAware.IsLoading = true;
            stateAware.LoadingMessage = loadingMessage;
        }

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during loading operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (viewModel is ILoadingStateAware stateAware)
            {
                stateAware.IsLoading = false;
                stateAware.LoadingMessage = null;
            }
        }
    }

    /// <summary>
    /// Wraps an async operation with a skeleton loading overlay.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="skeletonContainer">The container to show skeleton overlay in.</param>
    /// <param name="animationService">The animation service.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithSkeletonLoadingAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        Control skeletonContainer,
        IAnimationService animationService,
        ILogger? logger = null)
    {
        await animationService.ShowSkeletonAsync(skeletonContainer);

        try
        {
            var result = await action();
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during skeleton loading operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await animationService.HideSkeletonAsync(skeletonContainer);
        }
    }

    /// <summary>
    /// Wraps an async operation with a skeleton loading overlay (void return).
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="skeletonContainer">The container to show skeleton overlay in.</param>
    /// <param name="animationService">The animation service.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    public static async Task WithSkeletonLoadingAsync(
        this ObservableObject viewModel,
        Func<Task> action,
        Control skeletonContainer,
        IAnimationService animationService,
        ILogger? logger = null)
    {
        await animationService.ShowSkeletonAsync(skeletonContainer);

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during skeleton loading operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await animationService.HideSkeletonAsync(skeletonContainer);
        }
    }

    /// <summary>
    /// Wraps an async operation with a progress indicator.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model to track progress on.</param>
    /// <param name="action">The async operation that reports progress.</param>
    /// <param name="progress">The progress indicator control.</param>
    /// <param name="totalSteps">Total number of steps.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithProgressAsync<T>(
        this ObservableObject viewModel,
        Func<IProgress<ProgressReport>, Task<T>> action,
        ProgressIndicator progress,
        int totalSteps,
        ILogger? logger = null)
    {
        if (viewModel is IProgressStateAware progressAware)
        {
            progressAware.IsInProgress = true;
            progressAware.TotalProgressSteps = totalSteps;
            progressAware.CurrentProgressStep = 0;
        }

        var progressReporter = new Progress<ProgressReport>(report =>
        {
            if (viewModel is IProgressStateAware progressAware)
            {
                progressAware.CurrentProgressStep = report.Step;
                progressAware.ProgressMessage = report.Message;
                progressAware.ProgressPercentage = report.Percentage;
            }

            // Update the progress indicator if provided
            progress.CurrentStep = report.Step;
            progress.Message = report.Message;
        });

        try
        {
            var result = await action(progressReporter);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during progress operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (viewModel is IProgressStateAware progressAware)
            {
                progressAware.IsInProgress = false;
                progressAware.ProgressMessage = null;
            }
        }
    }

    /// <summary>
    /// Wraps an async operation with a spinner overlay.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="spinnerContainer">The element to show spinner on.</param>
    /// <param name="animationService">The animation service.</param>
    /// <param name="message">Optional loading message.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithSpinnerAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        Control spinnerContainer,
        IAnimationService animationService,
        string? message = null,
        ILogger? logger = null)
    {
        await animationService.ShowSpinnerAsync(spinnerContainer, message);

        try
        {
            var result = await action();
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during spinner operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await animationService.HideSpinnerAsync(spinnerContainer);
        }
    }

    /// <summary>
    /// Shows a skeleton loading state and manages the IsLoading property.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="skeletonContainer">The container to show skeleton in.</param>
    /// <param name="animationService">The animation service.</param>
    /// <param name="logger">Optional logger.</param>
    public static async Task WithFullLoadingAsync(
        this ObservableObject viewModel,
        Func<Task> action,
        Control skeletonContainer,
        IAnimationService animationService,
        ILogger? logger = null)
    {
        if (viewModel is ILoadingStateAware stateAware)
        {
            stateAware.IsLoading = true;
        }

        await animationService.ShowSkeletonAsync(skeletonContainer);

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during full loading operation: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await animationService.HideSkeletonAsync(skeletonContainer);

            if (viewModel is ILoadingStateAware stateAware)
            {
                stateAware.IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Retries an operation with loading state and exponential backoff.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="loadingMessage">Message to show during retries.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithRetryAndLoadingAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        int maxRetries = 3,
        string? loadingMessage = null,
        ILogger? logger = null)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            if (viewModel is ILoadingStateAware stateAware)
            {
                stateAware.IsLoading = true;
                stateAware.LoadingMessage = attempt > 0
                    ? $"{loadingMessage} (Retry {attempt}/{maxRetries})"
                    : loadingMessage;
            }

            try
            {
                var result = await action();

                if (viewModel is ILoadingStateAware stateAware)
                {
                    stateAware.IsLoading = false;
                    stateAware.LoadingMessage = null;
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                logger?.LogWarning(ex, "Attempt {Attempt} failed: {Message}", attempt, ex.Message);

                if (attempt < maxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
            finally
            {
                if (viewModel is ILoadingStateAware stateAware && attempt >= maxRetries)
                {
                    stateAware.IsLoading = false;
                    stateAware.LoadingMessage = null;
                }
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {maxRetries} attempts.",
            lastException);
    }

    /// <summary>
    /// Debounces an operation to prevent rapid successive calls.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="debounceMs">Debounce delay in milliseconds.</param>
    /// <param name="debounceKey">Unique key for this debounced operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> WithDebounceAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        int debounceMs = 300,
        string? debounceKey = null)
    {
        var key = debounceKey ?? action.Method.Name;
        var cts = DebounceCancellationTokens.GetOrAdd(key, _ => new CancellationTokenSource());

        try
        {
            cts.Cancel();
            cts.Dispose();
        }
        catch { }

        var newCts = new CancellationTokenSource();
        DebounceCancellationTokens[key] = newCts;

        try
        {
            await Task.Delay(debounceMs, newCts.Token);
        }
        catch (TaskCanceledException)
        {
            // Debounced - return default
            return default!;
        }

        return await action();
    }

    /// <summary>
    /// Throttles an operation to limit execution frequency.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="viewModel">The view model.</param>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="throttleMs">Minimum time between executions in milliseconds.</param>
    /// <param name="throttleKey">Unique key for this throttled operation.</param>
    /// <returns>The result of the operation, or default if throttled.</returns>
    public static async Task<T> WithThrottleAsync<T>(
        this ObservableObject viewModel,
        Func<Task<T>> action,
        int throttleMs = 1000,
        string? throttleKey = null)
    {
        var key = throttleKey ?? action.Method.Name;

        if (ThrottleLastExecution.TryGetValue(key, out var lastExecution))
        {
            var elapsed = DateTime.UtcNow - lastExecution;
            if (elapsed.TotalMilliseconds < throttleMs)
            {
                return default!;
            }
        }

        ThrottleLastExecution[key] = DateTime.UtcNow;
        return await action();
    }

    // Static dictionaries for debounce/throttle state
    private static readonly Dictionary<string, CancellationTokenSource> DebounceCancellationTokens = new();
    private static readonly Dictionary<string, DateTime> ThrottleLastExecution = new();
}

#region Supporting Interfaces

/// <summary>
/// Interface for view models that support loading state tracking.
/// </summary>
public interface ILoadingStateAware
{
    /// <summary>
    /// Gets or sets whether the view model is in a loading state.
    /// </summary>
    bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the current loading message.
    /// </summary>
    string? LoadingMessage { get; set; }
}

/// <summary>
/// Interface for view models that support progress tracking.
/// </summary>
public interface IProgressStateAware
{
    /// <summary>
    /// Gets or sets whether an operation is in progress.
    /// </summary>
    bool IsInProgress { get; set; }

    /// <summary>
    /// Gets or sets the current progress step.
    /// </summary>
    int CurrentProgressStep { get; set; }

    /// <summary>
    /// Gets or sets the total number of progress steps.
    /// </summary>
    int TotalProgressSteps { get; set; }

    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    double ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the current progress message.
    /// </summary>
    string? ProgressMessage { get; set; }
}

/// <summary>
/// Represents a progress report for long-running operations.
/// </summary>
public class ProgressReport
{
    /// <summary>
    /// Gets or sets the current step number.
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Gets or sets the progress message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Creates a progress report for the specified step.
    /// </summary>
    public static ProgressReport Create(int step, string message, double percentage)
    {
        return new ProgressReport
        {
            Step = step,
            Message = message,
            Percentage = percentage
        };
    }
}

#endregion
