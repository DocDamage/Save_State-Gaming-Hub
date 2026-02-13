using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record EnableSmartPauseCommand(SmartPauseOptions Options) : IRequest<Result>;

public sealed class EnableSmartPauseCommandHandler : IRequestHandler<EnableSmartPauseCommand, Result>
{
    private readonly IGameAssistantService _assistantService;

    public EnableSmartPauseCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result> Handle(EnableSmartPauseCommand request, CancellationToken ct)
    {
        return await _assistantService.EnableSmartPauseAsync(request.Options, ct);
    }
}
