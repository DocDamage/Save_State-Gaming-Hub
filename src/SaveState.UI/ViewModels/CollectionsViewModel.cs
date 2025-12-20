using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class CollectionsViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<CollectionsViewModel>();

    [ObservableProperty]
    private ObservableCollection<Collection> _collections = new();

    [ObservableProperty]
    private Collection? _selectedCollection;

    [ObservableProperty]
    private ObservableCollection<Game> _collectionGames = new();

    [ObservableProperty]
    private ObservableCollection<Game> _allGames = new();

    [ObservableProperty]
    private string _newCollectionName = string.Empty;

    [ObservableProperty]
    private string _newCollectionDescription = string.Empty;

    [ObservableProperty]
    private Game? _selectedGameToAdd;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public IAsyncRelayCommand LoadCollectionsCommand { get; }
    public IAsyncRelayCommand CreateCollectionCommand { get; }
    public IAsyncRelayCommand<Collection> DeleteCollectionCommand { get; }
    public IAsyncRelayCommand AddGameToCollectionCommand { get; }
    public IAsyncRelayCommand<Game> RemoveGameFromCollectionCommand { get; }

    public CollectionsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        LoadCollectionsCommand = new AsyncRelayCommand(LoadCollectionsAsync);
        CreateCollectionCommand = new AsyncRelayCommand(CreateCollectionAsync);
        DeleteCollectionCommand = new AsyncRelayCommand<Collection>(DeleteCollectionAsync);
        AddGameToCollectionCommand = new AsyncRelayCommand(AddGameToCollectionAsync);
        RemoveGameFromCollectionCommand = new AsyncRelayCommand<Game>(RemoveGameFromCollectionAsync);

        _ = LoadCollectionsAsync();
    }

    partial void OnSelectedCollectionChanged(Collection? value)
    {
        _ = LoadCollectionGamesAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            var collections = await collectionService.GetAllAsync();
            Collections = new ObservableCollection<Collection>(collections);

            var games = await gameService.GetAllAsync();
            AllGames = new ObservableCollection<Game>(games);

            if (Collections.Any() && SelectedCollection == null)
            {
                SelectedCollection = Collections.First();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load collections");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCollectionGamesAsync()
    {
        if (SelectedCollection == null)
        {
            CollectionGames.Clear();
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            var games = await collectionService.GetGamesInCollectionAsync(SelectedCollection.Id);
            CollectionGames = new ObservableCollection<Game>(games);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load collection games");
        }
    }

    private async Task CreateCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName))
        {
            StatusMessage = "Please enter a collection name";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            
            var collection = await collectionService.CreateAsync(NewCollectionName, NewCollectionDescription);
            Collections.Add(collection);
            SelectedCollection = collection;
            
            NewCollectionName = string.Empty;
            NewCollectionDescription = string.Empty;
            StatusMessage = $"Created '{collection.Name}'";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create collection");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task DeleteCollectionAsync(Collection? collection)
    {
        if (collection == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            
            await collectionService.DeleteAsync(collection.Id);
            Collections.Remove(collection);
            
            if (SelectedCollection?.Id == collection.Id)
            {
                SelectedCollection = Collections.FirstOrDefault();
            }
            
            StatusMessage = $"Deleted '{collection.Name}'";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete collection");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task AddGameToCollectionAsync()
    {
        if (SelectedCollection == null || SelectedGameToAdd == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            
            await collectionService.AddGameToCollectionAsync(SelectedCollection.Id, SelectedGameToAdd.Id);
            
            if (!CollectionGames.Any(g => g.Id == SelectedGameToAdd.Id))
            {
                CollectionGames.Add(SelectedGameToAdd);
            }
            
            StatusMessage = $"Added '{SelectedGameToAdd.Title}' to '{SelectedCollection.Name}'";
            SelectedGameToAdd = null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add game to collection");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task RemoveGameFromCollectionAsync(Game? game)
    {
        if (SelectedCollection == null || game == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            
            await collectionService.RemoveGameFromCollectionAsync(SelectedCollection.Id, game.Id);
            CollectionGames.Remove(game);
            
            StatusMessage = $"Removed '{game.Title}' from '{SelectedCollection.Name}'";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove game from collection");
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
