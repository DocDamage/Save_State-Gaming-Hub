using MediatR;
using SaveState.Application.AiGaming.Options;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.AiGaming.Services;

namespace SaveState.Application.AiGaming.Commands;

public record DetectCheatsCommand : IRequest<Result<CheatDetectionResult>>
{
    public Guid ProcessId { get; init; }
    public IReadOnlyList<long> Addresses { get; init; } = Array.Empty<long>();
    public CheatDetectionOptions? Options { get; init; }
}
