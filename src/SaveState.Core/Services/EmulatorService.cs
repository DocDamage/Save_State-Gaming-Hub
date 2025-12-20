using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

namespace SaveState.Core.Services;

public class EmulatorService : IEmulatorService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger _logger = Log.ForContext<EmulatorService>();

    public EmulatorService(SaveStateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Emulator>> GetAllAsync()
    {
        return await _dbContext.Emulators.ToListAsync();
    }

    public async Task<Emulator?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Emulators.FindAsync(id);
    }

    public async Task<Emulator?> GetDefaultForPlatformAsync(string platformName)
    {
        // Find emulator that supports this platform and is marked as default
        return await _dbContext.Emulators
            .Where(e => e.SupportedPlatforms.Contains(platformName) && e.IsDefault)
            .FirstOrDefaultAsync()
            ?? await _dbContext.Emulators
                .Where(e => e.SupportedPlatforms.Contains(platformName))
                .FirstOrDefaultAsync();
    }

    public async Task<Emulator> AddAsync(Emulator emulator)
    {
        emulator.Id = Guid.NewGuid();
        emulator.CreatedAt = DateTime.UtcNow;
        emulator.UpdatedAt = DateTime.UtcNow;
        _dbContext.Emulators.Add(emulator);
        await _dbContext.SaveChangesAsync();
        return emulator;
    }

    public async Task UpdateAsync(Emulator emulator)
    {
        emulator.UpdatedAt = DateTime.UtcNow;
        _dbContext.Emulators.Update(emulator);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var emulator = await _dbContext.Emulators.FindAsync(id);
        if (emulator != null)
        {
            _dbContext.Emulators.Remove(emulator);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> LaunchRomAsync(Game rom, Emulator? emulator = null)
    {
        try
        {
            // Get emulator if not specified
            emulator ??= await GetDefaultForPlatformAsync(rom.Platform?.Name ?? "");
            
            if (emulator == null || string.IsNullOrEmpty(emulator.ExecutablePath))
            {
                _logger.Warning("No emulator configured for platform: {Platform}", rom.Platform?.Name);
                return false;
            }

            if (!File.Exists(emulator.ExecutablePath))
            {
                _logger.Error("Emulator not found: {Path}", emulator.ExecutablePath);
                return false;
            }

            var romPath = rom.InstallPath ?? "";
            if (!File.Exists(romPath))
            {
                _logger.Error("ROM not found: {Path}", romPath);
                return false;
            }

            // Build arguments - replace {rom} placeholder with actual path
            var args = emulator.Arguments ?? "\"{rom}\"";
            args = args.Replace("{rom}", romPath);
            args = args.Replace("{ROM}", romPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = emulator.ExecutablePath,
                Arguments = args,
                UseShellExecute = true,
                WorkingDirectory = emulator.WorkingDirectory ?? Path.GetDirectoryName(emulator.ExecutablePath)
            };

            _logger.Information("Launching ROM: {Rom} with {Emulator}", rom.Title, emulator.Name);
            Process.Start(startInfo);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to launch ROM: {Title}", rom.Title);
            return false;
        }
    }
}
