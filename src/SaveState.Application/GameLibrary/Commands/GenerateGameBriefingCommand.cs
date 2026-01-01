using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to generate a comprehensive game briefing.
/// </summary>
public record GenerateGameBriefingCommand(Guid GameId) : IRequest<Result<GameBriefing>>;

/// <summary>
/// Command to generate a last session summary.
/// </summary>
public record GenerateLastSessionSummaryCommand(Guid GameId) : IRequest<Result<string>>;

/// <summary>
/// Command to get current objectives for a game.
/// </summary>
public record GetCurrentObjectivesCommand(Guid GameId) : IRequest<Result<IReadOnlyList<string>>>;

/// <summary>
/// Command to get helpful tips for a game.
/// </summary>
public record GetGameTipsCommand(Guid GameId) : IRequest<Result<IReadOnlyList<string>>>;

/// <summary>
/// Command to generate a quick briefing for mobile/Big Picture mode.
/// </summary>
public record GenerateQuickBriefingCommand(Guid GameId) : IRequest<Result<GameBriefing>>;
