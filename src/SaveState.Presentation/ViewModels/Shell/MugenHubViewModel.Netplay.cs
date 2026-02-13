using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Netplay partial class for MugenHubViewModel.
/// </summary>
public partial class MugenHubViewModel
{
    [RelayCommand]
    private async Task LoadNetplayAsync()
    {
        try
        {
            IsNetplayLoading = true;
            NetplayStatus = "Loading lobbies...";

            var result = await _netplayService.GetLobbiesAsync();
            NetplayLobbies.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var lobby in result.Value)
                    NetplayLobbies.Add(lobby);

                NetplayStatus = $"{NetplayLobbies.Count} lobbies found.";
            }
            else
            {
                NetplayStatus = result.Error ?? "Failed to load lobbies.";
            }
        }
        catch (Exception ex)
        {
            NetplayStatus = $"Lobby load failed: {ex.Message}";
        }
        finally
        {
            IsNetplayLoading = false;
        }
    }

    [RelayCommand]
    private async Task JoinLobbyAsync(MugenNetplayLobby? lobby)
    {
        if (lobby == null)
            return;

        var result = await _netplayService.JoinLobbyAsync(lobby);
        if (result.IsSuccess)
            _notificationService.ShowSuccess($"Joining lobby: {lobby.Name}");
        else
            _notificationService.ShowError(result.Error ?? "Failed to join lobby.");
    }
}
