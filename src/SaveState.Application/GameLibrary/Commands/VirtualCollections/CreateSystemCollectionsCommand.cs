using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.VirtualCollections;

public sealed record CreateSystemCollectionsCommand : IRequest<Result>;

public sealed class CreateSystemCollectionsCommandHandler : IRequestHandler<CreateSystemCollectionsCommand, Result>
{
    private readonly IVirtualCollectionService _collectionService;

    public CreateSystemCollectionsCommandHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result> Handle(CreateSystemCollectionsCommand request, CancellationToken ct)
    {
        return await _collectionService.CreateSystemCollectionsAsync(ct);
    }
}