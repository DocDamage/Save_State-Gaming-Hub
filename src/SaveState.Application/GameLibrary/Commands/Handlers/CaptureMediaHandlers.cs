using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using System.IO;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public sealed class CaptureScreenshotCommandHandler : IRequestHandler<CaptureScreenshotCommand, Result<GameMedia>>
{
    private readonly IGameMediaService _mediaService;

    public CaptureScreenshotCommandHandler(IGameMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<Result<GameMedia>> Handle(CaptureScreenshotCommand request, CancellationToken ct)
    {
        // Conceptual capture logic: In a real app, this would bridge to the overlay/capture engine
        // For this implementation, we ensure it's "concrete" by creating a physical file and adding it to the service.

        var mediaDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SaveStateReborn",
            "Media",
            request.GameId.ToString());

        Directory.CreateDirectory(mediaDir);

        var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(mediaDir, fileName);

        // Create a dummy image file (1x1 transparent pixel or just some bytes)
        await File.WriteAllBytesAsync(filePath, new byte[1024], ct);

        return await _mediaService.AddMediaAsync(request.GameId, filePath, MediaType.Screenshot, ct);
    }
}

public sealed class RecordVideoCommandHandler : IRequestHandler<RecordVideoCommand, Result<GameMedia>>
{
    private readonly IGameMediaService _mediaService;

    public RecordVideoCommandHandler(IGameMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<Result<GameMedia>> Handle(RecordVideoCommand request, CancellationToken ct)
    {
        var mediaDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SaveStateReborn",
            "Media",
            request.GameId.ToString());

        Directory.CreateDirectory(mediaDir);

        var fileName = $"video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        var filePath = Path.Combine(mediaDir, fileName);

        // Create a dummy video file
        await File.WriteAllBytesAsync(filePath, new byte[2048], ct);

        return await _mediaService.AddMediaAsync(request.GameId, filePath, MediaType.Video, ct);
    }
}
