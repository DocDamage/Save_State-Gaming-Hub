using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Application.Performance.Queries;
using SaveState.Core.Common;
using SaveState.Core.Common.Configuration;
using SaveState.Infrastructure.Persistence;
using System.Data.Common;

namespace SaveState.Infrastructure.Performance;

public sealed class GetDatabaseStatisticsQueryHandler : IRequestHandler<GetDatabaseStatisticsQuery, Result<DatabaseStatistics>>
{
    private readonly SaveStateDbContext _dbContext;
    private readonly DatabaseOptions _options;
    private readonly ILogger<GetDatabaseStatisticsQueryHandler> _logger;

    public GetDatabaseStatisticsQueryHandler(
        SaveStateDbContext dbContext,
        IOptions<DatabaseOptions> options,
        ILogger<GetDatabaseStatisticsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<DatabaseStatistics>> Handle(GetDatabaseStatisticsQuery request, CancellationToken ct)
    {
        try
        {
            var totalGames = await _dbContext.Games.CountAsync(ct);
            var totalSessions = await _dbContext.GameSessions.CountAsync(ct);

            var connectionString = _options.ConnectionString;
            var size = GetDatabaseSize(connectionString);

            // In a real app, we might track compaction date in a Settings table
            // For now, we'll return a placeholder or use the file's last write time
            var lastCompacted = DateTime.Now.AddDays(-2); // Placeholder

            var stats = new DatabaseStatistics(
                "🟢 Healthy",
                size,
                totalGames,
                totalSessions,
                lastCompacted);

            return Result.Success<DatabaseStatistics>(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database statistics");
            return Result.Failure<DatabaseStatistics>($"Failed to get database statistics: {ex.Message}");
        }
    }

    private string GetDatabaseSize(string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.TryGetValue("Data Source", out var dataSourceObj) && dataSourceObj is string path)
            {
                if (System.IO.File.Exists(path))
                {
                    var fileInfo = new System.IO.FileInfo(path);
                    return FormatSize(fileInfo.Length);
                }
            }
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine database file size");
            return "Unknown";
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:F2} {units[unitIndex]}";
    }
}

