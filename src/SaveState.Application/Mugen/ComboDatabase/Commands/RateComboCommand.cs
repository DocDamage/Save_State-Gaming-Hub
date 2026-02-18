using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to rate a combo.
/// </summary>
public sealed record RateComboCommand(
    Guid ComboId,
    int Rating,
    string? UserId = null) : IRequest<Result>;

/// <summary>
/// Handler for RateComboCommand.
/// </summary>
public sealed class RateComboCommandHandler : IRequestHandler<RateComboCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public RateComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(RateComboCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.RateComboAsync(
            request.ComboId, 
            request.Rating, 
            request.UserId, 
            cancellationToken);
    }
}
