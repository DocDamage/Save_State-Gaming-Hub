using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class DreamSequenceViewModel : ViewModelBase
{
    private readonly DreamSequenceService _dreamService;

    [ObservableProperty]
    private ObservableCollection<DreamLevel> _generatedLevels = new();

    [ObservableProperty]
    private DreamLevel? _selectedLevel;

    [ObservableProperty]
    private DreamMood _selectedMood = DreamMood.Surreal;

    [ObservableProperty]
    private int? _customSeed;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public DreamMood[] AvailableMoods { get; } = Enum.GetValues<DreamMood>();

    public IRelayCommand GenerateLevelCommand { get; }
    public IRelayCommand<string> DeleteLevelCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public DreamSequenceViewModel()
    {
        _dreamService = new DreamSequenceService();

        GenerateLevelCommand = new RelayCommand(GenerateLevel);
        DeleteLevelCommand = new RelayCommand<string>(DeleteLevel);
        RefreshCommand = new RelayCommand(RefreshLevels);

        RefreshLevels();
    }

    private void GenerateLevel()
    {
        var level = _dreamService.GenerateLevel(SelectedMood, CustomSeed);
        GeneratedLevels.Insert(0, level);
        SelectedLevel = level;
        CustomSeed = null;
        StatusMessage = $"Generated: {level.Name} ({level.Elements.Count} elements)";
    }

    private void DeleteLevel(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _dreamService.DeleteLevel(id);
        var level = GeneratedLevels.FirstOrDefault(l => l.Id == id);
        if (level != null) GeneratedLevels.Remove(level);
        StatusMessage = "Level deleted";
    }

    private void RefreshLevels()
    {
        GeneratedLevels.Clear();
        foreach (var level in _dreamService.GetGeneratedLevels().OrderByDescending(l => l.Generated))
        {
            GeneratedLevels.Add(level);
        }
        StatusMessage = $"Loaded {GeneratedLevels.Count} dream levels";
    }
}
