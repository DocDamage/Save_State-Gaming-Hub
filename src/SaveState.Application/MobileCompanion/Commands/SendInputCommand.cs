using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record SendGamepadInputCommand(
    Guid DeviceId,
    GamepadInputDto Input
) : IRequest<Result>;

public sealed class SendGamepadInputCommandHandler : IRequestHandler<SendGamepadInputCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public SendGamepadInputCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(SendGamepadInputCommand request, CancellationToken cancellationToken)
    {
        var input = new GamepadInput
        {
            Button = request.Input.Button,
            IsPressed = request.Input.IsPressed,
            AxisX = request.Input.AxisX,
            AxisY = request.Input.AxisY
        };

        return await _companionService.SendGamepadInputAsync(
            request.DeviceId,
            input,
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed record SendTouchpadInputCommand(
    Guid DeviceId,
    TouchpadInputDto Input
) : IRequest<Result>;

public sealed class SendTouchpadInputCommandHandler : IRequestHandler<SendTouchpadInputCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public SendTouchpadInputCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(SendTouchpadInputCommand request, CancellationToken cancellationToken)
    {
        var input = new TouchpadInput
        {
            X = request.Input.X,
            Y = request.Input.Y,
            Action = request.Input.Action,
            FingerId = request.Input.FingerId
        };

        return await _companionService.SendTouchpadInputAsync(
            request.DeviceId,
            input,
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed record SendKeyboardInputCommand(
    Guid DeviceId,
    KeyboardInputDto Input
) : IRequest<Result>;

public sealed class SendKeyboardInputCommandHandler : IRequestHandler<SendKeyboardInputCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public SendKeyboardInputCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(SendKeyboardInputCommand request, CancellationToken cancellationToken)
    {
        var input = new KeyboardInput
        {
            Key = request.Input.Key,
            IsPressed = request.Input.IsPressed,
            IsModifier = request.Input.IsModifier,
            Modifiers = request.Input.Modifiers
        };

        return await _companionService.SendKeyboardInputAsync(
            request.DeviceId,
            input,
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed record SetControlModeCommand(
    Guid DeviceId,
    RemoteControlMode Mode
) : IRequest<Result>;

public sealed class SetControlModeCommandHandler : IRequestHandler<SetControlModeCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public SetControlModeCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(SetControlModeCommand request, CancellationToken cancellationToken)
    {
        return await _companionService.SetControlModeAsync(
            request.DeviceId,
            request.Mode,
            cancellationToken).ConfigureAwait(false);
    }
}
