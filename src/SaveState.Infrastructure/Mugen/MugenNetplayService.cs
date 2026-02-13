namespace SaveState.Infrastructure.Mugen;

using System.Text.Json;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Loads and joins IKEMEN netplay lobbies.
/// </summary>
public class MugenNetplayService : IMugenNetplayService
{
    private readonly MugenOptions _options;

    public MugenNetplayService(IOptions<MugenOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Result<IReadOnlyList<MugenNetplayLobby>>> GetLobbiesAsync(CancellationToken ct = default)
    {
        try
        {
            var path = _options.NetplayLobbiesPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return Result.Failure<IReadOnlyList<MugenNetplayLobby>>("Netplay lobby catalog not found.");

            var json = await File.ReadAllTextAsync(path, ct);
            var lobbies = JsonSerializer.Deserialize<List<MugenNetplayLobby>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<MugenNetplayLobby>();

            return Result.Success<IReadOnlyList<MugenNetplayLobby>>(lobbies);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenNetplayLobby>>($"Failed to load lobbies: {ex.Message}");
        }
    }

    public Task<Result> JoinLobbyAsync(MugenNetplayLobby lobby, CancellationToken ct = default)
    {
        if (lobby == null)
            return Task.FromResult(Result.Failure("Lobby is required."));

        if (string.IsNullOrWhiteSpace(lobby.JoinUrl))
            return Task.FromResult(Result.Failure("Lobby does not provide a join URL."));

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = lobby.JoinUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to join lobby: {ex.Message}"));
        }
    }
}
