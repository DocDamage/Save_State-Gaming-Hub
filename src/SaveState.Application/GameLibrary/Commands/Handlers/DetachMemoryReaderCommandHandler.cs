namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class DetachMemoryReaderCommandHandler : IRequestHandler<DetachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;

    public DetachMemoryReaderCommandHandler(IGameMemoryReader memoryReader)
    {
        _memoryReader = memoryReader;
    }

    public async Task<Result> Handle(DetachMemoryReaderCommand request, CancellationToken ct)
    {
        return await _memoryReader.DetachAsync(ct);
    }
}