using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SaveState.Presentation.Utilities;

/// <summary>
/// Helper class for throttling search input to improve performance.
/// Prevents excessive database/API calls by debouncing rapid input changes.
/// </summary>
public sealed class SearchThrottleHelper : IDisposable
{
    private readonly Subject<string?> _searchSubject = new();
    private readonly IDisposable _subscription;
    private readonly Action<string?> _onSearch;
    private readonly TimeSpan _throttleInterval;

    /// <summary>
    /// Creates a new search throttle helper.
    /// </summary>
    /// <param name="onSearch">Action to execute when search should be performed.</param>
    /// <param name="throttleInterval">Interval to wait before executing search after input stops.</param>
    public SearchThrottleHelper(Action<string?> onSearch, TimeSpan? throttleInterval = null)
    {
        _onSearch = onSearch ?? throw new ArgumentNullException(nameof(onSearch));
        _throttleInterval = throttleInterval ?? TimeSpan.FromMilliseconds(300);

        _subscription = _searchSubject
            .Throttle(_throttleInterval)
            .DistinctUntilChanged()
            .Subscribe(query =>
            {
                try
                {
                    _onSearch(query);
                }
                catch (Exception)
                {
                    // Swallow exceptions to prevent subscription termination
                    // Caller should handle errors in their action
                }
            });
    }

    /// <summary>
    /// Updates the search text. The search action will be triggered after the throttle interval.
    /// </summary>
    /// <param name="searchText">The current search text.</param>
    public void UpdateSearchText(string? searchText)
    {
        _searchSubject.OnNext(searchText);
    }

    /// <summary>
    /// Triggers an immediate search without waiting for the throttle interval.
    /// </summary>
    /// <param name="searchText">The search text to use.</param>
    public void SearchImmediately(string? searchText)
    {
        _onSearch(searchText);
    }

    /// <summary>
    /// Disposes the throttle helper and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _subscription.Dispose();
        _searchSubject.Dispose();
    }
}

/// <summary>
/// Async version of the search throttle helper for async search operations.
/// </summary>
public sealed class AsyncSearchThrottleHelper : IDisposable
{
    private readonly Subject<string?> _searchSubject = new();
    private readonly IDisposable _subscription;
    private readonly Func<string?, CancellationToken, Task> _onSearchAsync;
    private readonly TimeSpan _throttleInterval;
    private CancellationTokenSource? _currentCts;

    /// <summary>
    /// Creates a new async search throttle helper.
    /// </summary>
    /// <param name="onSearchAsync">Async action to execute when search should be performed.</param>
    /// <param name="throttleInterval">Interval to wait before executing search after input stops.</param>
    public AsyncSearchThrottleHelper(Func<string?, CancellationToken, Task> onSearchAsync, TimeSpan? throttleInterval = null)
    {
        _onSearchAsync = onSearchAsync ?? throw new ArgumentNullException(nameof(onSearchAsync));
        _throttleInterval = throttleInterval ?? TimeSpan.FromMilliseconds(300);

        _subscription = _searchSubject
            .Throttle(_throttleInterval)
            .DistinctUntilChanged()
            .Subscribe(async query =>
            {
                // Cancel any previous search
                _currentCts?.Cancel();
                _currentCts = new CancellationTokenSource();

                try
                {
                    await _onSearchAsync(query, _currentCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a new search cancels the previous one
                }
                catch (Exception)
                {
                    // Swallow exceptions to prevent subscription termination
                    // Caller should handle errors in their action
                }
            });
    }

    /// <summary>
    /// Updates the search text. The search action will be triggered after the throttle interval.
    /// </summary>
    /// <param name="searchText">The current search text.</param>
    public void UpdateSearchText(string? searchText)
    {
        _searchSubject.OnNext(searchText);
    }

    /// <summary>
    /// Triggers an immediate search without waiting for the throttle interval.
    /// </summary>
    /// <param name="searchText">The search text to use.</param>
    public async Task SearchImmediatelyAsync(string? searchText)
    {
        _currentCts?.Cancel();
        _currentCts = new CancellationTokenSource();
        await _onSearchAsync(searchText, _currentCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the throttle helper and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _currentCts?.Cancel();
        _currentCts?.Dispose();
        _subscription.Dispose();
        _searchSubject.Dispose();
    }
}
