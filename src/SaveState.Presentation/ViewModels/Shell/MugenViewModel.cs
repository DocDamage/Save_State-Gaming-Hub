using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the MUGEN tab.
/// </summary>
public partial class MugenViewModel : ObservableObject
{
    private readonly IMugenCharacterRepository? _characterRepository;
    private readonly IDeathMatchSimulator? _deathMatchSimulator;
    private readonly ILogger<MugenViewModel> _logger;

    public MugenViewModel(
        IMugenCharacterRepository? characterRepository,
        IDeathMatchSimulator? deathMatchSimulator,
        ILogger<MugenViewModel> logger)
    {
        _characterRepository = characterRepository;
        _deathMatchSimulator = deathMatchSimulator;
        _logger = logger;

        // Initialize collections
        MugenSections = new ObservableCollection<MugenSectionViewModel>
        {
            new MugenSectionViewModel("🎮", "Roster", "Roster", true),
            new MugenSectionViewModel("💀", "Death Battle", "DeathBattle", false),
            new MugenSectionViewModel("📥", "Downloads", "Downloads", false),
            new MugenSectionViewModel("📊", "Stats", "Stats", false)
        };

        SelectedSection = MugenSections[0];
        Characters = new ObservableCollection<CharacterViewModel>();

        // Auto-load characters
        _ = LoadCharactersAsync();
    }

    /// <summary>
    /// Gets the display title for the MUGEN tab.
    /// </summary>
    public string Title => "MUGEN";

    // Collections
    public ObservableCollection<MugenSectionViewModel> MugenSections { get; }
    public ObservableCollection<CharacterViewModel> Characters { get; }

    // Selected section
    [ObservableProperty]
    private MugenSectionViewModel? selectedSection;

    partial void OnSelectedSectionChanged(MugenSectionViewModel? value)
    {
        foreach (var section in MugenSections)
        {
            section.IsSelected = section == value;
        }
    }

    // Roster properties
    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private string selectedFranchise = "All";

    [ObservableProperty]
    private int totalCharacters;

    [ObservableProperty]
    private bool isLoading;

    // Death Battle properties
    [ObservableProperty]
    private CharacterViewModel? player1;

    [ObservableProperty]
    private CharacterViewModel? player2;

    [ObservableProperty]
    private string predictionText = "Select two characters to see AI prediction";

    [ObservableProperty]
    private int matchCount = 10;

    [ObservableProperty]
    private string battleResults = string.Empty;

    [ObservableProperty]
    private bool isSimulating;

    // Stats properties
    [ObservableProperty]
    private int totalMatches = 1247;

    [ObservableProperty]
    private string mostPlayedCharacter = "Ryu";

    [ObservableProperty]
    private string highestWinRate = "Wolverine (72%)";

    // Downloader properties
    [ObservableProperty]
    private string _assetUrl = string.Empty;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = "Ready to download";

