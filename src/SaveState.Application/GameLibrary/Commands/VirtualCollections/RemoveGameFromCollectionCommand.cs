using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.VirtualCollections;

public sealed record RemoveGameFromCollectionCommand(Guid CollectionId, Guid GameId) : IRequest<Result>;

public sealed class RemoveGameFromCollectionCommandHandler : IRequestHandler<RemoveGameFromCollectionCommand, Result>
{
    private readonly IVirtualCollectionService _collectionService;

    public RemoveGameFromCollectionCommandHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(RemoveGameFromCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.RemoveGameFromCollectionAsync(request.CollectionId, request.GameId, ct);
    }
}