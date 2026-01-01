namespace SaveState.Application.Input.Commands;

using MediatR;
using SaveState.Core.Common;

public record StartVoiceListeningCommand() : IRequest<Result<bool>>;