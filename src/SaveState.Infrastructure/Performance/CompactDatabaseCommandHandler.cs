using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Application.Performance.Commands;
using SaveState.Core.Common;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Handler for CompactDatabaseCommand.
/// </summary>
public sealed class CompactDatabaseCommandHandler : IRequestHandler<CompactDatabaseCommand, Result>
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<CompactDatabaseCommandHandler> _logger;

    public CompactDatabaseCommandHandler(
        SaveStateDbContext dbContext,
        ILogger<CompactDatabaseCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(CompactDatabaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database compaction...");

            // SQLite VACUUM command - reclaims space from deleted records
            await _dbContext.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);

            // Optimize database - analyze and update statistics
            await _dbContext.Database.ExecuteSqlRawAsync("ANALYZE;", cancellationToken);

            _logger.LogInformation("Database compaction completed successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compact database");
            return Result.Failure($"Database compaction failed: {ex.Message}");
        }
    }
}
