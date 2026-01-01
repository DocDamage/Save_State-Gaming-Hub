using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to delete a shared collection.
/// </summary>
public record DeleteSharedCollectionCommand(Guid CollectionId) : IRequest<Result>;

/// <summary>
/// Handler for deleting shared collections.
/// </summary>
public class DeleteSharedCollectionCommandHandler : IRequestHandler<DeleteSharedCollectionCommand, Result>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public DeleteSharedCollectionCommandHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(DeleteSharedCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.DeleteCollectionAsync(request.CollectionId, ct);
    }
}