using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Splat;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the navigation service.
/// </summary>
public class NavigationService : ObservableObject, INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ObservableCollection<NavigationEntry> _history = new();
    private readonly Dictionary<Type, ObservableObject> _viewModelCache = new();

    private ObservableObject? _currentViewModel;
    private string _currentTab = "Dashboard";
    private NavigationEntry? _currentEntry;
    private bool _isInitialized = false;

    public NavigationService(
        ILogger<NavigationService> logger,
        IServiceProvider serviceProvider)
    {
        Console.WriteLine("[DEBUG] NavigationService constructor starting");
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Don't resolve ViewModels here - defer until first actual navigation
        // This avoids blocking during DI resolution chain
        Console.WriteLine("[DEBUG] NavigationService constructor finished");
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        Console.WriteLine("[DEBUG] NavigationService initializing Dashboard...");
        _currentViewModel = GetOrCreateViewModel(TabRegistry.Tabs["Dashboard"].ViewModelType);
        _currentEntry = new NavigationEntry("Dashboard", _currentViewModel.GetType(), null, DateTime.UtcNow);
        _isInitialized = true;
        Console.WriteLine("[DEBUG] NavigationService initialization complete");
    }

    /// <inheritdoc />
    public ObservableObject CurrentViewModel
    {
        get
        {
            EnsureInitialized();
            return _currentViewModel!;
        }
        private set
        {
            _isInitialized = true; // Mark as initialized if we set it manually
            SetProperty(ref _currentViewModel, value);
        }
    }

    /// <inheritdoc />
    public string CurrentTab
    {
        get => _currentTab;
        private set => SetProperty(ref _currentTab, value);
    }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<NavigationEntry> History => new(_history);

    /// <inheritdoc />
    public bool CanGoBack => _history.Count > 0;

    /// <inheritdoc />
    public async Task NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        await NavigateTo(typeof(TViewModel), null);
    }

    /// <inheritdoc />
    public async Task NavigateTo(string tabName)
    {
        await NavigateTo(tabName, null);
    }

    /// <inheritdoc />
    public async Task NavigateTo(string tabName, object parameter)
    {
        var tab = TabRegistry.GetTab(tabName);
        if (tab == null)
        {
            _logger.LogWarning("Attempted to navigate to unknown tab: {TabName}", tabName);
            return;
        }

        // Add current entry to history if different tab
        if (_currentEntry != null && _currentEntry.Tab != tabName)
        {
            _history.Add(_currentEntry);
        }

        // Create new entry
        var viewModel = GetOrCreateViewModel(tab.ViewModelType);
        var entry = new NavigationEntry(tabName, tab.ViewModelType, parameter, DateTime.UtcNow);

        // Update state
        CurrentTab = tabName;
        CurrentViewModel = viewModel;
        _currentEntry = entry;

        // Notify the view model if it implements INavigationAware
        if (viewModel is INavigationAware navigationAware)
        {
            await navigationAware.OnNavigatedTo(parameter);
        }

        _logger.LogInformation("Navigated to tab: {TabName}", tabName);

        // Raise event
        Navigated?.Invoke(this, new NavigationEventArgs(entry, NavigationDirection.Forward));
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (!CanGoBack)
        {
            _logger.LogWarning("Cannot go back - no history available");
            return;
        }

        var previousEntry = _history[^1];
        _history.RemoveAt(_history.Count - 1);

        // Navigate to previous entry
        var viewModel = GetOrCreateViewModel(previousEntry.ViewModelType);
        CurrentTab = previousEntry.Tab;
        CurrentViewModel = viewModel;
        _currentEntry = previousEntry;

        _logger.LogInformation("Navigated back to tab: {TabName}", previousEntry.Tab);

        // Raise event
        Navigated?.Invoke(this, new NavigationEventArgs(previousEntry, NavigationDirection.Backward));
    }

    /// <inheritdoc />
    public event EventHandler<NavigationEventArgs>? Navigated;

    private async Task NavigateTo(Type viewModelType, object? parameter)
    {
        // Find tab that matches this view model type
        var tab = TabRegistry.Tabs.FirstOrDefault(t => t.Value.ViewModelType == viewModelType);
        if (tab.Value != null)
        {
            await NavigateTo(tab.Key, parameter);
        }
        else
        {
            _logger.LogWarning("No tab registered for ViewModel type: {ViewModelType}", viewModelType.Name);
        }
    }

    private ObservableObject GetOrCreateViewModel(Type viewModelType)
    {
        if (_viewModelCache.TryGetValue(viewModelType, out var cached))
        {
            return cached;
        }

        try
        {
            var viewModel = (ObservableObject)_serviceProvider.GetService(viewModelType);
            if (viewModel == null)
            {
                _logger.LogError("Failed to resolve ViewModel of type: {ViewModelType}", viewModelType.Name);
                throw new InvalidOperationException($"Could not resolve ViewModel: {viewModelType.Name}");
            }

            _viewModelCache[viewModelType] = viewModel;
            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ViewModel of type: {ViewModelType}", viewModelType.Name);
            throw;
        }
    }
}
