namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service interface for launching MUGEN/IKEMEN with specific characters and game modes.
/// </summary>
public interface IMugenLauncher
{
    /// <summary>
    /// Launches IKEMEN in versus mode with the specified characters.
    /// </summary>
    /// <param name="player1Character">Character for player 1.</param>
    /// <param name="player2Character">Character for player 2.</param>
    /// <param name="rounds">Number of rounds (default 3).</param>
    /// <returns>The launched process.</returns>
    Task<System.Diagnostics.Process> LaunchVersusAsync(string player1Character, string player2Character, int rounds = 3);

    /// <summary>
    /// Launches IKEMEN in training mode with the specified character.
    /// </summary>
    /// <param name="character">Character for training.</param>
    /// <param name="dummyCharacter">Character for the dummy opponent.</param>
    /// <returns>The launched process.</returns>
    Task<System.Diagnostics.Process> LaunchTrainingAsync(string character, string dummyCharacter = "KFM");

    /// <summary>
    /// Launches IKEMEN in single-player mode with the specified character.
    /// </summary>
    /// <param name="character">Character for single-player mode.</param>
    /// <returns>The launched process.</returns>
    Task<System.Diagnostics.Process> LaunchSinglePlayerAsync(string character);

    /// <summary>
    /// Launches IKEMEN in watch mode to observe a match.
    /// </summary>
    /// <param name="player1Character">Character for player 1.</param>
    /// <param name="player2Character">Character for player 2.</param>
    /// <returns>The launched process.</returns>
    Task<System.Diagnostics.Process> LaunchWatchAsync(string player1Character, string player2Character);

    /// <summary>
    /// Checks if IKEMEN is properly installed and available.
    /// </summary>
    /// <returns>True if IKEMEN is available.</returns>
    bool IsIkemenAvailable();

    /// <summary>
    /// Gets the path to the IKEMEN executable.
    /// </summary>
    /// <returns>The executable path, or null if not found.</returns>
    string? GetIkemenExecutablePath();
}
