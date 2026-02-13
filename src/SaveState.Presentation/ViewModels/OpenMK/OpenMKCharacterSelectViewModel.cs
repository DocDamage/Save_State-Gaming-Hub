using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.OpenMK;

/// <summary>
/// View model for OpenMK character selection.
/// </summary>
public partial class OpenMKCharacterSelectViewModel : ObservableObject
{
    private readonly IOpenMKService _openMKService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<OpenMKCharacter> _characters = new();

    [ObservableProperty]
    private OpenMKCharacter? _selectedCharacter;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Select a character";

    public OpenMKCharacterSelectViewModel(
        IOpenMKService openMKService,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _openMKService = openMKService;
        _dialogService = dialogService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading OpenMK characters...";

        try
        {
            var result = await _openMKService.GetCharactersAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Characters.Clear();
                foreach (var character in result.Value)
                {
                    Characters.Add(character);
                }
                StatusMessage = $"Loaded {Characters.Count} characters";
            }
            else
            {
                StatusMessage = $"Failed to load characters: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Failed to load characters", "OpenMK Error");
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = "Error loading characters";
            _notificationService.ShowError($"Error loading characters: {ex.Message}", "OpenMK Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectCharacterAsync(OpenMKCharacter? character)
    {
        if (character == null) return;

        SelectedCharacter = character;
        StatusMessage = $"Selected: {character.DisplayName}";

        // Show character details
        var details = $"Realm: {character.Realm}\n" +
                     $"Fighting Style: {character.FightingStyle}\n" +
                     $"Alignment: {character.Alignment}\n" +
                     $"Bio: {character.Bio}";

        await _dialogService.ShowInformationAsync(
            $"OpenMK Character: {character.DisplayName}",
            details);
    }

    [RelayCommand]
    private async Task ViewSpecialMovesAsync()
    {
        if (SelectedCharacter == null)
        {
            await _dialogService.ShowWarningAsync("Selection Required", "Please select a character first.");
            return;
        }

        var result = await _openMKService.GetCharacterSpecialMovesAsync(SelectedCharacter.Id);
        if (result.IsSuccess && result.Value != null)
        {
            var movesText = string.Join("\n\n", result.Value.Select(m =>
                $"{m.DisplayName}\nInput: {m.InputCommand}\nDamage: {m.Damage}\n{m.Description}"));

            await _dialogService.ShowInformationAsync(
                $"{SelectedCharacter.DisplayName} - Special Moves",
                movesText);
        }
    }

    [RelayCommand]
    private async Task ViewFatalitiesAsync()
    {
        if (SelectedCharacter == null)
        {
            await _dialogService.ShowWarningAsync("Selection Required", "Please select a character first.");
            return;
        }

        var result = await _openMKService.GetCharacterFatalitiesAsync(SelectedCharacter.Id);
        if (result.IsSuccess && result.Value != null)
        {
            var fatalitiesText = string.Join("\n\n", result.Value.Select(f =>
                $"{f.DisplayName}\nInput: {f.InputCommand}\n{f.Description}"));

            await _dialogService.ShowInformationAsync(
                $"{SelectedCharacter.DisplayName} - Fatalities",
                fatalitiesText);
        }
    }

    [RelayCommand]
    private async Task ViewCharacterEndingAsync()
    {
        if (SelectedCharacter == null)
        {
            await _dialogService.ShowWarningAsync("Selection Required", "Please select a character first.");
            return;
        }

        var result = await _openMKService.GetCharacterEndingAsync(SelectedCharacter.Id);
        if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
        {
            await _dialogService.ShowInformationAsync(
                $"{SelectedCharacter.DisplayName} - Ending",
                result.Value);
        }
    }
}