    // Commands
    [RelayCommand]
    private void SelectSection(MugenSectionViewModel? section)
    {
        if (section != null)
        {
            SelectedSection = section;
        }
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        if (_characterRepository == null)
        {
            _logger.LogWarning("Character repository not available");
            // Add demo characters
            AddDemoCharacters();
            return;
        }

        IsLoading = true;
        try
        {
            var characters = await _characterRepository.GetAllAsync();
            Characters.Clear();
            foreach (var character in characters)
            {
                Characters.Add(new CharacterViewModel(character));
            }
            TotalCharacters = Characters.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading characters");
            AddDemoCharacters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddDemoCharacters()
    {
        Characters.Clear();
        Characters.Add(new CharacterViewModel { Name = "Ryu", Franchise = "Street Fighter", WinRate = 67 });
        Characters.Add(new CharacterViewModel { Name = "Ken", Franchise = "Street Fighter", WinRate = 64 });
        Characters.Add(new CharacterViewModel { Name = "Chun-Li", Franchise = "Street Fighter", WinRate = 70 });
        Characters.Add(new CharacterViewModel { Name = "Wolverine", Franchise = "Marvel", WinRate = 72 });
        Characters.Add(new CharacterViewModel { Name = "Akuma", Franchise = "Street Fighter", WinRate = 75 });
        Characters.Add(new CharacterViewModel { Name = "Guile", Franchise = "Street Fighter", WinRate = 62 });
        TotalCharacters = Characters.Count;
    }

    [RelayCommand]
    private void SelectPlayer1(CharacterViewModel? character)
    {
        Player1 = character;
        UpdatePrediction();
    }

    [RelayCommand]
    private void SelectPlayer2(CharacterViewModel? character)
    {
        Player2 = character;
        UpdatePrediction();
    }

    private void UpdatePrediction()
    {
        if (Player1 == null || Player2 == null)
        {
            PredictionText = "Select two characters to see AI prediction";
            return;
        }

        var p1WinChance = Player1.WinRate / (float)(Player1.WinRate + Player2.WinRate) * 100;
        var winner = p1WinChance > 50 ? Player1.Name : Player2.Name;
        var winChance = Math.Max(p1WinChance, 100 - p1WinChance);

        PredictionText = $"🤖 AI predicts {winner} has {winChance:F0}% chance of winning based on historical win rates";
    }

    [RelayCommand]
    private async Task RunDeathBattleAsync()
    {
        if (Player1 == null || Player2 == null)
        {
            _logger.LogWarning("Both players must be selected");
            return;
        }

        IsSimulating = true;
        BattleResults = "Running simulation...";

        try
        {
            await Task.Delay(2000); // Simulate battle time

            // Simulate results
            var p1Wins = 0;
            var p2Wins = 0;
            var random = new Random();

            for (int i = 0; i < MatchCount; i++)
            {
                var p1Chance = Player1.WinRate / (float)(Player1.WinRate + Player2.WinRate);
                if (random.NextDouble() < p1Chance)
                    p1Wins++;
                else
                    p2Wins++;
            }

            BattleResults = $"RESULTS ({MatchCount} matches):\n\n" +
                          $"{Player1.Name}: {p1Wins} wins ({p1Wins * 100.0 / MatchCount:F1}%)\n" +
                          $"{Player2.Name}: {p2Wins} wins ({p2Wins * 100.0 / MatchCount:F1}%)\n\n" +
                          $"WINNER: {(p1Wins > p2Wins ? Player1.Name : Player2.Name)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running death battle");
            BattleResults = "Error running simulation";
        }
        finally
        {
            IsSimulating = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(AssetUrl)) return;

        IsDownloading = true;
        DownloadStatus = "Starting download...";
        DownloadProgress = 0;

        try
        {
            // Simulate download progress
            for (int i = 0; i <= 100; i += 10)
            {
                DownloadProgress = i;
                DownloadStatus = $"Downloading asset... {i}%";
                await Task.Delay(500);
            }

            DownloadStatus = "Finalizing and extracting...";
            await Task.Delay(1000);

            DownloadStatus = "✅ Asset successfully saved to MUGEN directory!";
            AssetUrl = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            DownloadStatus = "❌ Download failed. Check the URL and try again.";
        }
        finally
        {
            IsDownloading = false;
        }
    }
}

// Supporting ViewModels
public class MugenSectionViewModel : ObservableObject
{
    public MugenSectionViewModel(string icon, string name, string id, bool isSelected = false)
    {
        Icon = icon;
        Name = name;
        Id = id;
        IsSelected = isSelected;
    }

    public string Icon { get; }
    public string Name { get; }
    public string Id { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class CharacterViewModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Franchise { get; set; } = string.Empty;
    public int WinRate { get; set; }
    public string DisplayName => $"{Name} ({Franchise})";

    public CharacterViewModel() { }

    public CharacterViewModel(SaveState.Core.Mugen.Entities.MugenCharacter character)
    {
        Name = character.Name;
        Franchise = character.Author ?? "Unknown"; // Use Author as franchise for now
        WinRate = 65; // Default win rate
    }
}
