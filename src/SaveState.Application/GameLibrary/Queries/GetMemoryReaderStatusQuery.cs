using MediatR;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries;

public class GetMemoryReaderStatusQuery : IRequest<Result<MemoryReaderStatus>>
{
}

public class GetMemoryReaderStatusQueryHandler : IRequestHandler<GetMemoryReaderStatusQuery, Result<MemoryReaderStatus>>
{
    private readonly IGameMemoryReader _memoryReader;

    public GetMemoryReaderStatusQueryHandler(IGameMemoryReader memoryReader)
    {
        _memoryReader = memoryReader;
    }

    public Task<Result<MemoryReaderStatus>> Handle(GetMemoryReaderStatusQuery request, CancellationToken cancellationToken)
    {
        var status = new MemoryReaderStatus
        {
            IsAttached = _memoryReader.IsAttached,
            IsSupported = true, // Windows-specific, but we can detect this
            Platform = Environment.OSVersion.Platform.ToString()
        };

        return Task.FromResult(Result.Success<MemoryReaderStatus>(status));
    }
}

public class MemoryReaderStatus
{
    public bool IsAttached { get; init; }
    public bool IsSupported { get; init; }
    public string Platform { get; init; } = string.Empty;
    public string? AttachedProcessName { get; init; }
}

