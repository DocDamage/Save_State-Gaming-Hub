using MediatR;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands;

public class DetachMemoryReaderCommand : IRequest<Result>
{
}

public class DetachMemoryReaderCommandHandler : IRequestHandler<DetachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;

    public DetachMemoryReaderCommandHandler(IGameMemoryReader memoryReader)
    {
        _memoryReader = memoryReader;
    }

    public async Task<Result> Handle(DetachMemoryReaderCommand request, CancellationToken cancellationToken)
    {
        return await _memoryReader.DetachAsync(cancellationToken);
    }
}