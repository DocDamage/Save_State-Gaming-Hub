using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.DTOs;

namespace SaveState.Application.RomManagement.Commands;

public record ScanRomFolderCommand : IRequest<Result<ScanResult>>
{
    public string FolderPath { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public bool Recursive { get; init; } = true;
    public bool VerifyChecksums { get; init; } = false;
}
