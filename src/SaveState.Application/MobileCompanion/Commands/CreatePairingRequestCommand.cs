using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record CreatePairingRequestCommand : IRequest<Result<PairingRequestDto>>;

public sealed class CreatePairingRequestCommandHandler : IRequestHandler<CreatePairingRequestCommand, Result<PairingRequestDto>>
{
    private readonly IMobileCompanionService _companionService;

    public CreatePairingRequestCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<PairingRequestDto>> Handle(CreatePairingRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await _companionService.CreatePairingRequestAsync(cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<PairingRequestDto>.Failure(result.Error!, result.ErrorType);
        }

        var dto = new PairingRequestDto
        {
            Id = result.Value.Id,
            PairingCode = result.Value.PairingCode,
            ExpiresAt = result.Value.ExpiresAt,
            CreatedAt = result.Value.CreatedAt
        };

        return Result<PairingRequestDto>.Success(dto);
    }
}
