using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Services;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class KnowledgeViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<KnowledgeViewModel>();

    [ObservableProperty]
    private ObservableCollection<KnowledgeEntryItem> _entries = new();

    [ObservableProperty]
    private KnowledgeEntryItem? _selectedEntry;

    [ObservableProperty]
    private string _newContent = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = KnowledgeCategories.UserNotes;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalEntries;

    [ObservableProperty]
    private bool _isRagEnabled = true;

    [ObservableProperty]
    private bool _isMbadEnabled = true;

    [ObservableProperty]
    private double _mbadAnomalyScore;

    [ObservableProperty]
    private bool _isAnomalyDetected;

    public ObservableCollection<string> Categories { get; } = new()
    {
        KnowledgeCategories.GameTips,
        KnowledgeCategories.CheatGuides,
        KnowledgeCategories.UserNotes,
        KnowledgeCategories.SystemDocs
    };

    public IAsyncRelayCommand LoadEntriesCommand { get; }
    public IAsyncRelayCommand AddEntryCommand { get; }
    public IAsyncRelayCommand<KnowledgeEntryItem> DeleteEntryCommand { get; }
    public IAsyncRelayCommand RebuildIndexCommand { get; }
    public IAsyncRelayCommand ImportFileCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }

    public KnowledgeViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        LoadEntriesCommand = new AsyncRelayCommand(LoadEntriesAsync);
        AddEntryCommand = new AsyncRelayCommand(AddEntryAsync);
        DeleteEntryCommand = new AsyncRelayCommand<KnowledgeEntryItem>(DeleteEntryAsync);
        RebuildIndexCommand = new AsyncRelayCommand(RebuildIndexAsync);
        ImportFileCommand = new AsyncRelayCommand(ImportFileAsync);
        SearchCommand = new AsyncRelayCommand(SearchAsync);

        _ = LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();
            
            if (knowledgeService == null)
            {
                StatusMessage = "Knowledge service not available";
                return;
            }

            var entries = await knowledgeService.GetAllAsync();
            Entries = new ObservableCollection<KnowledgeEntryItem>(
                entries.Select(e => new KnowledgeEntryItem(e)));
            TotalEntries = Entries.Count;

            StatusMessage = $"Loaded {TotalEntries} knowledge entries";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load knowledge entries");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewContent))
        {
            StatusMessage = "Please enter content to add";
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();
            
            if (knowledgeService == null)
            {
                StatusMessage = "Knowledge service not available";
                return;
            }

            var entry = await knowledgeService.AddKnowledgeAsync(NewContent, SelectedCategory);
            Entries.Insert(0, new KnowledgeEntryItem(entry));
            TotalEntries = Entries.Count;

            NewContent = string.Empty;
            StatusMessage = $"Added new {SelectedCategory} entry";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add knowledge entry");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteEntryAsync(KnowledgeEntryItem? entry)
    {
        if (entry == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();
            
            if (knowledgeService == null) return;

            await knowledgeService.DeleteAsync(entry.Id);
            Entries.Remove(entry);
            TotalEntries = Entries.Count;

            StatusMessage = "Entry deleted";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete knowledge entry");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task RebuildIndexAsync()
    {
        IsLoading = true;
        StatusMessage = "Rebuilding embeddings... This may take a while.";
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();
            
            if (knowledgeService == null) return;

            await knowledgeService.RebuildIndexAsync();
            StatusMessage = "Embeddings rebuilt successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to rebuild index");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ImportFileAsync()
    {
        try
        {
            // Get the main window for file picker
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                    ? desktop.MainWindow 
                    : null;

            if (topLevel == null)
            {
                StatusMessage = "Cannot open file picker: No main window available";
                return;
            }

            var storageProvider = topLevel.StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Knowledge File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Text Files") { Patterns = new[] { "*.txt", "*.md" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count == 0)
            {
                return;
            }

            var file = files[0];
            var filePath = file.Path.LocalPath;

            IsLoading = true;
            StatusMessage = $"Importing {file.Name}...";

            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();

            if (knowledgeService == null)
            {
                StatusMessage = "Knowledge service not available";
                return;
            }

            var count = await knowledgeService.ImportFromFileAsync(filePath, SelectedCategory);
            await LoadEntriesAsync();
            
            StatusMessage = $"Imported {count} entries from {file.Name}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to import file");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadEntriesAsync();
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeService>();
            
            if (knowledgeService == null) return;

            // Get semantic search results
            var context = await knowledgeService.GetRelevantContextAsync(SearchQuery, 5000);
            
            // For display, filter local entries
            var searchLower = SearchQuery.ToLower();
            var filtered = Entries.Where(e => 
                e.Content.ToLower().Contains(searchLower) ||
                e.Category.ToLower().Contains(searchLower)).ToList();

            StatusMessage = $"Found {filtered.Count} matching entries (RAG context: {context.Length} chars)";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Search failed");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void UpdateMbadStatus(bool isAnomalyDetected, double anomalyScore)
    {
        IsAnomalyDetected = isAnomalyDetected;
        MbadAnomalyScore = anomalyScore;
    }
}

/// <summary>
/// UI wrapper for KnowledgeEntry for better display
/// </summary>
public class KnowledgeEntryItem
{
    public Guid Id { get; }
    public string Content { get; }
    public string Category { get; }
    public string CategoryIcon => Category switch
    {
        KnowledgeCategories.GameTips => "💡",
        KnowledgeCategories.CheatGuides => "🎮",
        KnowledgeCategories.UserNotes => "📝",
        KnowledgeCategories.SystemDocs => "📚",
        _ => "📄"
    };
    public string Preview => Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;
    public DateTime CreatedAt { get; }
    public string CreatedAtDisplay => CreatedAt.ToString("MMM dd, yyyy HH:mm");

    public KnowledgeEntryItem(KnowledgeEntry entry)
    {
        Id = entry.Id;
        Content = entry.Content;
        Category = entry.Category;
        CreatedAt = entry.CreatedAt;
    }
}
