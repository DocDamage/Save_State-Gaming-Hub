using System.IO;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.RomManagement;
using SaveState.Core.GameLibrary;

namespace SaveState.Core.GameLibrary.DomainServices;

public class MetadataEnrichmentService : IMetadataEnrichmentService
{
    private readonly IMetadataService _metadataService;
    private readonly IPlatformRepository _platformRepository;
    private readonly IPlatformExtensionRegistry _extensionRegistry;

    public MetadataEnrichmentService(
        IMetadataService metadataService,
        IPlatformRepository platformRepository,
        IPlatformExtensionRegistry extensionRegistry)
    {
        _metadataService = metadataService;
        _platformRepository = platformRepository;
        _extensionRegistry = extensionRegistry;
    }

    public async Task EnrichGameMetadataAsync(Game game, CancellationToken ct = default)
    {
        var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);

        if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Title))
        {
            game.Update(
                title: metadata.Title,
                description: metadata.Description,
                coverImagePath: metadata.CoverImageUrl
            );
        }
    }

    public async Task<Result<string?>> GetCoverImageUrlAsync(Game game, CancellationToken ct = default)
    {
        try
        {
            var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
            return Result.Success<string?>(metadata?.CoverImageUrl);
        }
        catch (Exception ex)
        {
            return Result.Failure<string?>(
                $"Failed to retrieve cover image for '{game.Title}': {ex.Message}",
                ErrorType.External);
        }
    }

    public async Task<Result<IEnumerable<string>>> GetTagsAsync(Game game, CancellationToken ct = default)
    {
        try
        {
            var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
            return Result.Success(metadata?.Genres ?? Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            return Result.Failure<IEnumerable<string>>(
                $"Failed to retrieve tags for '{game.Title}': {ex.Message}",
                ErrorType.External);
        }
    }

    public async Task<Result<string?>> GetDescriptionAsync(Game game, CancellationToken ct = default)
    {
        try
        {
            var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
            return Result.Success<string?>(metadata?.Description);
        }
        catch (Exception ex)
        {
            return Result.Failure<string?>(
                $"Failed to retrieve description for '{game.Title}': {ex.Message}",
                ErrorType.External);
        }
    }

    public async Task<Result<Platform>> DetectPlatformAsync(string gamePath, CancellationToken ct = default)
    {
        var platformNameResult = _extensionRegistry.DetectPlatformName(gamePath);
        if (platformNameResult.IsFailure || platformNameResult.Value is null)
        {
            return Result.Failure<Platform>(platformNameResult.Error ?? "Failed to detect platform", platformNameResult.ErrorType);
        }

        var platform = await _platformRepository.GetByNameAsync(platformNameResult.Value, ct).ConfigureAwait(false);
        if (platform is null)
        {
            return Result.Failure<Platform>($"Platform '{platformNameResult.Value}' not found in repository", ErrorType.NotFound);
        }

        return Result.Success<Platform>(platform);
    }
}

