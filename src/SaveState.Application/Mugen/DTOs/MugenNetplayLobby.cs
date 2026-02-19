using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN netplay lobby information.
/// </summary>
public sealed class MugenNetplayLobby
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Ping { get; set; }
    public bool HasPassword { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Characters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
}
