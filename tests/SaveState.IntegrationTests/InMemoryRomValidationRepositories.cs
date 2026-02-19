using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.IntegrationTests;

internal sealed class InMemoryRomFileRepository : IRomFileRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, RomFile> _roms = new();
    private readonly Dictionary<Guid, Platform> _platforms = new();

    public Task<RomFile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _roms.TryGetValue(id, out var rom);
            return Task.FromResult<RomFile?>(rom);
        }
    }

    public Task<IReadOnlyList<RomFile>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<RomFile>>(_roms.Values.ToList());
        }
    }

    public Task<IReadOnlyList<RomFile>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<RomFile>>(
                _roms.Values.Where(r => r.PlatformId == platformId).ToList());
        }
    }

    public Task<IReadOnlyList<RomFile>> GetByIdsAsync(IEnumerable<Guid> romFileIds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ids = romFileIds.ToHashSet();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<RomFile>>(
                _roms.Values.Where(r => ids.Contains(r.Id)).ToList());
        }
    }

    public Task<IReadOnlyList<RomFile>> GetByPlatformIdsAsync(IEnumerable<Guid> platformIds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ids = platformIds.ToHashSet();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<RomFile>>(
                _roms.Values.Where(r => ids.Contains(r.PlatformId)).ToList());
        }
    }

    public Task<IReadOnlyList<Guid>> GetIdsByPlatformAsync(Guid platformId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(
                _roms.Values.Where(r => r.PlatformId == platformId).Select(r => r.Id).ToList());
        }
    }

    public Task<Platform?> GetPlatformAsync(Guid platformId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _platforms.TryGetValue(platformId, out var platform);
            return Task.FromResult(platform);
        }
    }

    public Task<IReadOnlyList<Platform>> GetAllPlatformsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Platform>>(_platforms.Values.ToList());
        }
    }

    public Task<IReadOnlyList<RomFile>> GetByFolderPathAsync(string folderPath, Guid platformId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var normalizedFolder = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<RomFile>>(
                _roms.Values.Where(r =>
                        r.PlatformId == platformId &&
                        r.FilePath.Value.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase))
                    .ToList());
        }
    }

    public Task<PagedResult<RomFile>> GetRomFilesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        Guid? platformId = null,
        string? folderPath = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IEnumerable<RomFile> query = _roms.Values;

            if (platformId.HasValue)
            {
                query = query.Where(r => r.PlatformId == platformId.Value);
            }

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                var normalizedFolder = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                query = query.Where(r =>
                    r.FilePath.Value.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase));
            }

            query = query.OrderBy(r => r.FilePath.Value);
            var totalCount = query.Count();
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new PagedResult<RomFile>(items, totalCount, pageNumber, pageSize));
        }
    }

    public Task<int> CountAsync(Guid? platformId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var count = platformId.HasValue
                ? _roms.Values.Count(r => r.PlatformId == platformId.Value)
                : _roms.Count;
            return Task.FromResult(count);
        }
    }

    public Task AddAsync(RomFile romFile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _roms[romFile.Id] = romFile;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(RomFile romFile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _roms[romFile.Id] = romFile;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _roms.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task AddPlatformAsync(Platform platform, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _platforms[platform.Id] = platform;
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryRomHashInfoRepository : IRomHashInfoRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, RomHashInfo> _hashes = new();

    public Task<RomHashInfo?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var hash = _hashes.Values.FirstOrDefault(h => h.RomFileId == romFileId);
            return Task.FromResult(hash);
        }
    }

    public Task<IEnumerable<RomHashInfo>> GetByHashAsync(string hash, HashAlgorithmType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var matches = type switch
            {
                HashAlgorithmType.Crc32 => _hashes.Values.Where(h => string.Equals(h.Crc32, hash, StringComparison.OrdinalIgnoreCase)),
                HashAlgorithmType.Md5 => _hashes.Values.Where(h => string.Equals(h.Md5, hash, StringComparison.OrdinalIgnoreCase)),
                HashAlgorithmType.Sha1 => _hashes.Values.Where(h => string.Equals(h.Sha1, hash, StringComparison.OrdinalIgnoreCase)),
                HashAlgorithmType.Sha256 => _hashes.Values.Where(h => string.Equals(h.Sha256, hash, StringComparison.OrdinalIgnoreCase)),
                _ => Enumerable.Empty<RomHashInfo>()
            };

            return Task.FromResult<IEnumerable<RomHashInfo>>(matches.ToList());
        }
    }

    public Task<IEnumerable<RomHashInfo>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<RomHashInfo>>(_hashes.Values.ToList());
        }
    }

    public Task AddAsync(RomHashInfo hashInfo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var existing = _hashes.Values.FirstOrDefault(h => h.RomFileId == hashInfo.RomFileId);
            if (existing is not null)
            {
                _hashes.Remove(existing.Id);
            }

            _hashes[hashInfo.Id] = hashInfo;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(RomHashInfo hashInfo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _hashes[hashInfo.Id] = hashInfo;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _hashes.Remove(id);
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryRomValidationReportRepository : IRomValidationReportRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, RomValidationReport> _reports = new();
    private readonly IRomFileRepository _romFileRepository;

    public InMemoryRomValidationReportRepository(IRomFileRepository romFileRepository)
    {
        _romFileRepository = romFileRepository;
    }

    public Task<RomValidationReport?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var report = _reports.Values
                .Where(r => r.RomFileId == romFileId)
                .OrderByDescending(r => r.ValidatedAt)
                .FirstOrDefault();
            return Task.FromResult(report);
        }
    }

    public Task<IEnumerable<RomValidationReport>> GetByStatusAsync(ValidationStatus status, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<RomValidationReport>>(
                _reports.Values.Where(r => r.Status == status).ToList());
        }
    }

    public async Task<IEnumerable<RomValidationReport>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var romIds = (await _romFileRepository.GetIdsByPlatformAsync(platformId, ct).ConfigureAwait(false)).ToHashSet();

        lock (_gate)
        {
            return _reports.Values.Where(r => romIds.Contains(r.RomFileId)).ToList();
        }
    }

    public Task<IEnumerable<RomValidationReport>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<RomValidationReport>>(_reports.Values.ToList());
        }
    }

    public Task AddAsync(RomValidationReport report, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _reports[report.Id] = report;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(RomValidationReport report, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _reports[report.Id] = report;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _reports.Remove(id);
        }

        return Task.CompletedTask;
    }
}
