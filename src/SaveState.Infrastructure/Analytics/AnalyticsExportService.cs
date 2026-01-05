using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Analytics;

public class AnalyticsExportService : IAnalyticsExportService
{
    private readonly ILogger<AnalyticsExportService> _logger;

    public AnalyticsExportService(ILogger<AnalyticsExportService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<string>> ExportToCsvAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default)
    {
        try
        {
            var csv = new StringBuilder();

            // Summary section
            csv.AppendLine("=== ANALYTICS SUMMARY ===");
            csv.AppendLine($"Generated At,{data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Total Play Time,{data.TotalPlayTime}");
            csv.AppendLine($"Total Games,{data.TotalGames}");
            csv.AppendLine($"Total Sessions,{data.TotalSessions}");
            csv.AppendLine($"Average Session Length,{data.AverageSessionLength}");
            csv.AppendLine();

            // Sessions
            csv.AppendLine("=== GAME SESSIONS ===");
            csv.AppendLine("Game Title,Started At,Duration,Platform");
            foreach (var session in data.Sessions)
            {
                csv.AppendLine($"\"{session.GameTitle}\",{session.StartedAt:yyyy-MM-dd HH:mm:ss},{session.Duration},{session.Platform ?? "Unknown"}");
            }
            csv.AppendLine();

            // Genres
            csv.AppendLine("=== GENRE BREAKDOWN ===");
            csv.AppendLine("Genre,Hours,Percentage");
            foreach (var genre in data.Genres)
            {
                csv.AppendLine($"{genre.Genre},{genre.Hours:F2},{genre.Percentage:F1}%");
            }
            csv.AppendLine();

            // Streaks
            csv.AppendLine("=== PLAY STREAKS ===");
            csv.AppendLine("Start Date,End Date,Days Count,Total Sessions");
            foreach (var streak in data.Streaks)
            {
                csv.AppendLine($"{streak.StartDate:yyyy-MM-dd},{streak.EndDate:yyyy-MM-dd},{streak.DaysCount},{streak.TotalSessions}");
            }
            csv.AppendLine();

            // Predictions
            csv.AppendLine("=== AI PREDICTIONS ===");
            csv.AppendLine("Game Name,Estimated Time Remaining,Confidence Score");
            foreach (var prediction in data.Predictions)
            {
                csv.AppendLine($"\"{prediction.GameName}\",{prediction.EstimatedTimeRemaining},{prediction.ConfidenceScore:F2}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), ct);
            _logger.LogInformation("Analytics exported to CSV: {FilePath}", filePath);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analytics to CSV");
            return Result<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportToJsonAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, json, ct);
            _logger.LogInformation("Analytics exported to JSON: {FilePath}", filePath);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analytics to JSON");
            return Result<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateHtmlReportAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default)
    {
        try
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>SaveState Analytics Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; background: #0a0e27; color: #e0e0e0; }");
            html.AppendLine("        h1 { color: #00d4ff; border-bottom: 3px solid #00d4ff; padding-bottom: 10px; }");
            html.AppendLine("        h2 { color: #00d4ff; margin-top: 30px; }");
            html.AppendLine("        .summary { background: #1a1f3a; padding: 20px; border-radius: 8px; margin-bottom: 30px; }");
            html.AppendLine("        .summary-item { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #2a2f4a; }");
            html.AppendLine("        .summary-item:last-child { border-bottom: none; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; background: #1a1f3a; }");
            html.AppendLine("        th { background: #00d4ff; color: #0a0e27; padding: 12px; text-align: left; }");
            html.AppendLine("        td { padding: 10px; border-bottom: 1px solid #2a2f4a; }");
            html.AppendLine("        tr:hover { background: #2a2f4a; }");
            html.AppendLine("        .footer { margin-top: 50px; text-align: center; color: #666; font-size: 0.9em; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>🎮 SaveState Analytics Report</h1>");
            html.AppendLine($"    <p>Generated: {data.GeneratedAt:MMMM dd, yyyy 'at' HH:mm:ss}</p>");

            // Summary
            html.AppendLine("    <div class=\"summary\">");
            html.AppendLine("        <h2>📊 Summary</h2>");
            html.AppendLine($"        <div class=\"summary-item\"><strong>Total Play Time:</strong><span>{data.TotalPlayTime}</span></div>");
            html.AppendLine($"        <div class=\"summary-item\"><strong>Total Games:</strong><span>{data.TotalGames}</span></div>");
            html.AppendLine($"        <div class=\"summary-item\"><strong>Total Sessions:</strong><span>{data.TotalSessions}</span></div>");
            html.AppendLine($"        <div class=\"summary-item\"><strong>Average Session:</strong><span>{data.AverageSessionLength}</span></div>");
            html.AppendLine("    </div>");

            // Genre Breakdown
            if (data.Genres.Any())
            {
                html.AppendLine("    <h2>🎯 Genre Breakdown</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <tr><th>Genre</th><th>Hours</th><th>Percentage</th></tr>");
                foreach (var genre in data.Genres.OrderByDescending(g => g.Hours))
                {
                    html.AppendLine($"        <tr><td>{genre.Genre}</td><td>{genre.Hours:F2}h</td><td>{genre.Percentage:F1}%</td></tr>");
                }
                html.AppendLine("    </table>");
            }

            // Play Streaks
            if (data.Streaks.Any())
            {
                html.AppendLine("    <h2>🔥 Play Streaks</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <tr><th>Period</th><th>Duration</th><th>Sessions</th></tr>");
                foreach (var streak in data.Streaks.OrderByDescending(s => s.DaysCount))
                {
                    html.AppendLine($"        <tr><td>{streak.StartDate:MMM d} - {streak.EndDate:MMM d, yyyy}</td><td>{streak.DaysCount} days</td><td>{streak.TotalSessions}</td></tr>");
                }
                html.AppendLine("    </table>");
            }

            // AI Predictions
            if (data.Predictions.Any())
            {
                html.AppendLine("    <h2>🤖 AI Predictions</h2>");
                html.AppendLine("    <table>");
                html.AppendLine("        <tr><th>Game</th><th>Time to Complete</th><th>Confidence</th></tr>");
                foreach (var prediction in data.Predictions)
                {
                    html.AppendLine($"        <tr><td>{prediction.GameName}</td><td>{prediction.EstimatedTimeRemaining}</td><td>{prediction.ConfidenceScore:P0}</td></tr>");
                }
                html.AppendLine("    </table>");
            }

            html.AppendLine("    <div class=\"footer\">");
            html.AppendLine("        <p>Generated by SaveState Reborn Analytics Engine</p>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            await File.WriteAllTextAsync(filePath, html.ToString(), ct);
            _logger.LogInformation("HTML report generated: {FilePath}", filePath);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report");
            return Result<string>.Failure($"Report generation failed: {ex.Message}");
        }
    }
}
