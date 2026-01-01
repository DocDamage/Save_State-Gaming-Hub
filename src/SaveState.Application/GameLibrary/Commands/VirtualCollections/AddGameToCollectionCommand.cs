using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.VirtualCollections;

public sealed record AddGameToCollectionCommand(Guid CollectionId, Guid GameId) : IRequest<Result>;

public sealed class AddGameToCollectionCommandHandler : IRequestHandler<AddGameToCollectionCommand, Result>
{
    private readonly IVirtualCollectionService _collectionService;

    public AddGameToCollectionCommandHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(AddGameToCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.AddGameToCollectionAsync(request.CollectionId, request.GameId, ct);
    }
}