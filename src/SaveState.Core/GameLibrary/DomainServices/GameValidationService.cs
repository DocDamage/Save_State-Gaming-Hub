using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Core.GameLibrary.DomainServices;

public class GameValidationService : IGameValidationService
{
    private readonly IFileSystem _fileSystem;

    public GameValidationService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<bool> IsValidGameAsync(Game game, CancellationToken ct)
    {
        var errors = await GetValidationErrorsAsync(game, ct).ConfigureAwait(false);
        return !errors.Any();
    }

    public async Task<IReadOnlyList<string>> GetValidationErrorsAsync(Game game, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check title
        if (string.IsNullOrWhiteSpace(game.Title))
            errors.Add("Game title is required");

        // Check platform - either ID or navigation property is sufficient
        if (!game.PlatformId.HasValue && game.Platform is null)
            errors.Add("Platform is required");

        // Check install path if installed
        if (game.Status == GameStatus.Installed && string.IsNullOrWhiteSpace(game.InstallPath))
            errors.Add("Install path is required for installed games");

        // Check if install path exists
        if (!string.IsNullOrWhiteSpace(game.InstallPath) &&
            !await _fileSystem.DirectoryExistsAsync(game.InstallPath, ct).ConfigureAwait(false))
        {
            errors.Add($"Install path '{game.InstallPath}' does not exist");
        }

        // Validate files
        foreach (var file in game.Files)
        {
            if (!await _fileSystem.FileExistsAsync(file.Path, ct).ConfigureAwait(false))
            {
                errors.Add($"Game file '{file.Path}' does not exist");
            }
        }

        return errors;
    }

    public async Task<bool> CanLaunchGameAsync(Game game, CancellationToken ct)
    {
        if (game.Status != GameStatus.Installed)
            return false;

        if (string.IsNullOrWhiteSpace(game.InstallPath))
            return false;

        // Check if main executable exists
        var mainExecutableResult = await GetMainExecutablePathAsync(game, ct).ConfigureAwait(false);
        if (mainExecutableResult.IsFailure || mainExecutableResult.Value is null)
            return false;

        return await _fileSystem.FileExistsAsync(mainExecutableResult.Value, ct).ConfigureAwait(false);
    }

    private async Task<Result<string>> GetMainExecutablePathAsync(Game game, CancellationToken ct)
    {
        // Platform-specific logic to find main executable
        var platformType = game.Platform?.Type ?? PlatformType.Console; // Fallback

        // If platform is null but platformId exists, we might still have a problem finding type
        // For now, assume PC if path is set and it's a computer-like path, or just check the platform property

        if (platformType == PlatformType.Computer || (game.InstallPath != null && game.InstallPath.Contains(":")))
        {
            var result = await FindPcExecutableAsync(game.InstallPath!, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
                return Result.Failure<string>($"No executable found for PC game '{game.Title}'", ErrorType.NotFound);
            return Result.Success<string>(result.Value);
        }

        return Result.Failure<string>($"Platform type '{platformType}' is not supported for executable detection", ErrorType.Validation);
    }

    private async Task<Result<string>> FindPcExecutableAsync(string installPath, CancellationToken ct)
    {
        // Look for common executable patterns
        var patterns = new[] { "*.exe", "*.bat", "*.cmd" };

        foreach (var pattern in patterns)
        {
            var files = await _fileSystem.GetFilesAsync(installPath, pattern, SearchOption.TopDirectoryOnly, ct).ConfigureAwait(false);
            if (files.Any())
                return Result.Success<string>(files.First());
        }

        return Result.Failure<string>($"No executable files found in '{installPath}'", ErrorType.NotFound);
    }
}

