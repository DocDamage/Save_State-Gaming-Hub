using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the AI assistant panel.
/// </summary>
public partial class AiAssistantViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly IUiGameContextService _gameContextService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<AiAssistantViewModel> _logger;
    private GameId? _activeGameId;
    private string _activeGameContext = string.Empty;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private ObservableCollection<MessageViewModel> _messages = new();

    public AiAssistantViewModel(
        IOverlayService overlayService,
        IAiOrchestrator aiOrchestrator,
        ISpeechRecognitionService speechRecognitionService,
        IUiGameContextService gameContextService,
        IGameRepository gameRepository,
        ILogger<AiAssistantViewModel> logger)
    {
        _overlayService = overlayService;
        _aiOrchestrator = aiOrchestrator;
        _speechRecognitionService = speechRecognitionService;
        _gameContextService = gameContextService;
        _gameRepository = gameRepository;
        _logger = logger;

        // Welcome message
        Messages.Add(new MessageViewModel("AI", "Hello! I'm your gaming assistant. Ask me anything about your games, strategies, or for recommendations!", MessageType.Assistant));

        _gameContextService.CurrentGameChanged += OnGameContextChanged;
        _ = RefreshActiveGameContextAsync();
    }

    /// <summary>
    /// Command to send a message to the AI.
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsProcessing)
            return;

        var userMessage = InputText.Trim();
        InputText = string.Empty;

        // Add user message
        Messages.Add(new MessageViewModel("You", userMessage, MessageType.User));

        try
        {
            IsProcessing = true;

            await RefreshActiveGameContextAsync();
            var prompt = BuildPrompt(userMessage);

            // Send to AI Orchestrator
            var response = await _aiOrchestrator.GenerateTextAsync(prompt);

            if (response.IsSuccess && response.Value != null)
            {
                Messages.Add(new MessageViewModel("AI", response.Value, MessageType.Assistant));
            }
            else
            {
                Messages.Add(new MessageViewModel("AI", "I'm having trouble processing that request. Please try again.", MessageType.Assistant));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process AI message");
            Messages.Add(new MessageViewModel("AI", "An error occurred. Please try again later.", MessageType.Assistant));
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Command to start voice input.
    /// </summary>
    [RelayCommand]
    private async Task StartVoiceInput()
    {
        if (_speechRecognitionService.IsContinuousRecognitionActive)
        {
            await _speechRecognitionService.StopContinuousRecognitionAsync();
            _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
        }
        else
        {
            _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
            var result = await _speechRecognitionService.StartContinuousRecognitionAsync();
            if (!result.IsSuccess)
            {
               _logger.LogError("Failed to start voice recognition: {Error}", result.Error);
               Messages.Add(new MessageViewModel("System", "Could not start voice recognition.", MessageType.Assistant));
               _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
            }
        }
    }

    private void OnSpeechRecognized(object? sender, Core.Ai.Services.DTOs.SpeechRecognizedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Result.RecognizedText))
        {
            // Append text to input
            // Dispatch to UI thread if needed, though ObservableProperty usually handles binding updates
            // But we are in an event handler, so ensure thread safety if needed.
            // Avalonia ViewModels are usually okay if binding ensures dispatch, but let's be safe.
            InputText = (InputText + " " + e.Result.RecognizedText).Trim();
        }
    }

    /// <summary>
    /// Command to clear the conversation.
    /// </summary>
    [RelayCommand]
    private void ClearConversation()
    {
        Messages.Clear();
        Messages.Add(new MessageViewModel("AI", "Conversation cleared. How can I help you?", MessageType.Assistant));
    }

    /// <summary>
    /// Command to close the AI assistant.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // Stop listening if active
        if (_speechRecognitionService.IsContinuousRecognitionActive)
        {
             _speechRecognitionService.StopContinuousRecognitionAsync();
             _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
        }
        _overlayService.HideAiAssistantOverlay();
    }

    private void OnGameContextChanged(object? sender, Game? game)
    {
        _ = RefreshActiveGameContextAsync();
    }

    private async Task RefreshActiveGameContextAsync()
    {
        try
        {
            var currentGame = _gameContextService.CurrentGame;
            GameId? activeGameId = currentGame != null ? GameId.From(currentGame.Id) : null;
            
            if (activeGameId == _activeGameId)
            {
                return;
            }

            _activeGameId = activeGameId;
            if (activeGameId == null)
            {
                _activeGameContext = string.Empty;
                return;
            }

            var game = await _gameRepository.GetByIdAsync(activeGameId);
            if (game == null)
            {
                _activeGameContext = string.Empty;
                return;
            }

            _activeGameContext = BuildGameContext(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh active game context");
            _activeGameContext = string.Empty;
        }
    }

    private static string BuildGameContext(Game game)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Active game context:");
        builder.AppendLine($"Title: {game.Title}");
        builder.AppendLine($"Platform: {game.Platform?.Name.Value ?? "Unknown"}");
        builder.AppendLine($"Status: {game.Status}");
        builder.AppendLine($"Completed: {(game.IsCompleted ? "Yes" : "No")}");
        builder.AppendLine($"Total playtime (hours): {game.TotalPlayTime.TotalHours:F1}");
        if (game.LastPlayedAt.HasValue)
        {
            builder.AppendLine($"Last played: {game.LastPlayedAt.Value:yyyy-MM-dd}");
        }

        return builder.ToString().Trim();
    }

    private string BuildPrompt(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(_activeGameContext))
        {
            return userMessage;
        }

        return $"{_activeGameContext}\n\nUser: {userMessage}";
    }
}

/// <summary>
/// View model for a chat message.
/// </summary>
public record MessageViewModel(string Sender, string Content, MessageType Type);

/// <summary>
/// Message type enumeration.
/// </summary>
public enum MessageType
{
    User,
    Assistant
}
