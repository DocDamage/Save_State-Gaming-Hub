using System.Text.Json;
using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Commands.Handlers;

/// <summary>
/// Handler for exporting ROM validation results.
/// </summary>
public sealed class ExportValidationResultsCommandHandler
    : IRequestHandler<ExportValidationResultsCommand, Result<string>>
{
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ITimeProvider _timeProvider;

    public ExportValidationResultsCommandHandler(
        IRomValidationReportRepository reportRepository,
        ITimeProvider timeProvider)
    {
        _reportRepository = reportRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<string>> Handle(
        ExportValidationResultsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<RomValidationReport> reports;

            if (request.PlatformId.HasValue)
            {
                reports = await _reportRepository.GetByPlatformIdAsync(
                    request.PlatformId.Value,
                    cancellationToken);
            }
            else
            {
                reports = await _reportRepository.GetAllAsync(cancellationToken);
            }

            if (request.IncludeStatuses?.Count > 0)
            {
                reports = reports.Where(r => request.IncludeStatuses.Contains(r.Status));
            }

            var reportList = reports.ToList();
            var generatedAt = _timeProvider.UtcNow;

            string content;
            switch (request.Format)
            {
                case ValidationExportFormat.Csv:
                    content = ExportToCsv(reportList, request);
                    break;
                case ValidationExportFormat.Html:
                    content = ExportToHtml(reportList, request, generatedAt);
                    break;
                case ValidationExportFormat.Markdown:
                    content = ExportToMarkdown(reportList, request, generatedAt);
                    break;
                case ValidationExportFormat.Dat:
                    content = ExportToDat(reportList, request);
                    break;
                case ValidationExportFormat.Json:
                default:
                    content = JsonSerializer.Serialize(reportList, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    break;
            }

            var extension = request.Format.ToString().ToLowerInvariant();
            var filePath = Path.ChangeExtension(request.OutputPath, $".{extension}");

            await File.WriteAllTextAsync(filePath, content, cancellationToken);

            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(
                $"Export failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private string ExportToCsv(List<RomValidationReport> reports, ExportValidationResultsCommand request)
    {
        var lines = new List<string>
        {
            "RomFileId,Status,ValidatedAt,SuggestedName"
        };

        foreach (var report in reports)
        {
            lines.Add($"{report.RomFileId},{report.Status},{report.ValidatedAt:yyyy-MM-dd HH:mm:ss},{report.SuggestedName}");
        }

        return string.Join("\n", lines);
    }

    private string ExportToHtml(
        List<RomValidationReport> reports,
        ExportValidationResultsCommand request,
        DateTime generatedAt)
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><title>ROM Validation Report</title></head><body>");
        html.AppendLine("<h1>ROM Validation Report</h1>");
        html.AppendLine($"<p>Generated: {generatedAt:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine($"<p>Total Reports: {reports.Count}</p>");
        html.AppendLine("<table border='1'><tr><th>ROM File</th><th>Status</th><th>Validated</th></tr>");
        
        foreach (var report in reports)
        {
            html.AppendLine($"<tr><td>{report.RomFileId}</td><td>{report.Status}</td><td>{report.ValidatedAt:yyyy-MM-dd HH:mm:ss}</td></tr>");
        }
        
        html.AppendLine("</table></body></html>");
        return html.ToString();
    }

    private string ExportToMarkdown(
        List<RomValidationReport> reports,
        ExportValidationResultsCommand request,
        DateTime generatedAt)
    {
        var lines = new List<string>
        {
            "# ROM Validation Report",
            "",
            $"**Generated:** {generatedAt:yyyy-MM-dd HH:mm:ss}",
            $"**Total Reports:** {reports.Count}",
            ""
        };

        foreach (var report in reports)
        {
            lines.Add($"## {report.RomFileId}");
            lines.Add($"- **Status:** {report.Status}");
            lines.Add($"- **Validated:** {report.ValidatedAt:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(report.SuggestedName))
            {
                lines.Add($"- **Suggested Name:** {report.SuggestedName}");
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    private string ExportToDat(List<RomValidationReport> reports, ExportValidationResultsCommand request)
    {
        // Simplified DAT format output
        var lines = new List<string>
        {
            "clrmamepro ()",
            ""
        };

        foreach (var report in reports.Where(r => r.HashInfo != null))
        {
            var hashInfo = report.HashInfo;
            lines.Add("game (");
            lines.Add($"    name \"{report.SuggestedName ?? "Unknown\""}");
            lines.Add("    rom (");
            if (!string.IsNullOrEmpty(hashInfo?.Crc32))
                lines.Add($"        crc {hashInfo.Crc32}");
            if (!string.IsNullOrEmpty(hashInfo?.Md5))
                lines.Add($"        md5 {hashInfo.Md5}");
            if (!string.IsNullOrEmpty(hashInfo?.Sha1))
                lines.Add($"        sha1 {hashInfo.Sha1}");
            lines.Add("    )");
            lines.Add(")");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }
}
