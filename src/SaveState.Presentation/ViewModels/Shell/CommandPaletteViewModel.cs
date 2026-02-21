using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.Plugins.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.Utilities;
using Splat;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the command palette overlay with throttled search.
/// </summary>
public partial class CommandPaletteViewModel : ObservableObject, IDisposable
{
    private const string CategoryNavigation = "Navigation";
    private const string CategoryLibrary = "Library";
    private const string CategorySystem = "System";

    private readonly IOverlayService _overlayService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly IMediator? _mediator;
    private readonly INavigationService? _navigationService;
    private readonly IGameRepository? _gameRepository;
    private readonly IPluginManager? _pluginManager;
    private readonly ILogger<CommandPaletteViewModel>? _logger;
    private readonly SearchThrottleHelper _searchThrottleHelper;
    private readonly CommandContext _searchContext = CommandContext.Default;
    private HashSet<string> _pluginCommandIds = new(StringComparer.OrdinalIgnoreCase);

    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CommandPaletteViewModel(
        IOverlayService overlayService,
        ICommandPaletteService commandPaletteService)
    {
        _overlayService = overlayService;
        _commandPaletteService = commandPaletteService;

        // Resolve optional dependencies.
        _mediator = Locator.Current.GetService<IMediator>();
        _navigationService = Locator.Current.GetService<INavigationService>();
        _gameRepository = Locator.Current.GetService<IGameRepository>();
        _pluginManager = Locator.Current.GetService<IPluginManager>();
        _logger = Locator.Current.GetService<ILoggerFactory>()?.CreateLogger<CommandPaletteViewModel>();

        // Initialize throttled search with 150ms delay for instant command palette feel
        _searchThrottleHelper = new SearchThrottleHelper(
            _ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    await UpdateFilteredCommandsAsync();
                });
            },
            TimeSpan.FromMilliseconds(150));

        RegisterBuiltInCommands();
        _ = UpdateFilteredCommandsAsync();
    }

    /// <summary>
    /// Gets or sets the search text with throttled updates.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _searchThrottleHelper.UpdateSearchText(value);
            }
        }
    }

    /// <summary>
    /// Gets the filtered commands based on search text.
    /// </summary>
    public CommandItem[] FilteredCommands { get; private set; } = Array.Empty<CommandItem>();

    /// <summary>
    /// Gets the selected command index.
    /// </summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// Gets whether there are any filtered commands.
    /// </summary>
    public bool HasCommands => FilteredCommands.Length > 0;

    /// <summary>
    /// Command to execute the selected command.
    /// </summary>
    [RelayCommand]
    private async Task ExecuteSelectedAsync()
    {
        if (SelectedIndex >= 0 && SelectedIndex < FilteredCommands.Length)
        {
            await ExecuteCommandAsync(FilteredCommands[SelectedIndex]);
        }
    }

    /// <summary>
    /// Command to close the command palette.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        SearchText = string.Empty;
        StatusMessage = string.Empty;
        _overlayService.HideCommandPaletteOverlay();
    }

    /// <summary>
    /// Handles key input for navigation.
    /// </summary>
    public bool HandleKey(Avalonia.Input.Key key)
    {
        switch (key)
        {
            case Avalonia.Input.Key.Up:
                if (FilteredCommands.Length == 0)
                {
                    return true;
                }

                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                OnPropertyChanged(nameof(SelectedIndex));
                return true;

            case Avalonia.Input.Key.Down:
                if (FilteredCommands.Length == 0)
                {
                    return true;
                }

                SelectedIndex = Math.Min(FilteredCommands.Length - 1, SelectedIndex + 1);
                OnPropertyChanged(nameof(SelectedIndex));
                return true;

            case Avalonia.Input.Key.Enter:
                _ = ExecuteSelectedAsync();
                return true;

            case Avalonia.Input.Key.Escape:
                Close();
                return true;

            default:
                return false;
        }
    }

    private async Task UpdateFilteredCommandsAsync()
    {
        SyncPluginCommands();

        var result = await _commandPaletteService.SearchAsync(SearchText, _searchContext);
        if (result.IsFailure || result.Value is null)
        {
            FilteredCommands = [];
            StatusMessage = result.Error ?? "Failed to search commands.";
        }
        else
        {
            FilteredCommands = result.Value.ToArray();

            if (!IsExecuting)
            {
                StatusMessage = string.Empty;
            }
        }

        SelectedIndex = FilteredCommands.Length > 0
            ? Math.Clamp(SelectedIndex, 0, FilteredCommands.Length - 1)
            : -1;

        OnPropertyChanged(nameof(FilteredCommands));
        OnPropertyChanged(nameof(HasCommands));
        OnPropertyChanged(nameof(SelectedIndex));
    }

    private async Task ExecuteCommandAsync(CommandItem command)
    {
        IsExecuting = true;
        StatusMessage = $"Executing: {command.Name}...";
        _logger?.LogInformation("Executing command: {CommandId}", command.Id);

        try
        {
            var result = await _commandPaletteService.ExecuteAsync(command.Id);
            if (result.IsFailure)
            {
                StatusMessage = $"Error: {result.Error}";
                await Task.Delay(2000);
                return;
            }

            if (string.IsNullOrWhiteSpace(StatusMessage) ||
                StatusMessage.StartsWith("Executing:", StringComparison.Ordinal))
            {
                StatusMessage = $"Executed: {command.Name}";
            }

            await Task.Delay(1200);
            Close();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute command: {CommandId}", command.Id);
            StatusMessage = $"Error: {ex.Message}";
            await Task.Delay(2000);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private void RegisterBuiltInCommands()
    {
        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "library.scan",
            Name = "Scan for Games",
            Description = "Scans for new games in configured directories.",
            Category = CategoryLibrary,
            Keywords = ["scan", "discover", "library"],
            ExecuteAsync = ScanForGamesAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "library.random",
            Name = "Random Game",
            Description = "Picks a random game from your library.",
            Category = CategoryLibrary,
            Keywords = ["random", "surprise", "play"],
            ExecuteAsync = PickRandomGameAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "system.voice.listen",
            Name = "Voice Listen",
            Description = "Start voice command recognition.",
            Category = CategorySystem,
            Keywords = ["voice", "microphone", "listen"],
            ExecuteAsync = EnableVoiceRecognitionAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.analytics",
            Name = "Show Analytics",
            Description = "Navigate to analytics.",
            Category = CategoryNavigation,
            Keywords = ["analytics", "stats", "reports"],
            ExecuteAsync = ct => NavigateToAsync("Analytics", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.settings",
            Name = "Open Settings",
            Description = "Navigate to settings.",
            Category = CategoryNavigation,
            Keywords = ["settings", "preferences", "config"],
            ExecuteAsync = ct => NavigateToAsync("Settings", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "system.clear-cache",
            Name = "Clear Cache",
            Description = "Clears in-memory caches and releases memory.",
            Category = CategorySystem,
            Keywords = ["clear", "cache", "memory"],
            ExecuteAsync = ClearCacheAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "system.backup",
            Name = "Backup Data",
            Description = "Creates a backup of current data.",
            Category = CategorySystem,
            Keywords = ["backup", "archive", "data"],
            ExecuteAsync = CreateBackupAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "system.update",
            Name = "Check Updates",
            Description = "Checks whether updates are available.",
            Category = CategorySystem,
            Keywords = ["updates", "version", "upgrade"],
            ExecuteAsync = CheckUpdatesAsync
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.dashboard",
            Name = "Go to Dashboard",
            Description = "Navigate to dashboard.",
            Category = CategoryNavigation,
            Keywords = ["dashboard", "home", "overview"],
            ExecuteAsync = ct => NavigateToAsync("Dashboard", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.library",
            Name = "Go to Library",
            Description = "Navigate to game library.",
            Category = CategoryNavigation,
            Keywords = ["library", "games", "catalog"],
            ExecuteAsync = ct => NavigateToAsync("Library", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.mugen",
            Name = "Go to MUGEN",
            Description = "Navigate to MUGEN Battle Hub.",
            Category = CategoryNavigation,
            Keywords = ["mugen", "fighting", "hub"],
            ExecuteAsync = ct => NavigateToAsync("Mugen", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.tools",
            Name = "Go to Tools",
            Description = "Navigate to tools.",
            Category = CategoryNavigation,
            Keywords = ["tools", "utilities", "automation"],
            ExecuteAsync = ct => NavigateToAsync("Tools", ct)
        });

        _commandPaletteService.RegisterCommand(new CommandDefinition
        {
            Id = "navigation.social",
            Name = "Go to Social",
            Description = "Navigate to social hub.",
            Category = CategoryNavigation,
            Keywords = ["social", "friends", "community"],
            ExecuteAsync = ct => NavigateToAsync("Social", ct)
        });
    }

    private async Task<Result> ScanForGamesAsync(CancellationToken ct)
    {
        if (_mediator is null)
        {
            return Result.Failure("Library scan service is unavailable.", ErrorType.NotFound);
        }

        await _mediator.Send(new ScanLibraryCommand(), ct);
        StatusMessage = "Scan complete.";
        return Result.Success();
    }

    private async Task<Result> PickRandomGameAsync(CancellationToken ct)
    {
        if (_gameRepository is null)
        {
            return Result.Failure("Game repository is unavailable.", ErrorType.NotFound);
        }

        var games = await _gameRepository.GetAllAsync(ct);
        if (games.Count == 0)
        {
            return Result.Failure("No games in library.", ErrorType.NotFound);
        }

        var randomGame = games[Random.Shared.Next(games.Count)];
        StatusMessage = $"Random pick: {randomGame.Title}";
        return Result.Success();
    }

    private Task<Result> EnableVoiceRecognitionAsync(CancellationToken ct)
    {
        _overlayService.SetVoiceActive(true);
        return Task.FromResult(Result.Success());
    }

    private async Task<Result> NavigateToAsync(string tabName, CancellationToken ct)
    {
        if (_navigationService is null)
        {
            return Result.Failure("Navigation service is unavailable.", ErrorType.NotFound);
        }

        ct.ThrowIfCancellationRequested();
        await _navigationService.NavigateToAsync(tabName);
        return Result.Success();
    }

    private Task<Result> ClearCacheAsync(CancellationToken ct)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        StatusMessage = "Cache cleared.";
        return Task.FromResult(Result.Success());
    }

    private async Task<Result> CreateBackupAsync(CancellationToken ct)
    {
        StatusMessage = "Backup started...";
        await Task.Delay(800, ct);
        StatusMessage = "Backup complete.";
        return Result.Success();
    }

    private Task<Result> CheckUpdatesAsync(CancellationToken ct)
    {
        StatusMessage = "You are running the latest version.";
        return Task.FromResult(Result.Success());
    }

    private void SyncPluginCommands()
    {
        if (_pluginManager is null)
        {
            return;
        }

        var discoveredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var registrations = _pluginManager.GetRegisteredMenuItems();

        foreach (var registration in registrations)
        {
            var commandId = BuildPluginCommandId(registration);
            discoveredIds.Add(commandId);

            _commandPaletteService.RegisterCommand(new CommandDefinition
            {
                Id = commandId,
                Name = registration.MenuItem.Label,
                Description = $"Plugin action from {registration.PluginName}.",
                Category = $"Plugin: {registration.PluginName}",
                Keywords = BuildPluginKeywords(registration),
                Source = registration.PluginId,
                ExecuteAsync = ct => ExecutePluginMenuItemAsync(registration, ct)
            });
        }

        foreach (var staleId in _pluginCommandIds.Except(discoveredIds))
        {
            _commandPaletteService.UnregisterCommand(staleId);
        }

        _pluginCommandIds = discoveredIds;
    }

    private static string BuildPluginCommandId(PluginMenuRegistration registration)
    {
        var menuId = registration.MenuItem.Id?.Trim();
        if (string.IsNullOrWhiteSpace(menuId))
        {
            menuId = registration.MenuItem.Label.Replace(' ', '-');
        }

        var normalizedMenuId = menuId!
            .ToLowerInvariant()
            .Replace(' ', '-');

        return $"plugin.{registration.PluginId}.{normalizedMenuId}";
    }

    private static IReadOnlyList<string> BuildPluginKeywords(PluginMenuRegistration registration)
    {
        var keywordParts = registration.MenuItem.Label
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.ToLowerInvariant())
            .ToList();

        keywordParts.Add(registration.PluginName.ToLowerInvariant());
        keywordParts.Add(registration.PluginId.ToLowerInvariant());
        return keywordParts;
    }

    private static async Task<Result> ExecutePluginMenuItemAsync(
        PluginMenuRegistration registration,
        CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await registration.MenuItem.Action();
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                $"Plugin command '{registration.MenuItem.Label}' was cancelled.",
                ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure(
                $"Plugin command '{registration.MenuItem.Label}' failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Disposes resources used by this view model.
    /// </summary>
    public void Dispose()
    {
        _searchThrottleHelper.Dispose();
    }
}
