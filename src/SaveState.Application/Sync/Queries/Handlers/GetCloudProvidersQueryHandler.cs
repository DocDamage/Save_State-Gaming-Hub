using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Application.Sync.Queries.Handlers;

public class GetCloudProvidersQueryHandler :
    IRequestHandler<GetCloudProvidersQuery, Result<IReadOnlyList<CloudGamingProvider>>>
{
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ILogger<GetCloudProvidersQueryHandler> _logger;

    public GetCloudProvidersQueryHandler(
        ICloudGamingManager cloudGamingManager,
        ILogger<GetCloudProvidersQueryHandler> logger)
    {
        _cloudGamingManager = cloudGamingManager;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CloudGamingProvider>>> Handle(
        GetCloudProvidersQuery request,
        CancellationToken ct)
    {
        try
        {
            var result = await _cloudGamingManager.GetAvailableProvidersAsync(ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} available cloud gaming providers",
                    result.Value.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available cloud gaming providers");
            return Result<IReadOnlyList<CloudGamingProvider>>.Failure(
                $"Failed to get providers: {ex.Message}");
        }
    }
}

public class GetActiveCloudSessionsQueryHandler :
    IRequestHandler<GetActiveCloudSessionsQuery, Result<IReadOnlyList<CloudSession>>>
{
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ILogger<GetActiveCloudSessionsQueryHandler> _logger;

    public GetActiveCloudSessionsQueryHandler(
        ICloudGamingManager cloudGamingManager,
        ILogger<GetActiveCloudSessionsQueryHandler> logger)
    {
        _cloudGamingManager = cloudGamingManager;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CloudSession>>> Handle(
        GetActiveCloudSessionsQuery request,
        CancellationToken ct)
    {
        try
        {
            var result = await _cloudGamingManager.GetActiveSessionsAsync(ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} active cloud gaming sessions",
                    result.Value.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active cloud gaming sessions");
            return Result<IReadOnlyList<CloudSession>>.Failure(
                $"Failed to get active sessions: {ex.Message}");
        }
    }
}