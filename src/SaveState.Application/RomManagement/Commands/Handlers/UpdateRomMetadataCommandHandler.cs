using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Commands.Handlers;

/// <summary>
/// Handler for updating ROM metadata.
/// </summary>
public class UpdateRomMetadataCommandHandler : IRequestHandler<UpdateRomMetadataCommand, Result>
{
    private readonly IRomFileRepository _romFileRepository;
    private readonly ILogger<UpdateRomMetadataCommandHandler> _logger;

    public UpdateRomMetadataCommandHandler(
        IRomFileRepository romFileRepository,
        ILogger<UpdateRomMetadataCommandHandler> logger)
    {
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the command to update ROM metadata.
    /// </summary>
    /// <param name="request">The update ROM metadata command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(UpdateRomMetadataCommand request, CancellationToken ct)
    {
        // Get the ROM file
        var romFile = await _romFileRepository.GetByIdAsync(request.RomFileId, ct).ConfigureAwait(false);
        if (romFile is null)
            return Result.Failure("ROM file not found", ErrorType.NotFound);

        var originalTitle = romFile.Title;

        // Update the ROM file metadata
        romFile.SetMetadata(request.Description, request.Region, request.Version);

        // Update title if changed
        if (!string.Equals(romFile.Title, request.Title, StringComparison.Ordinal))
        {
            romFile.UpdateTitle(request.Title);
        }

        // Save the updated ROM file
        await _romFileRepository.UpdateAsync(romFile, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "ROM metadata updated successfully. ROM ID: {RomId}, Title: '{OriginalTitle}' -> '{NewTitle}'",
            romFile.Id, originalTitle, request.Title);

        return Result.Success();
    }
}