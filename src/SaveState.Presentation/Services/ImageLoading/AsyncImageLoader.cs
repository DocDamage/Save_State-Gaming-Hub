using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Splat;
using System.IO;

namespace SaveState.Presentation.Services.ImageLoading;

/// <summary>
/// Service for asynchronously loading images with caching support.
/// Prevents UI blocking during image loading and provides memory-efficient caching.
/// </summary>
public interface IAsyncImageLoader
{
    /// <summary>
    /// Loads an image asynchronously from a file path or URL.
    /// </summary>
    /// <param name="source">The image source (file path or URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded bitmap, or null if loading failed.</returns>
    Task<Bitmap?> LoadImageAsync(string? source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached image if available, otherwise returns null.
    /// </summary>
    /// <param name="source">The image source.</param>
    /// <returns>The cached bitmap, or null if not in cache.</returns>
    Bitmap? GetCachedImage(string? source);

    /// <summary>
    /// Clears the image cache to free memory.
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Implementation of async image loader with memory caching.
/// </summary>
public class AsyncImageLoader : IAsyncImageLoader, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AsyncImageLoader> _logger;
    private readonly SemaphoreSlim _loadSemaphore;
    private readonly TimeSpan _cacheExpiration;
    private readonly int _maxConcurrentLoads;

    // Default placeholder image as embedded resource fallback
    private static Bitmap? _defaultPlaceholder;

    public AsyncImageLoader(
        ILogger<AsyncImageLoader> logger,
        int maxCacheSizeMB = 100,
        TimeSpan? cacheExpiration = null,
        int maxConcurrentLoads = 5)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(10);
        _maxConcurrentLoads = maxConcurrentLoads;
        _loadSemaphore = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);

        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = maxCacheSizeMB * 1024 * 1024, // Convert MB to bytes
            CompactionPercentage = 0.25,
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        };

        _cache = new MemoryCache(cacheOptions);
    }

    /// <inheritdoc />
    public async Task<Bitmap?> LoadImageAsync(string? source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(source, out Bitmap? cachedBitmap) && cachedBitmap != null)
        {
            return cachedBitmap;
        }

        // Limit concurrent image loads to prevent thread pool exhaustion
        await _loadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Double-check cache after acquiring semaphore
            if (_cache.TryGetValue(source, out cachedBitmap) && cachedBitmap != null)
            {
                return cachedBitmap;
            }

            var bitmap = await LoadImageInternalAsync(source, cancellationToken).ConfigureAwait(false);

            if (bitmap != null)
            {
                // Calculate approximate size (width * height * 4 bytes per pixel)
                var pixelSize = bitmap.PixelSize;
                var approximateSize = pixelSize.Width * pixelSize.Height * 4L;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(approximateSize)
                    .SetAbsoluteExpiration(_cacheExpiration)
                    .RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        if (value is Bitmap disposedBitmap)
                        {
                            // Bitmap disposal is handled by the cache eviction
                            // Note: Bitmap must be disposed on UI thread if it has visual references
                            // For cached images not currently displayed, we can dispose directly
                        }
                    });

                _cache.Set(source, bitmap, cacheEntryOptions);
            }

            return bitmap;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load image from {Source}", source);
            return null;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public Bitmap? GetCachedImage(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        _cache.TryGetValue(source, out Bitmap? cachedBitmap);
        return cachedBitmap;
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        // Create a new cache instance to clear all entries
        // Note: MemoryCache doesn't have a Clear method in older versions
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        _logger.LogInformation("Image cache cleared");
    }

    /// <summary>
    /// Gets the default placeholder image for use when loading fails or source is null.
    /// </summary>
    public static Bitmap GetDefaultPlaceholder()
    {
        if (_defaultPlaceholder == null)
        {
            // Create a simple gray placeholder
            var pixelSize = new PixelSize(200, 300);
            var bitmap = new WriteableBitmap(pixelSize, new Vector(96, 96), PixelFormat.Rgba8888);

            using (var locked = bitmap.Lock())
            {
                // Fill with a subtle gray color (#2D2D2D)
                var buffer = new byte[pixelSize.Width * pixelSize.Height * 4];
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    buffer[i] = 0x2D;     // B
                    buffer[i + 1] = 0x2D; // G
                    buffer[i + 2] = 0x2D; // R
                    buffer[i + 3] = 0xFF; // A
                }

                var ptr = locked.Address;
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, ptr, buffer.Length);
            }

            _defaultPlaceholder = bitmap;
        }

        return _defaultPlaceholder;
    }

    private static async Task<Bitmap?> LoadImageInternalAsync(string source, CancellationToken cancellationToken)
    {
        // Handle HTTP/HTTPS URLs
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var data = await httpClient.GetByteArrayAsync(source, cancellationToken).ConfigureAwait(false);
            using var stream = new MemoryStream(data);
            return new Bitmap(stream);
        }

        // Handle file paths
        if (File.Exists(source))
        {
            // Load file bytes first to avoid holding file handle open during decoding
            var fileBytes = await File.ReadAllBytesAsync(source, cancellationToken).ConfigureAwait(false);
            using var stream = new MemoryStream(fileBytes);
            return new Bitmap(stream);
        }

        // Handle app resources (avares://)
        if (source.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(source);
            var assets = Locator.Current.GetService<IAssetLoader>();
            if (assets != null && assets.Exists(uri))
            {
                await using var stream = assets.Open(uri);
                return new Bitmap(stream);
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_cache is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _loadSemaphore.Dispose();
    }
}

