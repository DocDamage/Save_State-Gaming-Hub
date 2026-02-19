using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to find duplicate ROM files.
/// </summary>
public sealed record GetDuplicateRomsQuery(
    Guid? PlatformId = null,
    HashAlgorithmType? HashType = null) : IRequest<Result<List<DuplicateRomInfo>>>;
