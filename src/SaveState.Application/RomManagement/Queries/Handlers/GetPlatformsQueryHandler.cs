using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.RomManagement.Queries.Handlers;

/// <summary>
/// Handler for getting all available platforms.
/// </summary>
public class GetPlatformsQueryHandler : IRequestHandler<GetPlatformsQuery, Result<IReadOnlyList<PlatformDto>>>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<GetPlatformsQueryHandler> _logger;

    public GetPlatformsQueryHandler(
        IPlatformRepository platformRepository,
        ILogger<GetPlatformsQueryHandler> logger)
    {
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the query to get all platforms.
    /// </summary>
    /// <param name="request">The get platforms query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of platforms.</returns>
    public async Task<Result<IReadOnlyList<PlatformDto>>> Handle(GetPlatformsQuery request, CancellationToken ct)
    {
        var platforms = await _platformRepository.GetAllAsync(ct).ConfigureAwait(false);

        var platformDtos = platforms
            .OrderBy(p => p.Name.Value)
            .Select(p => new PlatformDto(
                p.Id,
                p.Name.Value,
                p.ShortName.Value,
                p.Type.ToString()))
            .ToList();

        _logger.LogInformation("Retrieved {Count} platforms", platformDtos.Count);

        return Result.Success<IReadOnlyList<PlatformDto>>(platformDtos);
    }
}