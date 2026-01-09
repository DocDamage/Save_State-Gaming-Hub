using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to capture a screenshot for a game.
/// </summary>
public sealed record CaptureScreenshotCommand(Guid GameId) : IRequest<Result<GameMedia>>;

/// <summary>
/// Command to record a video for a game.
/// </summary>
public sealed record RecordVideoCommand(Guid GameId, TimeSpan Duration) : IRequest<Result<GameMedia>>;
