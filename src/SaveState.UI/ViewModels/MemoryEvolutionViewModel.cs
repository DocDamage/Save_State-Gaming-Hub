using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class MemoryEvolutionViewModel : ViewModelBase
{
    private readonly MemoryEvolutionService _evolutionService;

    [ObservableProperty]
    private string _selectedGameId = "demo-game";

    [ObservableProperty]
    private PlaystyleProfile? _currentProfile;

    [ObservableProperty]
    private ObservableCollection<MutationItem> _activeMutations = new();

    [ObservableProperty]
    private ObservableCollection<DeathLocation> _deathHeatmap = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public IRelayCommand LoadProfileCommand { get; }
    public IRelayCommand SimulateDeathCommand { get; }
    public IRelayCommand SimulatePlayTimeCommand { get; }
    public IRelayCommand<string> RemoveMutationCommand { get; }
    public IRelayCommand ClearMutationsCommand { get; }

    public MemoryEvolutionViewModel()
    {
        _evolutionService = new MemoryEvolutionService();

        LoadProfileCommand = new RelayCommand(LoadProfile);
        SimulateDeathCommand = new RelayCommand(SimulateDeath);
        SimulatePlayTimeCommand = new RelayCommand(SimulatePlayTime);
        RemoveMutationCommand = new RelayCommand<string>(RemoveMutation);
        ClearMutationsCommand = new RelayCommand(ClearMutations);

        LoadProfile();
    }

    private void LoadProfile()
    {
        CurrentProfile = _evolutionService.GetOrCreateProfile(SelectedGameId);
        RefreshMutations();
        RefreshHeatmap();
        StatusMessage = $"Loaded profile for {SelectedGameId}";
    }

    private void SimulateDeath()
    {
        var locations = new[] { "Level 1 Pit", "Boss Room", "Spike Corridor", "Lava Section", "Final Jump" };
        var location = locations[new Random().Next(locations.Length)];
        
        _evolutionService.RecordDeath(SelectedGameId, location);
        CurrentProfile = _evolutionService.GetOrCreateProfile(SelectedGameId);
        RefreshMutations();
        RefreshHeatmap();
        StatusMessage = $"Death recorded at {location}";
    }

    private void SimulatePlayTime()
    {
        _evolutionService.RecordPlayTime(SelectedGameId, 300); // 5 minutes
        CurrentProfile = _evolutionService.GetOrCreateProfile(SelectedGameId);
        StatusMessage = "Added 5 minutes of play time";
    }

    private void RemoveMutation(string? mutation)
    {
        if (string.IsNullOrEmpty(mutation)) return;
        _evolutionService.RemoveMutation(SelectedGameId, mutation);
        RefreshMutations();
        StatusMessage = $"Removed mutation: {mutation}";
    }

    private void ClearMutations()
    {
        _evolutionService.ClearMutations(SelectedGameId);
        RefreshMutations();
        StatusMessage = "All mutations cleared";
    }

    private void RefreshMutations()
    {
        ActiveMutations.Clear();
        foreach (var mutation in _evolutionService.GetActiveMutations(SelectedGameId))
        {
            ActiveMutations.Add(new MutationItem { Name = mutation, Description = GetMutationDescription(mutation) });
        }
    }

    private void RefreshHeatmap()
    {
        DeathHeatmap.Clear();
        foreach (var kvp in _evolutionService.GetDeathHeatmap(SelectedGameId))
        {
            DeathHeatmap.Add(new DeathLocation { Location = kvp.Key, Deaths = kvp.Value });
        }
    }

    private string GetMutationDescription(string mutation) => mutation switch
    {
        "SpeedBoost" => "Game runs 10% faster - for skilled players",
        "ExtraLife" => "Start with an extra hit point",
        "CheckpointSave" => "Auto-save at checkpoints",
        "EnemyNerf" => "Enemies deal 50% less damage",
        "HintSystem" => "Shows hints for difficult sections",
        _ => "Unknown mutation"
    };
}

public class MutationItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DeathLocation
{
    public string Location { get; set; } = string.Empty;
    public int Deaths { get; set; }
}
