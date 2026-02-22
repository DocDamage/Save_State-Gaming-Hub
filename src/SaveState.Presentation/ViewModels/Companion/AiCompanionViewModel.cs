using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.AiCoOp.Models;
using SaveState.Core.AiCoOp.Services;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Companion;

/// <summary>
/// ViewModel for the AI Co-Op Companion interface.
/// Manages chat interaction, companion configuration, and game state display.
/// </summary>
public partial class AiCompanionViewModel : ObservableObject
{
    private readonly IAiCoOpCompanionService _companionService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AiCompanionViewModel> _logger;

    [ObservableProperty]
    private string _companionName = "AI Companion";

    [ObservableProperty]
    private CompanionPersonality _selectedPersonality = CompanionPersonality.Supportive;

    [ObservableProperty]
    private SkillLevel _selectedSkillLevel = SkillLevel.Equal;

    [ObservableProperty]
    private VoiceProfile _selectedVoice = VoiceProfile.Neutral;

    [ObservableProperty]
    private bool _proactiveSuggestions = true;

    [ObservableProperty]
    private bool _voiceEnabled = true;

    [ObservableProperty]
    private bool _takeControlAllowed = false;

    [ObservableProperty]
    private string _currentMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready to play!";

    [ObservableProperty]
    private string _gameId = string.Empty;

    [ObservableProperty]
    private string _currentLevel = string.Empty;

    [ObservableProperty]
    private float _playerHealth = 1.0f;

    [ObservableProperty]
    private int _enemyCount;

    [ObservableProperty]
    private string _currentObjective = string.Empty;

    [ObservableProperty]
    private bool _isCompanionActive;

    [ObservableProperty]
    private bool _showSettings;

    /// <summary>
    /// Collection of chat messages between player and companion.
    /// </summary>
    public ObservableCollection<CompanionChatMessage> ChatMessages { get; } = new();

    /// <summary>
    /// Available personality options.
    /// </summary>
    public IReadOnlyList<CompanionPersonality> Personalities { get; } = Enum.GetValues<CompanionPersonality>();

    /// <summary>
    /// Available skill level options.
    /// </summary>
    public IReadOnlyList<SkillLevel> SkillLevels { get; } = Enum.GetValues<SkillLevel>();

