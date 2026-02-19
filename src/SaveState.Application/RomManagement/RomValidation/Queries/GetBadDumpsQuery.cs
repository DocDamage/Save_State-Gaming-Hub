using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to get all identified bad dump ROMs.
/// </summary>
public sealed record GetBadDumpsQuery(
    Guid? PlatformId = null) : IRequest<Result<List<BadDumpInfo>>>;
