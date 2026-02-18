using System.Globalization;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Resolves replay file paths from various input formats.
/// </summary>
public sealed class ReplayPathResolver : IReplayPathResolver
{
    private static readonly string[] ReplayExtensions = { ".json", ".log", ".txt", ".replay" };

    /// <summary>
    /// Static helper to resolve a replay path without creating an instance.
    /// </summary>
    public static Result<string> ResolveStatic(string replayPath)
    {
        if (File.Exists(replayPath))
        {
            return Result.Success(replayPath);
        }

        if (Directory.Exists(replayPath))
        {
            var replayFile = Directory
                .GetFiles(replayPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => IsReplayExtensionStatic(Path.GetExtension(path)))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (replayFile != null)
            {
                return Result.Success(replayFile);
            }

            return Result.Failure<string>($"No replay files found in directory: {replayPath}", ErrorType.NotFound);
        }

        var directory = Path.GetDirectoryName(replayPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        var baseName = Path.GetFileNameWithoutExtension(replayPath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return Result.Failure<string>("Invalid replay path", ErrorType.Validation);
        }

        foreach (var extension in ReplayExtensions)
        {
            var candidate = Path.Combine(directory, baseName + extension);
            if (File.Exists(candidate))
            {
                return Result.Success(candidate);
            }
        }

        return Result.Failure<string>($"Replay file not found: {replayPath}", ErrorType.NotFound);
    }

    private static bool IsReplayExtensionStatic(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return ReplayExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Result<string> ResolveReplayPath(string replayPath)
    {
        if (File.Exists(replayPath))
        {
            return Result.Success(replayPath);
        }

        if (Directory.Exists(replayPath))
        {
            var replayFile = Directory
                .GetFiles(replayPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => IsReplayExtension(Path.GetExtension(path)))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (replayFile != null)
            {
                return Result.Success(replayFile);
            }

            return Result.Failure<string>($"No replay files found in directory: {replayPath}", ErrorType.NotFound);
        }

        var directory = Path.GetDirectoryName(replayPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        var baseName = Path.GetFileNameWithoutExtension(replayPath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return Result.Failure<string>("Invalid replay path", ErrorType.Validation);
        }

        foreach (var extension in ReplayExtensions)
        {
            var candidate = Path.Combine(directory, baseName + extension);
            if (File.Exists(candidate))
            {
                return Result.Success(candidate);
            }
        }

        return Result.Failure<string>($"Replay file not found: {replayPath}", ErrorType.NotFound);
    }

    private static bool IsReplayExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return ReplayExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }
}
