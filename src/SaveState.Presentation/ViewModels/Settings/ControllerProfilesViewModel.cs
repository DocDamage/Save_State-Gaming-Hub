using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Input.Commands;
using SaveState.Application.Input.Queries;
using SaveState.Core.Input.Entities;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for managing controller profiles.
/// </summary>
public partial class ControllerProfilesViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ControllerProfilesViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ControllerProfileItemViewModel? _selectedProfile;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ControllerType? _filterType;

    [ObservableProperty]
    private bool _showGlobalProfilesOnly;

    [ObservableProperty]
    private bool _isCreateDialogVisible;

    [ObservableProperty]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private ControllerType _newProfileType = ControllerType.Xbox;

    [ObservableProperty]
    private Guid? _newProfileGameId;

    public ControllerProfilesViewModel(
        IMediator mediator,
        INotificationService notificationService,
        ILogger<ControllerProfilesViewModel> logger)
    {
        _mediator = mediator;
        _notificationService = notificationService;
        _logger = logger;

        Profiles = new ObservableCollection<ControllerProfileItemViewModel>();
        AvailableTypes = new ObservableCollection<ControllerType>
        {
            ControllerType.Xbox,
            ControllerType.PlayStation,
            ControllerType.Nintendo,
            ControllerType.SteamDeck,
            ControllerType.Generic,
            ControllerType.Keyboard
        };

        // Load profiles on initialization
        _ = LoadProfilesAsync();
    }

    /// <summary>
    /// Gets the collection of controller profiles.
    /// </summary>
    public ObservableCollection<ControllerProfileItemViewModel> Profiles { get; }

    /// <summary>
    /// Gets the available controller types.
    /// </summary>
    public ObservableCollection<ControllerType> AvailableTypes { get; }

    /// <summary>
    /// Command to load controller profiles.
    /// </summary>
    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            Profiles.Clear();

            var query = new GetControllerProfilesQuery(
                GameId: null,
                Type: FilterType,
                IncludeGlobal: true);

            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var dto in result.Value)
                {
                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(SearchText) &&
                        !dto.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Apply global filter
                    if (ShowGlobalProfilesOnly && dto.GameId.HasValue)
                    {
                        continue;
                    }

                    Profiles.Add(new ControllerProfileItemViewModel(
                        dto.Id,
                        dto.Name,
                        dto.Type,
                        dto.GameId,
                        dto.IsDefault,
                        dto.LastUsedAt));
                }

                _logger.LogInformation("Loaded {Count} controller profiles", Profiles.Count);
            }
            else
            {
                _notificationService.ShowError($"Failed to load profiles: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load controller profiles");
            _notificationService.ShowError("Failed to load controller profiles");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to show the create profile dialog.
    /// </summary>
    [RelayCommand]
    private void ShowCreateDialog()
    {
        IsCreateDialogVisible = true;
        NewProfileName = string.Empty;
        NewProfileType = ControllerType.Xbox;
        NewProfileGameId = null;
    }

    /// <summary>
    /// Command to cancel creating a profile.
    /// </summary>
    [RelayCommand]
    private void CancelCreateDialog()
    {
        IsCreateDialogVisible = false;
    }

    /// <summary>
    /// Command to create a new controller profile.
    /// </summary>
    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            _notificationService.ShowWarning("Please enter a profile name");
            return;
        }

        try
        {
            var command = new CreateControllerProfileCommand(
                NewProfileName,
                NewProfileType,
                NewProfileGameId);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Profile created: {NewProfileName}");
                IsCreateDialogVisible = false;
                await LoadProfilesAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to create profile: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create controller profile");
            _notificationService.ShowError("Failed to create profile");
        }
    }

    /// <summary>
    /// Command to apply a controller profile.
    /// </summary>
    [RelayCommand]
    private async Task ApplyProfileAsync(ControllerProfileItemViewModel? profile)
    {
        if (profile == null) return;

        try
        {
            var command = new ApplyControllerProfileCommand(profile.Id, profile.GameId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Profile applied: {profile.Name}");
                await LoadProfilesAsync(); // Refresh to update LastUsedAt
            }
            else
            {
                _notificationService.ShowError($"Failed to apply profile: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply controller profile");
            _notificationService.ShowError("Failed to apply profile");
        }
    }

    /// <summary>
    /// Command to refresh the profiles list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadProfilesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadProfilesAsync();
    }

    partial void OnFilterTypeChanged(ControllerType? value)
    {
        _ = LoadProfilesAsync();
    }

    partial void OnShowGlobalProfilesOnlyChanged(bool value)
    {
        _ = LoadProfilesAsync();
    }
}

/// <summary>
/// View model for a single controller profile item.
/// </summary>
public class ControllerProfileItemViewModel
{
    public ControllerProfileItemViewModel(
        Guid id,
        string name,
        ControllerType type,
        Guid? gameId,
        bool isDefault,
        DateTime? lastUsedAt)
    {
        Id = id;
        Name = name;
        Type = type;
        GameId = gameId;
        IsDefault = isDefault;
        LastUsedAt = lastUsedAt;
    }

    public Guid Id { get; }
    public string Name { get; }
    public ControllerType Type { get; }
    public Guid? GameId { get; }
    public bool IsDefault { get; }
    public DateTime? LastUsedAt { get; }

    public string TypeIcon => Type switch
    {
        ControllerType.Xbox => "🎮",
        ControllerType.PlayStation => "🎮",
        ControllerType.Nintendo => "🎮",
        ControllerType.SteamDeck => "🎮",
        ControllerType.Keyboard => "⌨️",
        _ => "🕹️"
    };

    public string Scope => GameId.HasValue ? "Game-Specific" : "Global";
    public string LastUsedText => LastUsedAt.HasValue
        ? $"Last used: {LastUsedAt.Value:g}"
        : "Never used";
}
