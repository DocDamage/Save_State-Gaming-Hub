namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a discoverable IKEMEN GO netplay lobby.
/// </summary>
public sealed record MugenNetplayLobby(
    string Name,
    string Host,
    int Players,
    int MaxPlayers,
    string? Region,
    string? JoinUrl,
    string? Description);
