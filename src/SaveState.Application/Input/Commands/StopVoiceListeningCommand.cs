namespace SaveState.Application.Input.Commands;

using MediatR;
using SaveState.Core.Common;

public record StopVoiceListeningCommand() : IRequest<Result<bool>>;