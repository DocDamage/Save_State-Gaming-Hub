using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to create a new shared collection.
/// </summary>
public record CreateSharedCollectionCommand(
    string Title,
    string? Description = null,
    bool IsPublic = false) : IRequest<Result>;

/// <summary>
/// Handler for creating shared collections.
/// </summary>
public class CreateSharedCollectionCommandHandler : IRequestHandler<CreateSharedCollectionCommand, Result>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public CreateSharedCollectionCommandHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(CreateSharedCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.CreateCollectionAsync(
            request.Title,
            request.Description,
            request.IsPublic,
            ct);
    }
}