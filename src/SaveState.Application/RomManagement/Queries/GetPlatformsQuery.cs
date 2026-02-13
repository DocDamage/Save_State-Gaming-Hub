using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;

namespace SaveState.Application.RomManagement.Queries;

/// <summary>
/// Query to get all available platforms.
/// </summary>
public record GetPlatformsQuery : IRequest<Result<IReadOnlyList<PlatformDto>>>;

/// <summary>
/// DTO for platform information.
/// </summary>
public record PlatformDto(
    Guid Id,
    string Name,
    string ShortName,
    string Type);