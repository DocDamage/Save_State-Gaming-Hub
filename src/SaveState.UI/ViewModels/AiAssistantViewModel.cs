using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<AiAssistantViewModel>();

    [ObservableProperty]
    private string _userMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConfigured;

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    [ObservableProperty]
    private ObservableCollection<GameRecommendation> _recommendations = new();

    [ObservableProperty]
    private string _selectedTab = "Chat";

    public IAsyncRelayCommand SendMessageCommand { get; }
    public IAsyncRelayCommand GetRecommendationsCommand { get; }
    public IAsyncRelayCommand<string> GetGameTipsCommand { get; }
    public IRelayCommand ClearChatCommand { get; }

    public AiAssistantViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        GetRecommendationsCommand = new AsyncRelayCommand(GetRecommendationsAsync);
        GetGameTipsCommand = new AsyncRelayCommand<string>(GetGameTipsAsync);
        ClearChatCommand = new RelayCommand(ClearChat);

        // Check if AI is configured
        var aiService = _serviceProvider.GetService<IAiService>();
        IsConfigured = aiService?.IsConfigured ?? false;

        // Add welcome message
        Messages.Add(new ChatMessageViewModel
        {
            IsUser = false,
            Content = IsConfigured 
                ? "👋 Hi! I'm your gaming AI assistant. Ask me anything about games, get recommendations, or tips!"
                : "⚠️ AI not configured. Add your OpenAI API key in Settings to enable me!"
        });
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage)) return;

        var userMsg = UserMessage;
        UserMessage = string.Empty;

        // Add user message
        Messages.Add(new ChatMessageViewModel { IsUser = true, Content = userMsg });
        IsLoading = true;

        try
        {
            var aiService = _serviceProvider.GetRequiredService<IAiService>();
            
            // Build history from recent messages
            var history = Messages
                .Where(m => !m.Content.StartsWith("👋") && !m.Content.StartsWith("⚠️"))
                .TakeLast(10)
                .Select(m => new AiChatMessage 
                { 
                    Role = m.IsUser ? "user" : "assistant", 
                    Content = m.Content 
                });

            var response = await aiService.ChatAsync(userMsg, history);
            Messages.Add(new ChatMessageViewModel { IsUser = false, Content = response });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AI chat failed");
            Messages.Add(new ChatMessageViewModel 
            { 
                IsUser = false, 
                Content = $"❌ Error: {ex.Message}" 
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GetRecommendationsAsync()
    {
        IsLoading = true;
        Recommendations.Clear();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            var aiService = _serviceProvider.GetRequiredService<IAiService>();

            var games = await gameService.GetAllAsync();
            var recommendations = await aiService.GetRecommendationsAsync(games.ToList());

            foreach (var rec in recommendations)
            {
                Recommendations.Add(rec);
            }

            if (!Recommendations.Any())
            {
                Messages.Add(new ChatMessageViewModel
                {
                    IsUser = false,
                    Content = "I couldn't generate recommendations. Make sure you have some games in your library!"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get recommendations");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GetGameTipsAsync(string? gameTitle)
    {
        if (string.IsNullOrEmpty(gameTitle)) return;

        IsLoading = true;
        try
        {
            var aiService = _serviceProvider.GetRequiredService<IAiService>();
            var tips = await aiService.GetGameTipsAsync(gameTitle);
            Messages.Add(new ChatMessageViewModel
            {
                IsUser = false,
                Content = $"🎮 Tips for {gameTitle}:\n\n{tips}"
            });
            SelectedTab = "Chat";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get game tips");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearChat()
    {
        Messages.Clear();
        Messages.Add(new ChatMessageViewModel
        {
            IsUser = false,
            Content = "💬 Chat cleared. How can I help you?"
        });
    }
}

public class ChatMessageViewModel
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
