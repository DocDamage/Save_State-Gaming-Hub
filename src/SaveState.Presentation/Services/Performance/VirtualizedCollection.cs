using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SaveState.Presentation.Services.Performance;

/// <summary>
/// Virtualized collection that loads items on-demand for better performance with large datasets.
/// </summary>
public class VirtualizedCollection<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Func<int, int, Task<IList<T>>> _itemsProvider;
    private readonly int _pageSize;
    private readonly Dictionary<int, T> _cachedItems = new();
    private int _count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public VirtualizedCollection(Func<int, int, Task<IList<T>>> itemsProvider, int totalCount, int pageSize = 50)
    {
        _itemsProvider = itemsProvider ?? throw new ArgumentNullException(nameof(itemsProvider));
        _count = totalCount;
        _pageSize = pageSize;
    }

    public int Count => _count;
    public bool IsReadOnly => true;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (!_cachedItems.TryGetValue(index, out var item))
            {
                // CAUTION: IList<T> indexer requires synchronous implementation.
                // This is an architectural constraint - IList<T> does not support async indexers.
                // The blocking call here is unavoidable for interface compliance.
                // RECOMMENDATION: Use PreloadRangeAsync() before accessing items to avoid blocking.
                #pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                try
                {
                    LoadPageAsync(index).GetAwaiter().GetResult();
                    return _cachedItems[index];
                }
                catch (InvalidOperationException)
                {
                    // Collection was modified during access - caller should preload for reliable access
                    return default!;
                }
                catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
                {
                    // Index access error - return default for graceful degradation
                    return default!;
                }
                #pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }

            return item;
        }
        set => throw new NotSupportedException("VirtualizedCollection is read-only");
    }

    private async Task LoadPageAsync(int index)
    {
        int pageIndex = index / _pageSize;
        int startIndex = pageIndex * _pageSize;
        int count = Math.Min(_pageSize, _count - startIndex);

        var items = await _itemsProvider(startIndex, count);

        for (int i = 0; i < items.Count; i++)
        {
            _cachedItems[startIndex + i] = items[i];
        }
    }

    /// <summary>
    /// Preloads a range of items for better performance.
    /// </summary>
    public async Task PreloadRangeAsync(int startIndex, int count)
    {
        int endIndex = Math.Min(startIndex + count, _count);
        for (int i = startIndex; i < endIndex; i += _pageSize)
        {
            await LoadPageAsync(i);
        }
    }

    /// <summary>
    /// Clears the cache to free memory.
    /// </summary>
    public void ClearCache()
    {
        _cachedItems.Clear();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    /// <summary>
    /// Updates the total count (e.g., after filtering).
    /// </summary>
    public void UpdateCount(int newCount)
    {
        _count = newCount;
        _cachedItems.Clear();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    #region IList Implementation

    public int IndexOf(T item) => throw new NotSupportedException();
    public void Insert(int index, T item) => throw new NotSupportedException();
    public void RemoveAt(int index) => throw new NotSupportedException();
    public void Add(T item) => throw new NotSupportedException();
    public void Clear() => ClearCache();
    public bool Contains(T item) => throw new NotSupportedException();
    public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
    public bool Remove(T item) => throw new NotSupportedException();

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
