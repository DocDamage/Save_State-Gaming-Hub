using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Mugen.Services;
using System.Diagnostics;

/// <summary>
/// Implementation of the MUGEN launcher for starting IKEMEN with specific configurations.
/// </summary>
public class MugenLauncher : IMugenLauncher
{
    private readonly MugenOptions _options;
    private readonly string _workingDirectory;

    /// <summary>
    /// Initializes a new instance of the MugenLauncher.
    /// </summary>
    public MugenLauncher(IOptions<MugenOptions> options)
    {
        _options = options.Value;
        // Working directory should be the repository root
        _workingDirectory = Path.GetFullPath(".");
    }

    /// <summary>
    /// Launches IKEMEN in versus mode with the specified characters.
    /// </summary>
    /// <param name="player1Character">Character for player 1.</param>
    /// <param name="player2Character">Character for player 2.</param>
    /// <param name="rounds">Number of rounds (default 3).</param>
    /// <returns>The launched process.</returns>
    public async Task<Process> LaunchVersusAsync(string player1Character, string player2Character, int rounds = 3)
    {
        var arguments = $"-p1 \"{player1Character}\" -p2 \"{player2Character}\" -rounds {rounds}";
        return await LaunchIkemenAsync(arguments);
    }

    /// <summary>
    /// Launches IKEMEN in training mode with the specified character.
    /// </summary>
    /// <param name="character">Character for training.</param>
    /// <param name="dummyCharacter">Character for the dummy opponent.</param>
    /// <returns>The launched process.</returns>
    public async Task<Process> LaunchTrainingAsync(string character, string dummyCharacter = "KFM")
    {
        var arguments = $"-p1 \"{character}\" -p2 \"{dummyCharacter}\" -training";
        return await LaunchIkemenAsync(arguments);
    }

    /// <summary>
    /// Launches IKEMEN in single-player mode with the specified character.
    /// </summary>
    /// <param name="character">Character for single-player mode.</param>
    /// <returns>The launched process.</returns>
    public async Task<Process> LaunchSinglePlayerAsync(string character)
    {
        var arguments = $"-p1 \"{character}\" -single";
        return await LaunchIkemenAsync(arguments);
    }

    /// <summary>
    /// Launches IKEMEN in watch mode to observe a match.
    /// </summary>
    /// <param name="player1Character">Character for player 1.</param>
    /// <param name="player2Character">Character for player 2.</param>
    /// <returns>The launched process.</returns>
    public async Task<Process> LaunchWatchAsync(string player1Character, string player2Character)
    {
        var arguments = $"-p1 \"{player1Character}\" -p2 \"{player2Character}\" -watch";
        return await LaunchIkemenAsync(arguments);
    }

    /// <summary>
    /// Checks if IKEMEN is properly installed and available.
    /// </summary>
    /// <returns>True if IKEMEN is available.</returns>
    public bool IsIkemenAvailable()
    {
        var exePath = GetIkemenExecutablePath();
        return exePath != null && File.Exists(exePath);
    }

    /// <summary>
    /// Gets the path to the IKEMEN executable.
    /// </summary>
    /// <returns>The executable path, or null if not found.</returns>
    public string? GetIkemenExecutablePath()
    {
        var exePath = Path.Combine(_workingDirectory, _options.ExecutablePath);
        return File.Exists(exePath) ? exePath : null;
    }

    /// <summary>
    /// Launches IKEMEN to play back a replay file.
    /// </summary>
    /// <param name="replayPath">Path to the replay file.</param>
    /// <returns>The launched process.</returns>
    public async Task<Process> LaunchReplayAsync(string replayPath)
    {
        if (!File.Exists(replayPath))
        {
            throw new FileNotFoundException("Replay file not found", replayPath);
        }

        // IKEMEN typically uses -replay or -r flag for replay playback
        var arguments = $"-replay \"{replayPath}\"";
        return await LaunchIkemenAsync(arguments);
    }

    private async Task<Process> LaunchIkemenAsync(string arguments)
    {
        var exePath = GetIkemenExecutablePath();
        if (exePath == null)
        {
            throw new FileNotFoundException("IKEMEN executable not found", _options.ExecutablePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(exePath),
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start IKEMEN process");
        }

        // Give the process a moment to start
        await Task.Delay(_options.ProcessStartupDelayMs);

        return process;
    }
}
