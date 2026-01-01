using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.Categorization;

public sealed record AutoTagLibraryCommand : IRequest<Result>;

public sealed class AutoTagLibraryCommandHandler : IRequestHandler<AutoTagLibraryCommand, Result>
{
    private readonly ISmartCategorizationService _categorizationService;

    public AutoTagLibraryCommandHandler(ISmartCategorizationService categorizationService)
    {
        _categorizationService = categorizationService;
    }

    public async Task<Result> Handle(AutoTagLibraryCommand request, CancellationToken ct)
    {
        return await _categorizationService.AutoTagLibraryAsync(ct: ct);
    }
}