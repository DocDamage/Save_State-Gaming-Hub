using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to update a shared collection.
/// </summary>
public record UpdateSharedCollectionCommand(
    Guid CollectionId,
    string? Title = null,
    string? Description = null,
    bool? IsPublic = null) : IRequest<Result>;

/// <summary>
/// Handler for updating shared collections.
/// </summary>
public class UpdateSharedCollectionCommandHandler : IRequestHandler<UpdateSharedCollectionCommand, Result>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public UpdateSharedCollectionCommandHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(UpdateSharedCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.UpdateCollectionAsync(
            request.CollectionId,
            request.Title,
            request.Description,
            request.IsPublic,
            ct);
    }
}