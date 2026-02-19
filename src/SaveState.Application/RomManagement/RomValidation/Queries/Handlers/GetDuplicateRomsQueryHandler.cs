using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries.Handlers;

/// <summary>
/// Handler for finding duplicate ROM files.
/// </summary>
public class GetDuplicateRomsQueryHandler : MediatR.IRequestHandler<GetDuplicateRomsQuery, Result<List<DuplicateRomInfo>>>
{
    private readonly IRomValidationService _validationService;
    private readonly ILogger<GetDuplicateRomsQueryHandler> _logger;

    public GetDuplicateRomsQueryHandler(
        IRomValidationService validationService,
        ILogger<GetDuplicateRomsQueryHandler> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the query to find duplicate ROMs.
    /// </summary>
    public async Task<Result<List<DuplicateRomInfo>>> Handle(GetDuplicateRomsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Finding duplicate ROMs (Platform: {PlatformId}, HashType: {HashType})",
                request.PlatformId?.ToString() ?? "All",
                request.HashType?.ToString() ?? "SHA1");

            var result = await _validationService.FindDuplicatesAsync(
                request.PlatformId,
                request.HashType,
                ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Found {DuplicateCount} sets of duplicate ROMs",
                    result.Value.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding duplicate ROMs");
            return Result<List<DuplicateRomInfo>>.Failure($"Failed to find duplicates: {ex.Message}");
        }
    }
}
