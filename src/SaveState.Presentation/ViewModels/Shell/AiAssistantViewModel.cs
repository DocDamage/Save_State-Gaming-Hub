using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the AI assistant panel.
/// </summary>
public partial class AiAssistantViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ILogger<AiAssistantViewModel> _logger;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private ObservableCollection<MessageViewModel> _messages = new();

    public AiAssistantViewModel(
        IOverlayService overlayService,
        IAiOrchestrator aiOrchestrator,
        ILogger<AiAssistantViewModel> logger)
    {
        _overlayService = overlayService;
        _aiOrchestrator = aiOrchestrator;
        _logger = logger;

        // Welcome message
        Messages.Add(new MessageViewModel("AI", "Hello! I'm your gaming assistant. Ask me anything about your games, strategies, or for recommendations!", MessageType.Assistant));
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

            // Send to AI Orchestrator
            var response = await _aiOrchestrator.GenerateTextAsync(userMessage);

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
    private void StartVoiceInput()
    {
        // Voice input requires additional setup
        _logger.LogInformation("Voice input requested");
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
        _overlayService.HideAiAssistantOverlay();
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
