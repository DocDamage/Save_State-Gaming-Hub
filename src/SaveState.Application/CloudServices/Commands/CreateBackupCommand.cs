using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.Enums;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.CloudServices.Commands;

public record CreateBackupCommand : IRequest<Result<BackupId>>
{
    public BackupType Type { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<GameId>? GameIds { get; init; }
    public bool IncludeSettings { get; init; } = true;
}
