using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Mugen;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.UI.ViewModels;

using SaveState.Core.Services;

public partial class CharacterFusionViewModel : ViewModelBase
{
    private readonly CharacterFusionService _fusionService;
    private readonly MugenService _mugenService;

    [ObservableProperty]
    private ObservableCollection<MugenFighter> _availableFighters = new();

    [ObservableProperty]
    private ObservableCollection<FusionCharacter> _fusionGallery = new();

    [ObservableProperty]
    private MugenFighter? _selectedParent1;

    [ObservableProperty]
    private MugenFighter? _selectedParent2;

    [ObservableProperty]
    private string _selectedFusionType = "balanced";

    [ObservableProperty]
    private FusionCharacter? _previewFusion;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string[] FusionTypes { get; } = { "balanced", "dominant-1", "dominant-2", "chaos" };

    public IAsyncRelayCommand FuseCommand { get; }
    public IRelayCommand RefreshFightersCommand { get; }
    public IRelayCommand<string> DeleteFusionCommand { get; }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public CharacterFusionViewModel(CharacterFusionService fusionService, MugenService mugenService)
    {
        _fusionService = fusionService ?? throw new ArgumentNullException(nameof(fusionService));
        _mugenService = mugenService ?? throw new ArgumentNullException(nameof(mugenService));

        FuseCommand = new AsyncRelayCommand(PerformFusionAsync, CanFuse);
        RefreshFightersCommand = new RelayCommand(RefreshFighters);
        DeleteFusionCommand = new RelayCommand<string>(DeleteFusion);

        RefreshFighters();
        LoadFusionGallery();
    }

    private bool CanFuse() => SelectedParent1 != null && SelectedParent2 != null && SelectedParent1 != SelectedParent2;

    private async Task PerformFusionAsync()
    {
        if (SelectedParent1 == null || SelectedParent2 == null) return;

        try
        {
            var fusion = await _fusionService.FuseCharactersAsync(SelectedParent1, SelectedParent2, SelectedFusionType);
            PreviewFusion = fusion;
            FusionGallery.Add(fusion);
            StatusMessage = $"Created {fusion.Name} ({fusion.Rarity})!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fusion failed: {ex.Message}";
        }
    }

    private void RefreshFighters()
    {
        AvailableFighters.Clear();
        foreach (var fighter in _mugenService.GetFighters())
        {
            AvailableFighters.Add(fighter);
        }
        StatusMessage = $"Loaded {AvailableFighters.Count} fighters";
    }

    private void LoadFusionGallery()
    {
        FusionGallery.Clear();
        foreach (var fusion in _fusionService.GetAllFusions())
        {
            FusionGallery.Add(fusion);
        }
    }

    private void DeleteFusion(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _fusionService.DeleteFusion(id);
        var item = FusionGallery.FirstOrDefault(f => f.Id == id);
        if (item != null) FusionGallery.Remove(item);
        StatusMessage = "Fusion deleted";
    }

    partial void OnSelectedParent1Changed(MugenFighter? value) => FuseCommand.NotifyCanExecuteChanged();
    partial void OnSelectedParent2Changed(MugenFighter? value) => FuseCommand.NotifyCanExecuteChanged();
}
