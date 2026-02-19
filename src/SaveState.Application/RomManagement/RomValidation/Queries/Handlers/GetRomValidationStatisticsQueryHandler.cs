using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries.Handlers;

/// <summary>
/// Handler for getting ROM validation statistics.
/// </summary>
public class GetRomValidationStatisticsQueryHandler : MediatR.IRequestHandler<GetRomValidationStatisticsQuery, Result<RomValidationStatistics>>
{
    private readonly IRomValidationService _validationService;
    private readonly ILogger<GetRomValidationStatisticsQueryHandler> _logger;

    public GetRomValidationStatisticsQueryHandler(
        IRomValidationService validationService,
        ILogger<GetRomValidationStatisticsQueryHandler> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the query to get validation statistics.
    /// </summary>
    public async Task<Result<RomValidationStatistics>> Handle(GetRomValidationStatisticsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Getting ROM validation statistics");

            var result = await _validationService.GetStatisticsAsync(ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Validation stats: {Validated}/{Total} ROMs validated ({Percentage:F1}%)",
                    result.Value.ValidatedRoms,
                    result.Value.TotalRoms,
                    result.Value.ValidationPercentage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation statistics");
            return Result<RomValidationStatistics>.Failure($"Failed to get statistics: {ex.Message}");
        }
    }
}
