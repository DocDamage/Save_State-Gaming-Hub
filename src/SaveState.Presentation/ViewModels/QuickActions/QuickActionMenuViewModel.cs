using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.QuickActions;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.QuickActions;
using SaveState.Presentation.Utilities;

namespace SaveState.Presentation.ViewModels.QuickActions;

/// <summary>
/// View model for the quick action menu overlay.
/// </summary>
public partial class QuickActionMenuViewModel : ObservableObject, IDisposable
{
    private readonly IQuickActionService _quickActionService;
    private readonly IOverlayService _overlayService;
    private readonly SearchThrottleHelper _searchThrottleHelper;
    private readonly ILogger<QuickActionMenuViewModel>? _logger;
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Initializes a new instance of the QuickActionMenuViewModel class.
    /// </summary>
    public QuickActionMenuViewModel(
        IQuickActionService quickActionService,
        IOverlayService overlayService)
    {
        _quickActionService = quickActionService;
        _overlayService = overlayService;
        _logger = Splat.Locator.Current.GetService<ILoggerFactory>()?.CreateLogger<QuickActionMenuViewModel>();

        _searchThrottleHelper = new SearchThrottleHelper(
            _ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    await SearchActionsAsync();
                });
            },
            TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Gets or sets the collection of action groups.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<QuickActionGroup> _actionGroups = new();

    /// <summary>
    /// Gets or sets the filtered actions based on search.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<QuickAction> _filteredActions = new();

    /// <summary>
    /// Gets or sets the search query text.
    /// </summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _searchThrottleHelper.UpdateSearchText(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current context for the menu.
    /// </summary>
    [ObservableProperty]
    private QuickActionContext _currentContext = QuickActionContext.Empty;

    /// <summary>
    /// Gets or sets whether the menu is open.
    /// </summary>
    [ObservableProperty]
    private bool _isOpen;

    /// <summary>
    /// Gets or sets the position where the menu was opened.
    /// </summary>
    [ObservableProperty]
    private Avalonia.Point _openPosition;

    /// <summary>
    /// Gets or sets the currently selected action.
    /// </summary>
    [ObservableProperty]
    private QuickAction? _selectedAction;

    /// <summary>
    /// Gets or sets whether the search has focus.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchFocused = true;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets whether there are any filtered actions.
    /// </summary>
    public bool HasFilteredActions => FilteredActions.Count > 0;

    /// <summary>
    /// Gets whether there are any action groups.
    /// </summary>
    public bool HasActionGroups => ActionGroups.Count > 0;

    /// <summary>
    /// Initializes the view model with a list of actions.
    /// </summary>
    /// <param name="actions">The actions to organize into groups.</param>
    public void InitializeWithActions(List<QuickAction> actions)
    {
        // Group actions by category
        var groups = actions
            .GroupBy(a => a.Category)
            .OrderBy(g => GetCategoryPriority(g.Key))
            .Select(g => new QuickActionGroup
            {
                Name = GetCategoryDisplayName(g.Key),
                Category = g.Key,
                Icon = GetCategoryIcon(g.Key),
                Priority = GetCategoryPriority(g.Key),
                Actions = g.OrderByDescending(a => a.Priority)
                          .ThenBy(a => a.Label)
                          .ToList(),
                IsExpanded = true
            })
            .ToList();

        ActionGroups = new ObservableCollection<QuickActionGroup>(groups);

        // Initialize filtered actions with all actions
        FilteredActions = new ObservableCollection<QuickAction>(actions);

        OnPropertyChanged(nameof(HasFilteredActions));
        OnPropertyChanged(nameof(HasActionGroups));

        _logger?.LogDebug("Initialized with {ActionCount} actions in {GroupCount} groups",
            actions.Count, groups.Count);
    }

    /// <summary>
    /// Executes the specified quick action.
    /// </summary>
    [RelayCommand]
    private async Task ExecuteActionAsync(QuickAction? action)
    {
        if (action == null)
        {
            return;
        }

        if (!action.IsEnabled)
        {
            StatusMessage = "This action is currently disabled.";
            return;
        }

        try
        {
            StatusMessage = $"Executing {action.Label}...";
            await _quickActionService.ExecuteActionAsync(action.Id, CurrentContext);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute action {ActionId}", action.Id);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches actions based on the current query.
    /// </summary>
    [RelayCommand]
    private async Task SearchActionsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Show all grouped actions
            var allActions = ActionGroups.SelectMany(g => g.Actions).ToList();
            FilteredActions = new ObservableCollection<QuickAction>(allActions);
        }
        else
        {
            // Search and flatten results
            var results = _quickActionService.SearchActions(SearchQuery, CurrentContext);
            FilteredActions = new ObservableCollection<QuickAction>(results);
        }

        OnPropertyChanged(nameof(HasFilteredActions));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Closes the menu.
    /// </summary>
    [RelayCommand]
    private async Task CloseMenuAsync()
    {
        SearchQuery = string.Empty;
        StatusMessage = string.Empty;

        // Close the menu window
        RequestClose?.Invoke(this, EventArgs.Empty);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles keyboard navigation.
    /// </summary>
    public bool HandleKey(Avalonia.Input.Key key)
    {
        switch (key)
        {
            case Avalonia.Input.Key.Escape:
                _ = CloseMenuAsync();
                return true;

            case Avalonia.Input.Key.Enter:
                if (SelectedAction != null)
                {
                    _ = ExecuteActionAsync(SelectedAction);
                }
                return true;

            case Avalonia.Input.Key.Up:
                NavigateSelection(-1);
                return true;

            case Avalonia.Input.Key.Down:
                NavigateSelection(1);
                return true;

            case Avalonia.Input.Key.Home:
                SelectFirst();
                return true;

            case Avalonia.Input.Key.End:
                SelectLast();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Event raised when the view model requests the view to close.
    /// </summary>
    public event EventHandler? RequestClose;

    private void NavigateSelection(int direction)
    {
        var actions = FilteredActions.Count > 0
            ? FilteredActions.ToList()
            : ActionGroups.SelectMany(g => g.Actions).ToList();

        if (actions.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedAction != null
            ? actions.IndexOf(SelectedAction)
            : -1;

        var newIndex = currentIndex + direction;
        newIndex = Math.Clamp(newIndex, 0, actions.Count - 1);

        SelectedAction = actions[newIndex];
    }

    private void SelectFirst()
    {
        var actions = FilteredActions.Count > 0
            ? FilteredActions.ToList()
            : ActionGroups.SelectMany(g => g.Actions).ToList();

        if (actions.Count > 0)
        {
            SelectedAction = actions[0];
        }
    }

    private void SelectLast()
    {
        var actions = FilteredActions.Count > 0
            ? FilteredActions.ToList()
            : ActionGroups.SelectMany(g => g.Actions).ToList();

        if (actions.Count > 0)
        {
            SelectedAction = actions[^1];
        }
    }

    private static string GetCategoryDisplayName(QuickActionCategory category)
    {
        return category switch
        {
            QuickActionCategory.Game => "GAME",
            QuickActionCategory.SaveState => "SAVE STATE",
            QuickActionCategory.Screenshot => "SCREENSHOT",
            QuickActionCategory.Recording => "RECORDING",
            QuickActionCategory.Social => "SOCIAL",
            QuickActionCategory.Settings => "SETTINGS",
            QuickActionCategory.Tools => "TOOLS",
            QuickActionCategory.Help => "HELP",
            QuickActionCategory.Navigation => "NAVIGATION",
            QuickActionCategory.Edit => "EDIT",
            QuickActionCategory.View => "VIEW",
            QuickActionCategory.File => "FILE",
            _ => category.ToString().ToUpperInvariant()
        };
    }

    private static string GetCategoryIcon(QuickActionCategory category)
    {
        return category switch
        {
            QuickActionCategory.Game => "🎮",
            QuickActionCategory.SaveState => "💾",
            QuickActionCategory.Screenshot => "📷",
            QuickActionCategory.Recording => "🎥",
            QuickActionCategory.Social => "👥",
            QuickActionCategory.Settings => "⚙️",
            QuickActionCategory.Tools => "🔧",
            QuickActionCategory.Help => "❓",
            QuickActionCategory.Navigation => "🧭",
            QuickActionCategory.Edit => "✏️",
            QuickActionCategory.View => "👁️",
            QuickActionCategory.File => "📁",
            _ => "•"
        };
    }

    private static int GetCategoryPriority(QuickActionCategory category)
    {
        return category switch
        {
            QuickActionCategory.Game => 1,
            QuickActionCategory.SaveState => 2,
            QuickActionCategory.Screenshot => 3,
            QuickActionCategory.Recording => 4,
            QuickActionCategory.Edit => 5,
            QuickActionCategory.View => 6,
            QuickActionCategory.Tools => 7,
            QuickActionCategory.Social => 8,
            QuickActionCategory.Navigation => 9,
            QuickActionCategory.Settings => 10,
            QuickActionCategory.Help => 11,
            QuickActionCategory.File => 12,
            _ => 99
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _searchThrottleHelper.Dispose();
    }
}
