using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.DTOs;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Queries.Handlers;

/// <summary>
/// Handler for retrieving ROM file details.
/// Provides information about ROM files, metadata, and validation status.
/// </summary>
public class GetRomDetailsQueryHandler : IRequestHandler<GetRomDetailsQuery, Result<RomDetailsDto>>
{
    private readonly IRomFileRepository _romRepository;

    public GetRomDetailsQueryHandler(IRomFileRepository romRepository)
    {
        _romRepository = romRepository;
    }

    /// <summary>
    /// Handles the query to get ROM file details.
    /// </summary>
    /// <param name="request">The ROM details query with file path.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the ROM details or an error.</returns>
    public async Task<Result<RomDetailsDto>> Handle(GetRomDetailsQuery request, CancellationToken ct)
    {
        var rom = await _romRepository.GetByIdAsync(request.RomFileId.Value, ct).ConfigureAwait(false);

        if (rom is null)
            return Result<RomDetailsDto>.Failure("ROM file not found");

        var dto = new RomDetailsDto
        {
            Id = RomFileId.From((Guid)rom.Id),
            Title = rom.Title,
            Platform = "Unknown", // Would be populated from platform relationship
            FilePath = rom.FilePath.Value,
            FileSize = rom.FileSize,
            Description = rom.Description,
            Region = rom.Region,
            Version = rom.Version,
            Status = rom.Status,
            Checksum = rom.Checksum,
            ScannedAt = rom.ScannedAt,
            VerifiedAt = rom.VerifiedAt
        };

        return Result<RomDetailsDto>.Success(dto);
    }
}
