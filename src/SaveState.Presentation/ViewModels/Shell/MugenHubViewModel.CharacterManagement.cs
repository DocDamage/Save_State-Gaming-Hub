using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Character management partial class for MugenHubViewModel.
/// </summary>
public partial class MugenHubViewModel
{
    private void FilterCharacters()
    {
        if (FilteredCharacters == null) return;

        FilteredCharacters.Clear();
        foreach(var character in Characters)
        {
            if(string.IsNullOrWhiteSpace(SearchText) ||
               character.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                FilteredCharacters.Add(character);
            }
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(MugenCharacter? character)
    {
        if (character == null) return;

        try
        {
            var newState = !character.IsFavorite;
            var result = await _collectionService.SetFavoriteAsync(character.Id, newState);

            if (result.IsSuccess)
            {
                character.SetFavorite(newState);
                _notificationService.ShowSuccess($"{character.DisplayName} {(newState ? "added to" : "removed from")} favorites");
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to update favorite");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite");
            _notificationService.ShowError("Failed to update favorite");
        }
    }

    [RelayCommand]
    private void SelectCharacter(MugenCharacter? character)
    {
        SelectedCharacter = character;
    }

    [RelayCommand]
    private void ClearSelectedCharacter()
    {
        SelectedCharacter = null;
    }

    private async Task LoadCharactersAsync()
    {
        try
        {
            var result = await _collectionService.GetRosterAsync();

            Characters.Clear();
            if (result.IsSuccess && result.Value != null)
            {
               foreach(var c in result.Value) Characters.Add(c);
            }

            FilterCharacters();

            TotalCharacters = Characters.Count;
            FavoriteCharacters = Characters.Count(c => c.IsFavorite);

            if (SelectedBetCharacter != null && Characters.All(c => c.Id != SelectedBetCharacter.Id))
                SelectedBetCharacter = null;

            _logger.LogInformation("Loaded {Count} characters", Characters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load characters");
        }
    }
}
