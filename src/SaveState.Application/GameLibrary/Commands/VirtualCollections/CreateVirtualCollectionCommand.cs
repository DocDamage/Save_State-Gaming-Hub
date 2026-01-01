using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.VirtualCollections;

public sealed record CreateManualCollectionCommand(string Name, string? Icon = null) : IRequest<Result<VirtualCollection>>;

public sealed record CreateSmartCollectionCommand(string Name, CollectionFilter Filter, string? Icon = null) : IRequest<Result<VirtualCollection>>;

public sealed class CreateManualCollectionCommandHandler : IRequestHandler<CreateManualCollectionCommand, Result<VirtualCollection>>
{
    private readonly IVirtualCollectionService _collectionService;

    public CreateManualCollectionCommandHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<VirtualCollection>> Handle(CreateManualCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.CreateManualCollectionAsync(request.Name, request.Icon, ct);
    }
}

public sealed class CreateSmartCollectionCommandHandler : IRequestHandler<CreateSmartCollectionCommand, Result<VirtualCollection>>
{
    private readonly IVirtualCollectionService _collectionService;

    public CreateSmartCollectionCommandHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<VirtualCollection>> Handle(CreateSmartCollectionCommand request, CancellationToken ct)
    {
        return await _collectionService.CreateSmartCollectionAsync(request.Name, request.Filter, request.Icon, ct);
    }
}