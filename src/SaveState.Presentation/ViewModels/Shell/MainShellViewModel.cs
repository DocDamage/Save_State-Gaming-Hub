using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using Splat;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the main application shell.
/// </summary>
public partial class MainShellViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MainShellViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    public MainShellViewModel(
        INavigationService navigationService,
        INotificationService notificationService,
        ILogger<MainShellViewModel> logger,
        ITimeProvider timeProvider)
    {
        _logger.LogDebug("MainShellViewModel constructor started");
        _navigationService = navigationService;
        _notificationService = notificationService;
        _logger = logger;
        _timeProvider = timeProvider;

        _logger.LogDebug("Creating TitleBarViewModel");
        // Initialize child view models
        TitleBarViewModel = new TitleBarViewModel();

        _logger.LogDebug("Creating HeaderBarViewModel");
        HeaderBarViewModel = new HeaderBarViewModel(navigationService, GetOverlayService());

        _logger.LogDebug("Creating StatusBarViewModel");
        // Resolve required services for StatusBarViewModel
        var loggerFactory = Locator.Current.GetService<ILoggerFactory>()!;
        var statusBarLogger = loggerFactory.CreateLogger<StatusBarViewModel>();
        var gameRepository = Locator.Current.GetService<SaveState.Core.GameLibrary.IGameRepository>()!;
        var analyticsService = Locator.Current.GetService<SaveState.Core.Analytics.Services.IAnalyticsService>()!;
        var performanceMonitor = Locator.Current.GetService<SaveState.Core.Performance.Services.IPerformanceMonitor>();
        StatusBarViewModel = new StatusBarViewModel(navigationService, GetOverlayService(), gameRepository, analyticsService, performanceMonitor, statusBarLogger, timeProvider);

        _logger.LogDebug("Creating OverlayContainerViewModel");
        var gameContextService = Locator.Current.GetService<IUiGameContextService>()!;
        OverlayContainerViewModel = new OverlayContainerViewModel(GetOverlayService(), gameContextService, timeProvider);

        // Note: We no longer call InitializeTabViewModels here.
        // The NavigationService will resolve them lazily as needed.

        _logger.LogDebug("Subscribing to navigation events");
        // Subscribe to navigation changes
        _navigationService.Navigated += OnNavigated;

        _logger.LogDebug("MainShellViewModel constructor finished");
        _logger.LogInformation("MainShellViewModel initialized");
    }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string Title => "SaveState Reborn";

    /// <summary>
    /// Gets the title bar view model.
    /// </summary>
    public TitleBarViewModel TitleBarViewModel { get; }

    /// <summary>
    /// Gets the header bar view model.
    /// </summary>
    public HeaderBarViewModel HeaderBarViewModel { get; }

    /// <summary>
    /// Gets the status bar view model.
    /// </summary>
    public StatusBarViewModel StatusBarViewModel { get; }

    /// <summary>
    /// Gets the overlay container view model.
    /// </summary>
    public OverlayContainerViewModel OverlayContainerViewModel { get; }

    /// <summary>
    /// Gets the notification service for displaying toast notifications.
    /// </summary>
    public INotificationService NotificationService => _notificationService;

    /// <summary>
    /// Gets the current view from navigation service.
    /// </summary>
    public ObservableObject CurrentView => _navigationService.CurrentViewModel;

    /// <summary>
    /// Disposes the view model and its children.
    /// </summary>
    public void Dispose()
    {
        StatusBarViewModel.Dispose();
        OverlayContainerViewModel.PerformanceHudViewModel.Dispose();
        _logger.LogInformation("MainShellViewModel disposed");
    }

    private void InitializeTabViewModels()
    {
        // Ensure all tab ViewModels are created and cached by the navigation service
        // This prevents errors when switching tabs
        var serviceProvider = Locator.Current.GetService<IServiceProvider>()!;
        foreach (var tab in TabRegistry.GetAllTabs())
        {
            try
            {
                var viewModel = serviceProvider.GetService(tab.ViewModelType);
                if (viewModel != null)
                {
                    _logger.LogDebug("Initialized tab ViewModel: {TabName}", tab.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize tab ViewModel: {TabName}", tab.Name);
            }
        }
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        // Notify that current view has changed
        OnPropertyChanged(nameof(CurrentView));
    }

    private IOverlayService GetOverlayService()
    {
        // Get the overlay service from the service locator
        // In a real app, this would be injected
        return Locator.Current.GetService<IOverlayService>()!;
    }
}
