using MediatR;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries;

public class GetMemoryPatternsQuery : IRequest<Result<IReadOnlyList<MemoryPattern>>>
{
    public int ProcessId { get; init; }
}

public class GetMemoryPatternsQueryHandler : IRequestHandler<GetMemoryPatternsQuery, Result<IReadOnlyList<MemoryPattern>>>
{
    private readonly IGameMemoryReader _memoryReader;

    public GetMemoryPatternsQueryHandler(IGameMemoryReader memoryReader)
    {
        _memoryReader = memoryReader;
    }

    public async Task<Result<IReadOnlyList<MemoryPattern>>> Handle(GetMemoryPatternsQuery request, CancellationToken cancellationToken)
    {
        // First ensure we're attached to the process
        if (!_memoryReader.IsAttached)
        {
            var attachResult = await _memoryReader.AttachToProcessAsync(request.ProcessId, cancellationToken);
            if (!attachResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<MemoryPattern>>(attachResult.Error ?? "Unknown error");
            }
        }

        return await _memoryReader.DetectPatternsAsync(cancellationToken);
    }
}
