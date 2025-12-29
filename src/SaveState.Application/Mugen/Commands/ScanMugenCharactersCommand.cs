namespace SaveState.Application.Mugen.Commands;

using MediatR;

/// <summary>
/// Command to scan a directory for MUGEN characters and add them to the library.
/// </summary>
public record ScanMugenCharactersCommand(
    string DirectoryPath,
    bool IncludeSubdirectories = true,
    bool OverwriteExisting = false
) : IRequest<Unit>;
