using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.DTOs;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Queries.Handlers;

public class GetRomDetailsQueryHandler : IRequestHandler<GetRomDetailsQuery, Result<RomDetailsDto>>
{
    private readonly IRomFileRepository _romRepository;

    public GetRomDetailsQueryHandler(IRomFileRepository romRepository)
    {
        _romRepository = romRepository;
    }

    public async Task<Result<RomDetailsDto>> Handle(GetRomDetailsQuery request, CancellationToken ct)
    {
        // In a real implementation, you'd have a method to get ROM by ID
        // For now, we'll return a mock result since we don't have individual ROM retrieval
        var roms = await _romRepository.GetAllAsync(ct).ConfigureAwait(false);
        var rom = roms.FirstOrDefault(r => (Guid)r.Id == request.RomFileId.Value);

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
