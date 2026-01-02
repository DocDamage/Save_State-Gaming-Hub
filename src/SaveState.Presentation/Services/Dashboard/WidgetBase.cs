using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SaveState.Presentation.Services.Dashboard;

/// <summary>
/// Base class for dashboard widgets.
/// </summary>
public abstract partial class WidgetBase : ObservableObject, IWidget
{
    private readonly ILogger _logger;
    private System.Timers.Timer? _refreshTimer;

    /// <summary>
    /// Gets whether the widget is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets the error message if loading failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets whether the widget is minimized.
    /// </summary>
    [ObservableProperty]
    private bool _isMinimized;

    protected WidgetBase(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <inheritdoc />
    public abstract string Icon { get; }

    /// <inheritdoc />
    public virtual WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public virtual WidgetSize[] SupportedSizes => new[] { DefaultSize };

    /// <inheritdoc />
    public virtual int RefreshIntervalMs => 30000; // 30 seconds

    /// <inheritdoc />
    public virtual bool CanMinimize => true;

    /// <inheritdoc />
    public virtual bool CanRemove => true;

    /// <inheritdoc />
    public virtual ObservableObject ViewModel => this;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await LoadDataAsync();

            // Start the refresh timer if refresh interval is set
            if (RefreshIntervalMs > 0)
            {
                _refreshTimer = new System.Timers.Timer(RefreshIntervalMs);
                _refreshTimer.Elapsed += async (sender, e) => await RefreshAsync();
                _refreshTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize widget {WidgetId}", Id);
            ErrorMessage = "Failed to load widget data";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <inheritdoc />
    public virtual async Task RefreshAsync()
    {
        try
        {
            ErrorMessage = null;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh widget {WidgetId}", Id);
            ErrorMessage = "Failed to refresh widget data";
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    /// <summary>
    /// Loads the widget data. Override this method in derived classes.
    /// </summary>
    protected virtual Task LoadDataAsync()
    {
        return Task.CompletedTask;
    }
}