namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.Common;

public record StartPerformanceProfilingCommand(Guid GameId) : IRequest<Result>;