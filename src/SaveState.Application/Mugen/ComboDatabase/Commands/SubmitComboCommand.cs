using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to submit a combo for community approval.
/// </summary>
public sealed record SubmitComboCommand(
    Guid ComboId,
    string SubmitterName,
    string? SubmitterId = null) : IRequest<Result<ComboSubmission>>;

/// <summary>
/// Handler for SubmitComboCommand.
/// </summary>
public sealed class SubmitComboCommandHandler : IRequestHandler<SubmitComboCommand, Result<ComboSubmission>>
{
    private readonly IComboDatabaseService _comboService;

    public SubmitComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboSubmission>> Handle(SubmitComboCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.SubmitComboAsync(
            request.ComboId,
            request.SubmitterName,
            request.SubmitterId,
            cancellationToken);
    }
}
