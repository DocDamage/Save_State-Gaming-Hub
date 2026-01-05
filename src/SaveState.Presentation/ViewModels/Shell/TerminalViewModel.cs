using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services.Terminal;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Terminal tab, supporting CLI commands, AI chat, and history.
/// </summary>
public partial class TerminalViewModel : ObservableObject
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ICommandExecutor _commandExecutor;
    private readonly string _chatSessionId = Guid.NewGuid().ToString();

    private List<string> _commandHistory = new();
    private int _historyIndex = -1;

    [ObservableProperty]
    private bool _isScriptEditorVisible;

    [ObservableProperty]
    private ObservableCollection<TerminalScriptViewModel> _scripts = new();

    [ObservableProperty]
    private TerminalScriptViewModel? _selectedScript;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private double _syncProgress;

    [ObservableProperty]
    private string _syncStatus = string.Empty;

    public TerminalViewModel(
        IAiOrchestrator aiOrchestrator,
        IUserPreferencesService preferencesService,
        IKnowledgeBaseService knowledgeBaseService,
        ICommandExecutor commandExecutor)
    {
        _aiOrchestrator = aiOrchestrator;
        _preferencesService = preferencesService;
        _knowledgeBaseService = knowledgeBaseService;
        _commandExecutor = commandExecutor;

        History = new ObservableCollection<string>
        {
            "SaveState OS v2.1.0 [Build 2026.01.03]",
            "Type 'help' for a list of available commands.",
            "AI Assistant: ONLINE",
            "CLI Integration: ACTIVE",
            ""
        };

        LoadScripts();
    }

    private void LoadScripts()
    {
        // Mock some scripts for now, in a real app we'd load from disk
        Scripts.Add(new TerminalScriptViewModel("Optimize System", "perf optimize --aggressive\ngame cleanup\nstats", "optimize.ss"));
        Scripts.Add(new TerminalScriptViewModel("Backup All", "savestate backup --all\ncloud sync\nstats", "backup.ss"));
        Scripts.Add(new TerminalScriptViewModel("Ghost in the Shell", "mugen search 'Ghost'\nmugen stats", "ghost.ss"));
    }

    [RelayCommand]
    private void ToggleScriptEditor() => IsScriptEditorVisible = !IsScriptEditorVisible;

    [RelayCommand]
    private async Task RunSelectedScriptAsync()
    {
        if (SelectedScript == null) return;

        History.Add($"[SYSTEM] Executing script: {SelectedScript.Name}...");
        var lines = SelectedScript.Content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue;

            History.Add($"[root@savestate]# {trimmedLine}");
            var output = await _commandExecutor.ExecuteAsync(trimmedLine);
            if (!string.IsNullOrEmpty(output))
            {
                foreach (var outLine in output.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(outLine)) History.Add(outLine);
                }
            }
        }
        History.Add("[SYSTEM] Script execution complete.");
        History.Add("");
    }

    /// <summary>
    /// Gets the display title for the terminal tab.
    /// </summary>
    public string Title => "Terminal";

    public ObservableCollection<string> History { get; }

    [ObservableProperty]
    private string _commandText = string.Empty;

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandText)) return;

        var cmd = CommandText.Trim();
        History.Add($"[root@savestate]# {cmd}");

        // Add to history list for navigation
        _commandHistory.Add(cmd);
        _historyIndex = -1;

        // Try executing as a CLI command first
        var output = await _commandExecutor.ExecuteAsync(cmd);

        if (!string.IsNullOrEmpty(output))
        {
            foreach (var line in output.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    History.Add(line);
                }
            }
        }
        else
        {
            // If no CLI output, check for internal commands or fallback to AI
            await ProcessInternalOrAiCommandAsync(cmd);
        }

        CommandText = string.Empty;
        History.Add("");
    }

    [RelayCommand]
    private void HistoryUp()
    {
        if (_commandHistory.Count == 0) return;

        if (_historyIndex == -1)
        {
            _historyIndex = _commandHistory.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        CommandText = _commandHistory[_historyIndex];
    }

    [RelayCommand]
    private void HistoryDown()
    {
        if (_commandHistory.Count == 0 || _historyIndex == -1) return;

        if (_historyIndex < _commandHistory.Count - 1)
        {
            _historyIndex++;
            CommandText = _commandHistory[_historyIndex];
        }
        else
        {
            _historyIndex = -1;
            CommandText = string.Empty;
        }
    }

    [RelayCommand]
    private void TabComplete()
    {
        if (string.IsNullOrEmpty(CommandText)) return;

        var completions = _commandExecutor.GetCompletions(CommandText).ToList();
        if (completions.Count == 1)
        {
            CommandText = completions[0];
        }
        else if (completions.Count > 1)
        {
            // Show available completions in history
            History.Add($"Completions: {string.Join(", ", completions)}");
        }
    }

    private async Task ProcessInternalOrAiCommandAsync(string cmd)
    {
        var lowerCmd = cmd.ToLower();
        try
        {
            switch (lowerCmd)
            {
                case "help":
                    History.Add("Available UI Commands:");
                    History.Add("  clear    - Clear terminal history");
                    History.Add("  reset    - Reset AI conversation context");
                    History.Add("  sync     - Sync Markdown knowledge base");
                    History.Add("  kb       - Show knowledge base path");
                    History.Add("");
                    History.Add("CLI Commands (Type <command> --help for details):");
                    History.Add("  game     - Manage game library");
                    History.Add("  savestate- Manage save states");
                    History.Add("  perf     - Performance monitoring");
                    History.Add("  mugen    - MUGEN character management");
                    History.Add("");
                    History.Add("Any other text will be handled by the AI Assistant.");
                    break;
                case "clear":
                    History.Clear();
                    break;
                case "reset":
                    await _aiOrchestrator.ClearConversationAsync(_chatSessionId);
                    History.Add("AI conversation context has been reset.");
                    break;
                case "sync":
                    await SyncKnowledgeBaseWithProgressAsync();
                    break;
                default:
                    // Only fallback to AI if it doesn't look like a failed CLI command
                    // (e.g. not starting with a known cli prefix if we have them)
                    await ProcessAiChatAsync(cmd);
                    break;
            }
        }
        catch (Exception ex)
        {
            History.Add($"Error: {ex.Message}");
        }
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

    private async Task SyncKnowledgeBaseWithProgressAsync()
    {
        try
        {
            IsSyncing = true;
            SyncProgress = 0;
            SyncStatus = "Starting knowledge base synchronization...";

            History.Add("Starting knowledge base synchronization...");

            // Since the current IKnowledgeBaseService doesn't support progress callbacks,
            // we'll show a simple progress simulation
            SyncProgress = 10;
            SyncStatus = "Scanning knowledge base directory...";

            await Task.Delay(500); // Simulate initial scanning

            SyncProgress = 30;
            SyncStatus = "Processing markdown files...";

            await Task.Delay(500); // Simulate processing

            SyncProgress = 60;
            SyncStatus = "Indexing content chunks...";

            var count = await _knowledgeBaseService.SyncKnowledgeBaseAsync();

            SyncProgress = 90;
            SyncStatus = "Finalizing index...";

            await Task.Delay(300); // Simulate finalization

            SyncProgress = 100;
            SyncStatus = "Complete!";

            History.Add($"Successfully indexed {count} knowledge chunks.");

            await Task.Delay(1000); // Show completion briefly
        }
        catch (Exception ex)
        {
            History.Add($"Error during sync: {ex.Message}");
            SyncStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
            SyncProgress = 0;
            SyncStatus = string.Empty;
        }
    }
}
