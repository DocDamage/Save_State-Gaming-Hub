using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to review a submitted combo.
/// </summary>
public sealed record ReviewSubmissionCommand(
    Guid SubmissionId,
    SubmissionStatus Status,
    string? ReviewerNotes = null,
    string? ReviewedBy = null) : IRequest<Result>;

/// <summary>
/// Handler for ReviewSubmissionCommand.
/// </summary>
public sealed class ReviewSubmissionCommandHandler : IRequestHandler<ReviewSubmissionCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public ReviewSubmissionCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(ReviewSubmissionCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.ReviewSubmissionAsync(
            request.SubmissionId,
            request.Status,
            request.ReviewerNotes,
            request.ReviewedBy,
            cancellationToken);
    }
}