    /// <summary>
    /// Available voice profile options.
    /// </summary>
    public IReadOnlyList<VoiceProfile> VoiceProfiles { get; } = Enum.GetValues<VoiceProfile>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AiCompanionViewModel"/> class.
    /// </summary>
    public AiCompanionViewModel(
        IAiCoOpCompanionService companionService,
        ITimeProvider timeProvider,
        ILogger<AiCompanionViewModel> logger)
    {
        _companionService = companionService ?? throw new ArgumentNullException(nameof(companionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the companion with current configuration.
    /// </summary>
    [RelayCommand]
    private async Task InitializeCompanionAsync()
    {
        IsLoading = true;
        StatusMessage = "Initializing companion...";

        try
        {
            var config = new CompanionConfiguration
            {
                Name = CompanionName,
                Personality = SelectedPersonality,
                SkillLevel = SelectedSkillLevel,
                Voice = SelectedVoice,
                ProactiveSuggestions = ProactiveSuggestions,
                VoiceEnabled = VoiceEnabled,
                TakeControlAllowed = TakeControlAllowed
            };

            var result = await _companionService.InitializeCompanionAsync(config);

            if (result.IsSuccess)
            {
                IsCompanionActive = true;
                StatusMessage = $"{CompanionName} is ready to play!";
                ShowSettings = false;

                // Add welcome message
                await LoadChatHistoryAsync();
            }
            else
            {
                StatusMessage = $"Failed to initialize: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing companion");
            StatusMessage = "Error initializing companion";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sends a message to the companion.
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage))
            return;

        var message = CurrentMessage.Trim();
        CurrentMessage = string.Empty;

        IsLoading = true;

        try
        {
            // Add player message immediately to UI
            var playerMessage = new CompanionChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Player",
                Message = message,
                Timestamp = _timeProvider.UtcNow,
                IsVoice = false
            };
            ChatMessages.Add(playerMessage);

            // Generate response
            var result = await _companionService.GenerateResponseAsync(message);

            if (result.IsSuccess)
            {
                var companionMessage = new CompanionChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Sender = "Companion",
                    Message = result.Value,
                    Timestamp = _timeProvider.UtcNow,
                    IsVoice = VoiceEnabled
                };
                ChatMessages.Add(companionMessage);
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            StatusMessage = "Error sending message";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Processes a voice command.
    /// </summary>
    [RelayCommand]
    private async Task ProcessVoiceCommandAsync()
    {
        IsLoading = true;
        StatusMessage = "Listening...";

        try
        {
            // In a real implementation, this would use voice recognition
            // For now, we'll simulate with a placeholder
            var simulatedVoiceInput = "What should I do next?";

            var result = await _companionService.ProcessVoiceCommandAsync(simulatedVoiceInput);

            if (result.IsSuccess)
            {
                var companionMessage = new CompanionChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Sender = "Companion",
                    Message = result.Value,
                    Timestamp = _timeProvider.UtcNow,
                    IsVoice = true
                };
                ChatMessages.Add(companionMessage);
                StatusMessage = "Voice command processed";
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voice command");
            StatusMessage = "Error processing voice command";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets the next suggested action from the companion based on current game state.
    /// </summary>
    [RelayCommand]
    private async Task GetSuggestionAsync()
    {
        IsLoading = true;
        StatusMessage = "Analyzing game state...";

        try
        {
            var gameState = new GameStateSnapshot
            {
                GameId = GameId,
                CurrentLevel = CurrentLevel,
                PlayerPosition = "Unknown",
                PlayerHealth = PlayerHealth,
                EnemyCount = EnemyCount,
                CurrentObjective = CurrentObjective,
                NearbyItems = Array.Empty<string>(),
                SessionDuration = TimeSpan.FromMinutes(30)
            };

            var result = await _companionService.GetNextActionAsync(gameState);

            if (result.IsSuccess && result.Value.ActionType != "None")
            {
                var companionMessage = new CompanionChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Sender = "Companion",
                    Message = result.Value.VoiceLine ?? result.Value.Description,
                    Timestamp = _timeProvider.UtcNow,
                    IsVoice = VoiceEnabled
                };
                ChatMessages.Add(companionMessage);
                StatusMessage = $"Action: {result.Value.ActionType} (Confidence: {result.Value.Confidence:P0})";
            }
            else if (result.IsFailure)
            {
                StatusMessage = $"Error: {result.Error}";
            }
            else
            {
                StatusMessage = "No action needed at this time";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestion");
            StatusMessage = "Error getting suggestion";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles voice output on/off.
    /// </summary>
    [RelayCommand]
    private async Task ToggleVoiceAsync()
    {
        try
        {
            if (VoiceEnabled)
            {
                var result = await _companionService.EnableVoiceAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Voice enabled";
                }
            }
            else
            {
                var result = await _companionService.DisableVoiceAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Voice disabled";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling voice");
            StatusMessage = "Error toggling voice";
        }
    }

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    [RelayCommand]
    private async Task ClearChatAsync()
    {
        try
        {
            var result = await _companionService.ClearChatHistoryAsync();
            if (result.IsSuccess)
            {
                ChatMessages.Clear();
                StatusMessage = "Chat history cleared";
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat");
            StatusMessage = "Error clearing chat";
        }
    }

    /// <summary>
    /// Loads the chat history from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadChatHistoryAsync()
    {
        try
        {
            var result = await _companionService.GetChatHistoryAsync(50);
            if (result.IsSuccess)
            {
                ChatMessages.Clear();
                foreach (var message in result.Value)
                {
                    ChatMessages.Add(message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat history");
        }
    }

    /// <summary>
    /// Toggles the settings panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSettings()
    {
        ShowSettings = !ShowSettings;
    }

    /// <summary>
    /// Updates the companion configuration.
    /// </summary>
    [RelayCommand]
    private async Task UpdateConfigurationAsync()
    {
        IsLoading = true;
        StatusMessage = "Updating configuration...";

        try
        {
            var config = new CompanionConfiguration
            {
                Name = CompanionName,
                Personality = SelectedPersonality,
                SkillLevel = SelectedSkillLevel,
                Voice = SelectedVoice,
                ProactiveSuggestions = ProactiveSuggestions,
                VoiceEnabled = VoiceEnabled,
                TakeControlAllowed = TakeControlAllowed
            };

            var result = await _companionService.UpdateConfigurationAsync(config);

            if (result.IsSuccess)
            {
                StatusMessage = "Configuration updated";
                ShowSettings = false;
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            StatusMessage = "Error updating configuration";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnVoiceEnabledChanged(bool value)
    {
        _ = ToggleVoiceAsync();
    }
}