/// <summary>
/// Attached properties for enabling async image loading in XAML.
/// </summary>
public static class ImageLoader
{
    static ImageLoader()
    {
        SourceProperty.Changed.AddClassHandler<Image>((img, e) => OnSourceChanged(img, e));
    }

    /// <summary>
    /// Attached property for async image source.
    /// </summary>
    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>(
            "Source",
            typeof(ImageLoader),
            null,
            false,
            BindingMode.OneWay);

    /// <summary>
    /// Gets the async source value.
    /// </summary>
    public static string? GetSource(Image element)
    {
        return element.GetValue(SourceProperty);
    }

    /// <summary>
    /// Sets the async source value.
    /// </summary>
    public static void SetSource(Image element, string? value)
    {
        element.SetValue(SourceProperty, value);
    }

    private static void OnSourceChanged(Image image, AvaloniaPropertyChangedEventArgs e)
    {
        var newSource = e.GetNewValue<string?>();

        if (string.IsNullOrWhiteSpace(newSource))
        {
            image.Source = AsyncImageLoader.GetDefaultPlaceholder();
            return;
        }

        // Fire and forget async load
        _ = LoadImageAsync(image, newSource);
    }

    private static async Task LoadImageAsync(Image image, string source)
    {
        try
        {
            // Get the image loader from service locator
            var loader = Locator.Current.GetService<IAsyncImageLoader>();

            if (loader == null)
            {
                // Fallback to direct loading
                if (File.Exists(source))
                {
                    await using var stream = File.OpenRead(source);
                    var bitmap = new Bitmap(stream);
                    image.Source = bitmap;
                }
                return;
            }

            // Check cache first for instant display
            var cachedImage = loader.GetCachedImage(source);
            if (cachedImage != null)
            {
                image.Source = cachedImage;
                return;
            }

            // Show placeholder while loading
            image.Source = AsyncImageLoader.GetDefaultPlaceholder();

            // Load async
            var loadedImage = await loader.LoadImageAsync(source).ConfigureAwait(false);

            // Update on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (loadedImage != null && GetSource(image) == source)
                {
                    image.Source = loadedImage;
                }
            });
        }
        catch (Exception)
        {
            // On error, show placeholder
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                image.Source = AsyncImageLoader.GetDefaultPlaceholder();
            });
        }
    }
}
