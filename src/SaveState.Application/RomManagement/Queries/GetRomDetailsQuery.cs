using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.DTOs;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.RomManagement.Queries;

public record GetRomDetailsQuery : IRequest<Result<RomDetailsDto>>
{
    public required RomFileId RomFileId { get; init; }
}
