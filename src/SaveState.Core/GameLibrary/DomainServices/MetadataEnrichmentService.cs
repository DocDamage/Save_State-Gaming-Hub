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

    public async Task<string?> GetCoverImageUrlAsync(Game game, CancellationToken ct = default)
    {
        var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
        return metadata?.CoverImageUrl;
    }

    public async Task<IEnumerable<string>> GetTagsAsync(Game game, CancellationToken ct = default)
    {
        var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
        return metadata?.Genres ?? Enumerable.Empty<string>();
    }

    public async Task<string?> GetDescriptionAsync(Game game, CancellationToken ct = default)
    {
        var metadata = await _metadataService.GetGameMetadataAsync(game.Title, ct).ConfigureAwait(false);
        return metadata?.Description;
    }

    public async Task<Result<Platform>> DetectPlatformAsync(string gamePath, CancellationToken ct = default)
    {
        var platformNameResult = _extensionRegistry.DetectPlatformName(gamePath);
        if (platformNameResult.IsFailure)
        {
            // Error is guaranteed to be non-null when IsFailure is true
            return Result.Failure<Platform>(platformNameResult.Error!, platformNameResult.ErrorType);
        }

        // Value is guaranteed to be non-null when IsSuccess is true
        var platform = await _platformRepository.GetByNameAsync(platformNameResult.Value!, ct).ConfigureAwait(false);
        if (platform is null)
        {
            return Result.Failure<Platform>($"Platform '{platformNameResult.Value}' not found in repository", ErrorType.NotFound);
        }

        return Result.Success<Platform>(platform);
    }
}

