using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class GameGridViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private ObservableCollection<GameItemViewModel> games = new();

    [ObservableProperty]
    private ObservableCollection<CollectionViewModel> availableCollections = new();

    [ObservableProperty]
    private CollectionViewModel? selectedCollection;

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private GameItemViewModel? selectedGame;

    private int _selectedIndex = -1;
    private const int GamesPerRow = 5;
    private readonly ITimeProvider _timeProvider;

    public event Action<GameItemViewModel>? GameSelected;

    public GameGridViewModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        LoadCollections();
        LoadGames();
        Games.CollectionChanged += OnGamesCollectionChanged;
    }

    private void LoadCollections()
    {
        AvailableCollections.Clear();
        AvailableCollections.Add(new CollectionViewModel { Name = "All Games", Id = "all" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Recently Played", Id = "recent" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Favorites", Id = "favorites" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Action", Id = "action" });
        AvailableCollections.Add(new CollectionViewModel { Name = "RPG", Id = "rpg" });

        SelectedCollection = AvailableCollections.FirstOrDefault();
    }

    private void LoadGames()
    {
        Games.Clear();
        var sampleGames = new[]
        {
            new GameItemViewModel { Title = "Hades II", PlatformName = "PC", ReleaseYear = 2024, TotalPlaytime = TimeSpan.FromHours(45), Rating = 4.9, Description = "Battle beyond the Underworld using dark sorcery to take on the Titan of Time in this god-like rogue-like dungeon crawler." },
            new GameItemViewModel { Title = "Elden Ring", PlatformName = "PC", ReleaseYear = 2022, TotalPlaytime = TimeSpan.FromHours(250), Rating = 5.0, Description = "Rise, Tarnished, and be led by grace to brandish the power of the Elden Ring and become an Elden Lord in the Lands Between." },
            new GameItemViewModel { Title = "Cyberpunk 2077", PlatformName = "PC", ReleaseYear = 2020, TotalPlaytime = TimeSpan.FromHours(120), Rating = 4.5, Description = "An open-world, action-adventure story set in Night City, a megalopolis obsessed with power, glamour and body modification." },
            new GameItemViewModel { Title = "Baldur's Gate 3", PlatformName = "PC", ReleaseYear = 2023, TotalPlaytime = TimeSpan.FromHours(180), Rating = 5.0, Description = "Gather your party and return to the Forgotten Realms in a tale of fellowship and betrayal, sacrifice and survival, and the lure of absolute power." },
            new GameItemViewModel { Title = "Final Fantasy VII Rebirth", PlatformName = "PS5", ReleaseYear = 2024, TotalPlaytime = TimeSpan.FromHours(85), Rating = 4.8, Description = "Cloud and his comrades escape the city of Midgar and set out on a journey across the planet." },
            new GameItemViewModel { Title = "Super Mario Odyssey", PlatformName = "Switch", ReleaseYear = 2017, TotalPlaytime = TimeSpan.FromHours(30), Rating = 4.9, Description = "Join Mario on a massive, globe-trotting 3D adventure." },
            new GameItemViewModel { Title = "God of War Ragnarök", PlatformName = "PS5", ReleaseYear = 2022, TotalPlaytime = TimeSpan.FromHours(60), Rating = 4.9, Description = "Kratos and Atreus must journey to each of the Nine Realms in search of answers as Asgardian forces prepare for a prophesied battle that will end the world." },
            new GameItemViewModel { Title = "Persona 3 Reload", PlatformName = "PC", ReleaseYear = 2024, TotalPlaytime = TimeSpan.FromHours(100), Rating = 4.7, Description = "Step into the shoes of a transfer student thrust into an unexpected fate when entering the hour 'hidden' between one day and the next." },
            new GameItemViewModel { Title = "Stardew Valley", PlatformName = "PC", ReleaseYear = 2016, TotalPlaytime = TimeSpan.FromHours(500), Rating = 4.9, Description = "You've inherited your grandfather's old farm plot in Stardew Valley. Armed with hand-me-down tools and a few coins, you set out to begin your new life." },
            new GameItemViewModel { Title = "Alan Wake 2", PlatformName = "PC", ReleaseYear = 2023, TotalPlaytime = TimeSpan.FromHours(25), Rating = 4.8, Description = "A string of ritualistic murders threatens Bright Falls, an idyllic small-town community surrounded by the Pacific Northwest wilderness." }
        };

        foreach (var game in sampleGames)
        {
            Games.Add(game);
        }

        if (Games.Any())
        {
            _selectedIndex = 0;
            UpdateSelection();
        }
    }

    private void OnGamesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSelection();
    }

    public void MoveSelection(int rowDelta, int colDelta)
    {
        if (!Games.Any()) return;

        var currentRow = _selectedIndex / GamesPerRow;
        var currentCol = _selectedIndex % GamesPerRow;

        var newRow = Math.Max(0, Math.Min(currentRow + rowDelta, (Games.Count - 1) / GamesPerRow));
        var newCol = Math.Max(0, Math.Min(currentCol + colDelta, GamesPerRow - 1));

        var newIndex = newRow * GamesPerRow + newCol;
        if (newIndex >= Games.Count)
        {
            newIndex = Games.Count - 1;
        }

        if (newIndex >= 0 && newIndex < Games.Count)
        {
            _selectedIndex = newIndex;
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        foreach (var game in Games)
        {
            game.IsSelected = false;
            game.IsFocused = false;
        }

        if (_selectedIndex >= 0 && _selectedIndex < Games.Count)
        {
            var selectedGameItem = Games[_selectedIndex];
            selectedGameItem.IsSelected = true;
            selectedGameItem.IsFocused = true;
            SelectedGame = selectedGameItem;
            GameSelected?.Invoke(selectedGameItem);
        }
    }

    public void LaunchSelectedGame()
    {
        if (SelectedGame != null)
        {
            SelectedGame.LastPlayed = _timeProvider.Now;
        }
    }

    [RelayCommand]
    private void ToggleFilter()
    {
    }

    public void Dispose()
    {
        Games.CollectionChanged -= OnGamesCollectionChanged;
    }
}

public partial class GameItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string platformName = "";

    [ObservableProperty]
    private int releaseYear;

    [ObservableProperty]
    private TimeSpan totalPlaytime;

    [ObservableProperty]
    private DateTime? lastPlayed;

    [ObservableProperty]
    private string coverImageUrl = "";

    [ObservableProperty]
    private double rating;

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isFocused;

    public double SelectionOpacity => IsSelected ? 0.3 : 0.0;
}
