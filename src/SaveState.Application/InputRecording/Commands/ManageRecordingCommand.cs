using MediatR;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;

namespace SaveState.Application.InputRecording.Commands;

/// <summary>
/// Command to update recording metadata.
/// </summary>
public sealed record UpdateRecordingCommand(
    Guid RecordingId,
    string Name,
    string? Description = null,
    List<string>? Tags = null) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for UpdateRecordingCommand.
/// </summary>
public sealed class UpdateRecordingCommandHandler : IRequestHandler<UpdateRecordingCommand, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public UpdateRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(UpdateRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.UpdateRecordingAsync(
            request.RecordingId, 
            request.Name, 
            request.Description, 
            request.Tags, 
            cancellationToken);
    }
}

/// <summary>
/// Command to delete a recording.
/// </summary>
public sealed record DeleteRecordingCommand(Guid RecordingId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteRecordingCommand.
/// </summary>
public sealed class DeleteRecordingCommandHandler : IRequestHandler<DeleteRecordingCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public DeleteRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(DeleteRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.DeleteRecordingAsync(request.RecordingId, cancellationToken);
    }
}

/// <summary>
/// Command to add a bookmark.
/// </summary>
public sealed record AddBookmarkCommand(Guid RecordingId, long FrameNumber, string Label) : IRequest<Result>;

/// <summary>
/// Handler for AddBookmarkCommand.
/// </summary>
public sealed class AddBookmarkCommandHandler : IRequestHandler<AddBookmarkCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public AddBookmarkCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(AddBookmarkCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.AddBookmarkAsync(
            request.RecordingId, 
            request.FrameNumber, 
            request.Label, 
            cancellationToken);
    }
}

/// <summary>
/// Command to remove a bookmark.
/// </summary>
public sealed record RemoveBookmarkCommand(Guid RecordingId, long FrameNumber) : IRequest<Result>;

/// <summary>
/// Handler for RemoveBookmarkCommand.
/// </summary>
public sealed class RemoveBookmarkCommandHandler : IRequestHandler<RemoveBookmarkCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public RemoveBookmarkCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(RemoveBookmarkCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.RemoveBookmarkAsync(
            request.RecordingId, 
            request.FrameNumber, 
            cancellationToken);
    }
}

/// <summary>
/// Command to toggle bookmark status.
/// </summary>
public sealed record ToggleBookmarkCommand(Guid RecordingId, bool IsBookmarked) : IRequest<Result>;

/// <summary>
/// Handler for ToggleBookmarkCommand.
/// </summary>
public sealed class ToggleBookmarkCommandHandler : IRequestHandler<ToggleBookmarkCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public ToggleBookmarkCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(ToggleBookmarkCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.ToggleBookmarkAsync(
            request.RecordingId, 
            request.IsBookmarked, 
            cancellationToken);
    }
}

/// <summary>
/// Command to export a recording.
/// </summary>
public sealed record ExportRecordingCommand(
    Guid RecordingId,
    string OutputPath,
    RecordingExportFormat Format = RecordingExportFormat.Native,
    bool IncludeMetadata = true) : IRequest<Result<string>>;

/// <summary>
/// Handler for ExportRecordingCommand.
/// </summary>
public sealed class ExportRecordingCommandHandler : IRequestHandler<ExportRecordingCommand, Result<string>>
{
    private readonly IInputRecordingService _recordingService;

    public ExportRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<string>> Handle(ExportRecordingCommand request, CancellationToken cancellationToken)
    {
        var exportRequest = new ExportRecordingRequest
        {
            RecordingId = request.RecordingId,
            OutputPath = request.OutputPath,
            Format = request.Format,
            IncludeMetadata = request.IncludeMetadata
        };

        return await _recordingService.ExportRecordingAsync(exportRequest, cancellationToken);
    }
}

/// <summary>
/// Command to import a recording.
/// </summary>
public sealed record ImportRecordingCommand(
    string FilePath,
    Guid GameId,
    string? CustomName = null,
    List<string>? Tags = null) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for ImportRecordingCommand.
/// </summary>
public sealed class ImportRecordingCommandHandler : IRequestHandler<ImportRecordingCommand, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public ImportRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(ImportRecordingCommand request, CancellationToken cancellationToken)
    {
        var importRequest = new ImportRecordingRequest
        {
            FilePath = request.FilePath,
            GameId = request.GameId,
            CustomName = request.CustomName,
            Tags = request.Tags
        };

        return await _recordingService.ImportRecordingAsync(importRequest, cancellationToken);
    }
}

/// <summary>
/// Command to trim a recording.
/// </summary>
public sealed record TrimRecordingCommand(Guid RecordingId, long StartFrame, long EndFrame) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for TrimRecordingCommand.
/// </summary>
public sealed class TrimRecordingCommandHandler : IRequestHandler<TrimRecordingCommand, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public TrimRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(TrimRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.TrimRecordingAsync(
            request.RecordingId, 
            request.StartFrame, 
            request.EndFrame, 
            cancellationToken);
    }
}

/// <summary>
/// Command to concatenate recordings.
/// </summary>
public sealed record ConcatenateRecordingsCommand(List<Guid> RecordingIds, string NewName) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for ConcatenateRecordingsCommand.
/// </summary>
public sealed class ConcatenateRecordingsCommandHandler : IRequestHandler<ConcatenateRecordingsCommand, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public ConcatenateRecordingsCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(ConcatenateRecordingsCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.ConcatenateRecordingsAsync(
            request.RecordingIds, 
            request.NewName, 
            cancellationToken);
    }
}
