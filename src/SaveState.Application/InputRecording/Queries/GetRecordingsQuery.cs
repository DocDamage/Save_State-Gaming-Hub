using MediatR;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;

namespace SaveState.Application.InputRecording.Queries;

/// <summary>
/// Query to get recordings with filtering.
/// </summary>
public sealed record GetRecordingsQuery(
    Guid? GameId = null,
    RecordingType? Type = null,
    RecordingStatus? Status = null,
    InputDeviceType? DeviceType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    List<string>? Tags = null,
    bool? OnlyBookmarked = null,
    bool? OnlyVerifiedTAS = null,
    string? SearchQuery = null) : IRequest<Result<List<InputRecordingEntity>>>;

/// <summary>
/// Handler for GetRecordingsQuery.
/// </summary>
public sealed class GetRecordingsQueryHandler : IRequestHandler<GetRecordingsQuery, Result<List<InputRecordingEntity>>>
{
    private readonly IInputRecordingService _recordingService;

    public GetRecordingsQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<List<InputRecordingEntity>>> Handle(GetRecordingsQuery request, CancellationToken cancellationToken)
    {
        var filter = new InputRecordingFilter
        {
            GameId = request.GameId,
            Type = request.Type,
            Status = request.Status,
            DeviceType = request.DeviceType,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Tags = request.Tags,
            OnlyBookmarked = request.OnlyBookmarked,
            OnlyVerifiedTAS = request.OnlyVerifiedTAS,
            SearchQuery = request.SearchQuery
        };

        return await _recordingService.GetRecordingsAsync(filter, cancellationToken);
    }
}

/// <summary>
/// Query to get a specific recording.
/// </summary>
public sealed record GetRecordingQuery(Guid RecordingId) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for GetRecordingQuery.
/// </summary>
public sealed class GetRecordingQueryHandler : IRequestHandler<GetRecordingQuery, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public GetRecordingQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(GetRecordingQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetRecordingAsync(request.RecordingId, cancellationToken);
    }
}

/// <summary>
/// Query to get active recording session for a game.
/// </summary>
public sealed record GetActiveRecordingQuery(Guid GameId) : IRequest<Result<RecordingSession>>;

/// <summary>
/// Handler for GetActiveRecordingQuery.
/// </summary>
public sealed class GetActiveRecordingQueryHandler : IRequestHandler<GetActiveRecordingQuery, Result<RecordingSession>>
{
    private readonly IInputRecordingService _recordingService;

    public GetActiveRecordingQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<RecordingSession>> Handle(GetActiveRecordingQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetActiveRecordingAsync(request.GameId, cancellationToken);
    }
}

/// <summary>
/// Query to get frame data for a recording.
/// </summary>
public sealed record GetFrameDataQuery(Guid RecordingId) : IRequest<Result<List<InputFrame>>>;

/// <summary>
/// Handler for GetFrameDataQuery.
/// </summary>
public sealed class GetFrameDataQueryHandler : IRequestHandler<GetFrameDataQuery, Result<List<InputFrame>>>
{
    private readonly IInputRecordingService _recordingService;

    public GetFrameDataQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<List<InputFrame>>> Handle(GetFrameDataQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetFrameDataAsync(request.RecordingId, cancellationToken);
    }
}

/// <summary>
/// Query to get a range of frames.
/// </summary>
public sealed record GetFrameRangeQuery(Guid RecordingId, long StartFrame, long EndFrame) : IRequest<Result<List<InputFrame>>>;

/// <summary>
/// Handler for GetFrameRangeQuery.
/// </summary>
public sealed class GetFrameRangeQueryHandler : IRequestHandler<GetFrameRangeQuery, Result<List<InputFrame>>>
{
    private readonly IInputRecordingService _recordingService;

    public GetFrameRangeQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<List<InputFrame>>> Handle(GetFrameRangeQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetFrameRangeAsync(
            request.RecordingId, 
            request.StartFrame, 
            request.EndFrame, 
            cancellationToken);
    }
}

/// <summary>
/// Query to get input histogram.
/// </summary>
public sealed record GetInputHistogramQuery(Guid RecordingId) : IRequest<Result<Dictionary<string, int>>>;

/// <summary>
/// Handler for GetInputHistogramQuery.
/// </summary>
public sealed class GetInputHistogramQueryHandler : IRequestHandler<GetInputHistogramQuery, Result<Dictionary<string, int>>>
{
    private readonly IInputRecordingService _recordingService;

    public GetInputHistogramQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<Dictionary<string, int>>> Handle(GetInputHistogramQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetInputHistogramAsync(request.RecordingId, cancellationToken);
    }
}

/// <summary>
/// Query to get input recording statistics.
/// </summary>
public sealed record GetRecordingStatisticsQuery(Guid? GameId = null) : IRequest<Result<InputRecordingStatistics>>;

/// <summary>
/// Handler for GetRecordingStatisticsQuery.
/// </summary>
public sealed class GetRecordingStatisticsQueryHandler : IRequestHandler<GetRecordingStatisticsQuery, Result<InputRecordingStatistics>>
{
    private readonly IInputRecordingService _recordingService;

    public GetRecordingStatisticsQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingStatistics>> Handle(GetRecordingStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.GetStatisticsAsync(request.GameId, cancellationToken);
    }
}

/// <summary>
/// Query to validate a recording.
/// </summary>
public sealed record ValidateRecordingQuery(Guid RecordingId) : IRequest<Result<bool>>;

/// <summary>
/// Handler for ValidateRecordingQuery.
/// </summary>
public sealed class ValidateRecordingQueryHandler : IRequestHandler<ValidateRecordingQuery, Result<bool>>
{
    private readonly IInputRecordingService _recordingService;

    public ValidateRecordingQueryHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<bool>> Handle(ValidateRecordingQuery request, CancellationToken cancellationToken)
    {
        return await _recordingService.ValidateRecordingAsync(request.RecordingId, cancellationToken);
    }
}
