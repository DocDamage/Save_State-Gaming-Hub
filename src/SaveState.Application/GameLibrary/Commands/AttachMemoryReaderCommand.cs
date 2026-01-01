using MediatR;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands;

public class AttachMemoryReaderCommand : IRequest<Result>
{
    public int ProcessId { get; init; }
}

public class AttachMemoryReaderCommandHandler : IRequestHandler<AttachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;

    public AttachMemoryReaderCommandHandler(IGameMemoryReader memoryReader)
    {
        _memoryReader = memoryReader;
    }

    public async Task<Result> Handle(AttachMemoryReaderCommand request, CancellationToken cancellationToken)
    {
        return await _memoryReader.AttachToProcessAsync(request.ProcessId, cancellationToken);
    }
}