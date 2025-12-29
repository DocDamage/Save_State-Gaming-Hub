using MediatR;
using SaveState.Application.AiGaming.DTOs;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.Enums;

namespace SaveState.Application.AiGaming.Queries;

public record GetCheatPatternsQuery : IRequest<Result<IReadOnlyList<CheatPatternDto>>>
{
    public string? GameTitle { get; init; }
    public CheatType? Type { get; init; }
}
