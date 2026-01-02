using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Collections.ObjectModel;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Common.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Terminal tab.
/// </summary>
public partial class TerminalViewModel : ObservableObject
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly string _chatSessionId = Guid.NewGuid().ToString();

    public TerminalViewModel(
        IAiOrchestrator aiOrchestrator,
        IUserPreferencesService preferencesService,
        IKnowledgeBaseService knowledgeBaseService)
    {
        _aiOrchestrator = aiOrchestrator;
        _preferencesService = preferencesService;
        _knowledgeBaseService = knowledgeBaseService;

        History = new ObservableCollection<string>
        {
            "SaveState OS v2.0.0 [Build 2026.01.02]",
            "Type 'help' for a list of available commands.",
            "AI Assistant: ONLINE",
            ""
        };
    }

    /// <summary>
    /// Gets the display title for the terminal tab.
    /// </summary>
    public string Title => "Terminal";

    public ObservableCollection<string> History { get; }

    [ObservableProperty]
    private string _commandText = string.Empty;

    [RelayCommand]
    private void Execute()
    {
        if (string.IsNullOrWhiteSpace(CommandText)) return;

        var cmd = CommandText.Trim();
        History.Add($"[root@savestate]# {cmd}");
        ProcessCommand(cmd.ToLower());
        CommandText = string.Empty;
    }

    private async void ProcessCommand(string cmd)
    {
        switch (cmd)
        {
            case "help":
                History.Add("Available commands:");
                History.Add("  scan     - Scan for new games");
                History.Add("  stats    - Show quick library stats");
                History.Add("  clear    - Clear terminal history");
                History.Add("  exit     - Close application (simulated)");
                History.Add("  reset    - Reset AI conversation context");
                History.Add("  sync     - Sync Markdown knowledge base");
                History.Add("  kb       - Show knowledge base path & instructions");
                History.Add("");
                History.Add("Any other text will be handled by the AI Assistant.");
                break;
            case "scan":
                History.Add("Scanning directories...");
                History.Add("Found 12 new titles. Import pending.");
                break;
            case "stats":
                History.Add("Library Stats:");
                History.Add("  Total Games: 142");
                History.Add("  Hours Played: 3,450");
                History.Add("  Completed: 24");
                break;
            case "clear":
                History.Clear();
                break;
            case "reset":
                await _aiOrchestrator.ClearConversationAsync(_chatSessionId);
                History.Add("AI conversation context has been reset.");
                break;
            case "sync":
                History.Add("Starting knowledge base synchronization...");
                try
                {
                    var count = await _knowledgeBaseService.SyncKnowledgeBaseAsync();
                    History.Add($"Successfully indexed {count} knowledge chunks from MD files.");
                }
                catch (Exception ex)
                {
                    History.Add($"Sync failed: {ex.Message}");
                }
                break;
            case "kb":
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var kbPath = Path.Combine(appData, "SaveStateReborn", "KnowledgeBase");
                History.Add("Knowledge Base Instructions:");
                History.Add($"  Path: {kbPath}");
                History.Add("  1. Place .md files with gaming info in this folder.");
                History.Add("  2. Type 'sync' to index them into the AI store.");
                History.Add("  3. Ask the AI questions about your custom files!");
                History.Add("");
                History.Add("The AI uses RAG (Retrieval-Augmented Generation) to pull");
                History.Add("context from these files instantly during chat.");
                break;
            default:
                await ProcessAiChatAsync(cmd);
                break;
        }
        History.Add("");
    }

    private async Task ProcessAiChatAsync(string prompt)
    {
        History.Add("AI: thinking...");
        var thinkingIndex = History.Count - 1;

        try
        {
            var provider = await _preferencesService.GetPreferredAiProviderAsync();
            var model = await _preferencesService.GetPreferredAiModelAsync();

            var request = new AiRequest(
                Type: AiRequestType.Chat,
                Prompt: prompt,
                Model: model,
                PreferredProvider: provider,
                MaxTokens: 1000,
                Temperature: 0.7f);

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(_chatSessionId, request);

            if (response.IsSuccessful)
            {
                History[thinkingIndex] = $"AI: {response.Content}";
            }
            else
            {
                History[thinkingIndex] = $"AI: Error - {response.Error}";
            }
        }
        catch (Exception ex)
        {
            History[thinkingIndex] = $"AI: Fatal error - {ex.Message}";
        }
    }
}
