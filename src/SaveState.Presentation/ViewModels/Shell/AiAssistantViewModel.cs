using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the AI assistant panel.
/// </summary>
public partial class AiAssistantViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private string _inputText = string.Empty;

    public AiAssistantViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;

        // Initialize with example conversation
        Messages = new[]
        {
            new MessageViewModel("AI", "Hello! I'm your gaming assistant. How can I help you today?", MessageType.Assistant),
            new MessageViewModel("You", "How do I beat the Margit boss in Elden Ring?", MessageType.User),
            new MessageViewModel("AI", "Margit is a challenging early-game boss. Here are some tips:\n\n1. Level up to at least 25\n2. Use Spirit Ashes for distraction\n3. Roll through his delayed attacks\n4. Watch for his grab attack\n\nGood luck!", MessageType.Assistant)
        };
    }

    /// <summary>
    /// Gets or sets the input text.
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// <summary>
    /// Gets the conversation messages.
    /// </summary>
    public MessageViewModel[] Messages { get; }

    /// <summary>
    /// Command to send a message.
    /// </summary>
    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userMessage = InputText;
        InputText = string.Empty;

        // TODO: Send message to AI service and get response
        // For now, just simulate a response
        await Task.Delay(1000); // Simulate API call

        // Add user message to conversation
        // Note: In real implementation, this would update the Messages collection
    }

    /// <summary>
    /// Command to start voice input.
    /// </summary>
    [RelayCommand]
    private void StartVoiceInput()
    {
        // TODO: Implement voice input
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