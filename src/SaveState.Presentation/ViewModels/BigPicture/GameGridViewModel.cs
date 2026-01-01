using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

    public event Action<GameItemViewModel>? GameSelected;

    public GameGridViewModel()
    {
        LoadCollections();
        LoadGames();
        Games.CollectionChanged += OnGamesCollectionChanged;
    }

    private void LoadCollections()
    {
        AvailableCollections.Add(new CollectionViewModel { Name = "All Games", Id = "all" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Recently Played", Id = "recent" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Favorites", Id = "favorites" });
        AvailableCollections.Add(new CollectionViewModel { Name = "Action", Id = "action" });
        AvailableCollections.Add(new CollectionViewModel { Name = "RPG", Id = "rpg" });

        SelectedCollection = AvailableCollections.FirstOrDefault();
    }

    private void LoadGames()
    {
        // Sample games - in real implementation, this would come from the game library service
        var sampleGames = new[]
        {
            new GameItemViewModel { Title = "The Legend of Zelda", PlatformName = "NES", ReleaseYear = 1986, TotalPlaytime = TimeSpan.FromHours(25) },
            new GameItemViewModel { Title = "Super Mario Bros", PlatformName = "NES", ReleaseYear = 1985, TotalPlaytime = TimeSpan.FromHours(12) },
            new GameItemViewModel { Title = "Final Fantasy VII", PlatformName = "PS1", ReleaseYear = 1997, TotalPlaytime = TimeSpan.FromHours(45) },
            new GameItemViewModel { Title = "The Witcher 3", PlatformName = "PC", ReleaseYear = 2015, TotalPlaytime = TimeSpan.FromHours(120) },
            new GameItemViewModel { Title = "Cyberpunk 2077", PlatformName = "PC", ReleaseYear = 2020, TotalPlaytime = TimeSpan.FromHours(85) },
            new GameItemViewModel { Title = "Elden Ring", PlatformName = "PC", ReleaseYear = 2022, TotalPlaytime = TimeSpan.FromHours(95) },
            new GameItemViewModel { Title = "God of War", PlatformName = "PS4", ReleaseYear = 2018, TotalPlaytime = TimeSpan.FromHours(65) },
            new GameItemViewModel { Title = "Hades", PlatformName = "PC", ReleaseYear = 2020, TotalPlaytime = TimeSpan.FromHours(35) },
            new GameItemViewModel { Title = "Stardew Valley", PlatformName = "PC", ReleaseYear = 2016, TotalPlaytime = TimeSpan.FromHours(200) },
            new GameItemViewModel { Title = "Among Us", PlatformName = "PC", ReleaseYear = 2018, TotalPlaytime = TimeSpan.FromHours(15) }
        };

        foreach (var game in sampleGames)
        {
            Games.Add(game);
        }

        // Select first game by default
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

        var newRow = Math.Clamp(currentRow + rowDelta, 0, (Games.Count - 1) / GamesPerRow);
        var newCol = Math.Clamp(currentCol + colDelta, 0, Math.Min(GamesPerRow - 1, (Games.Count - 1) % GamesPerRow + (newRow == (Games.Count - 1) / GamesPerRow ? 0 : GamesPerRow)));

        var newIndex = newRow * GamesPerRow + newCol;
        if (newIndex >= 0 && newIndex < Games.Count)
        {
            _selectedIndex = newIndex;
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        // Clear previous selection
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
            // In real implementation, this would launch the game
            // For now, just show a message
            SelectedGame.LastPlayed = DateTime.Now;
        }
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        // Toggle filter visibility - implementation would show/hide filter panel
    }

    public void Dispose()
    {
        Games.CollectionChanged -= OnGamesCollectionChanged;
    }
}

public class CollectionViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
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
    private bool isSelected;

    [ObservableProperty]
    private bool isFocused;

    public Avalonia.Media.IImage? CoverImage => null; // Would load actual image

    public Avalonia.Media.IBrush BackgroundBrush => IsSelected
        ? Avalonia.Media.Brushes.DarkBlue
        : Avalonia.Media.Brushes.DarkGray;

    public Avalonia.Media.IBrush BorderBrush => IsFocused
        ? Avalonia.Media.Brushes.Cyan
        : Avalonia.Media.Brushes.Gray;

    public Avalonia.Thickness BorderThickness => IsFocused
        ? new Avalonia.Thickness(3)
        : new Avalonia.Thickness(1);

    public double SelectionOpacity => IsSelected ? 0.3 : 0.0;
}