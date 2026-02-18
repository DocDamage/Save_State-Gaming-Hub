// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Repository for launch profile persistence using Entity Framework.
/// </summary>
public sealed class LaunchProfileRepository : ILaunchProfileRepository
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<LaunchProfileRepository> _logger;

    public LaunchProfileRepository(SaveStateDbContext dbContext, ILogger<LaunchProfileRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LaunchProfile>> GetProfilesAsync(Guid? gameId = null, CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.LaunchProfiles.AsNoTracking();

            if (gameId.HasValue)
            {
                query = query.Where(p => p.GameId == gameId || p.GameId == null);
            }

            var profiles = await query
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get launch profiles");
            return new List<LaunchProfile>();
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _dbContext.LaunchProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == profileId && p.IsActive, ct);

            if (profile == null)
            {
                return Result.Failure<LaunchProfile>($"Profile {profileId} not found", ErrorType.NotFound);
            }

            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get launch profile {ProfileId}", profileId);
            return Result.Failure<LaunchProfile>($"Database error: {ex.Message}", ErrorType.Database);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchProfile>> GetDefaultProfileAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            // First try to get game-specific default
            var profile = await _dbContext.LaunchProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.IsDefault && p.IsActive, ct);

            // Fall back to global default
            profile ??= await _dbContext.LaunchProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == null && p.IsDefault && p.IsActive, ct);

            if (profile == null)
            {
                return Result.Failure<LaunchProfile>($"No default profile found for game {gameId}", ErrorType.NotFound);
            }

            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default profile for game {GameId}", gameId);
            return Result.Failure<LaunchProfile>($"Database error: {ex.Message}", ErrorType.Database);
        }
    }

    /// <inheritdoc />
    public async Task SaveProfileAsync(LaunchProfile profile, CancellationToken ct = default)
    {
        try
        {
            var existing = await _dbContext.LaunchProfiles
                .FirstOrDefaultAsync(p => p.Id == profile.Id, ct);

            if (existing != null)
            {
                // Update existing
                _dbContext.Entry(existing).CurrentValues.SetValues(profile);
                _logger.LogInformation("Updated launch profile {ProfileId}", profile.Id);
            }
            else
            {
                // Add new
                await _dbContext.LaunchProfiles.AddAsync(profile, ct);
                _logger.LogInformation("Created launch profile {ProfileId}", profile.Id);
            }

            await _dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save launch profile {ProfileId}", profile.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _dbContext.LaunchProfiles
                .FirstOrDefaultAsync(p => p.Id == profileId, ct);

            if (profile != null)
            {
                profile.IsActive = false;
                profile.ModifiedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted launch profile {ProfileId}", profileId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete launch profile {ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetDefaultProfileAsync(Guid gameId, Guid? profileId, CancellationToken ct = default)
    {
        try
        {
            // Clear existing default for this game
            var existingDefaults = await _dbContext.LaunchProfiles
                .Where(p => p.GameId == gameId && p.IsDefault)
                .ToListAsync(ct);

            foreach (var profile in existingDefaults)
            {
                profile.IsDefault = false;
            }

            // Set new default
            if (profileId.HasValue)
            {
                var newDefault = await _dbContext.LaunchProfiles
                    .FirstOrDefaultAsync(p => p.Id == profileId.Value, ct);

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    newDefault.GameId = gameId;
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Set default profile for game {GameId} to {ProfileId}", gameId, profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default profile for game {GameId}", gameId);
            throw;
        }
    }
}
