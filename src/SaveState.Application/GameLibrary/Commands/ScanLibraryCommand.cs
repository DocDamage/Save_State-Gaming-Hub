using MediatR;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.Commands;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to perform a full scan of the game library across all providers and folders.
/// </summary>
public sealed record ScanLibraryCommand : IRequest<Result>;

/// <summary>
/// Handler for ScanLibraryCommand.
/// </summary>
public sealed class ScanLibraryCommandHandler : IRequestHandler<ScanLibraryCommand, Result>
{
    private readonly IMediator _mediator;

    public ScanLibraryCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result> Handle(ScanLibraryCommand request, CancellationToken cancellationToken)
    {
        var romRoot = Path.Combine(Environment.CurrentDirectory, "data", "roms");

        var platforms = new Dictionary<string, string>
        {
            { "GBA", Path.Combine(romRoot, "gba") },
            { "NES", Path.Combine(romRoot, "nes") },
            { "SNES", Path.Combine(romRoot, "snes") },
            { "N64", Path.Combine(romRoot, "n64") },
            { "Nintendo DS", Path.Combine(romRoot, "nds") },
            { "Neo Geo", Path.Combine(romRoot, "neogeo") },
            { "Arcade", Path.Combine(romRoot, "arcade") },
            { "Atari 2600", Path.Combine(romRoot, "atari2600") },
            { "Genesis", Path.Combine(romRoot, "genesis") },
            { "Master System", Path.Combine(romRoot, "mastersystem") }
        };

        foreach (var platform in platforms)
        {
            if (Directory.Exists(platform.Value))
            {
                // Send the command to scan this specific folder
                await _mediator.Send(new ScanRomFolderCommand
                {
                    FolderPath = platform.Value,
                    PlatformName = platform.Key,
                    Recursive = true,
                    VerifyChecksums = false
                }, cancellationToken);
            }
        }

        return Result.Success();
    }
}
