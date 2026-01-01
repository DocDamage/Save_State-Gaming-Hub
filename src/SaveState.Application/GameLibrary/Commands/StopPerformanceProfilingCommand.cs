namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.Common;

public record StopPerformanceProfilingCommand() : IRequest<Result>;