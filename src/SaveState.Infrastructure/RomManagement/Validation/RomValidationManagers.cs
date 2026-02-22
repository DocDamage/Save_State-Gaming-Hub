using System.Text.RegularExpressions;
using System.Xml.Linq;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement.Validation;

internal static class RomHashCalculationManager
{
    public static string CalculateCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & ~(crc & 1));
            }
        }

        return (~crc).ToString("X8").ToLowerInvariant();
    }

    public static bool IsPartialMatch(RomHashInfo hashInfo, DatFileEntry entry)
    {
        if (hashInfo.Crc32 != null && entry.Crc32 != null)
        {
            var hashPrefix = hashInfo.Crc32.Substring(0, Math.Min(4, hashInfo.Crc32.Length));
            var entryPrefix = entry.Crc32.Substring(0, Math.Min(4, entry.Crc32.Length));
            return hashPrefix.Equals(entryPrefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static string GetHashByType(RomHashInfo hashInfo, HashAlgorithmType type)
    {
        return type switch
        {
            HashAlgorithmType.Crc32 => hashInfo.Crc32 ?? string.Empty,
            HashAlgorithmType.Md5 => hashInfo.Md5 ?? string.Empty,
            HashAlgorithmType.Sha1 => hashInfo.Sha1 ?? string.Empty,
            HashAlgorithmType.Sha256 => hashInfo.Sha256 ?? string.Empty,
            _ => hashInfo.Sha1 ?? string.Empty
        };
    }
}

internal static class RomValidationNamingHelper
{
    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim();
    }

    public static string GetBadDumpDescription(RomValidationReport report)
    {
        if (report.MatchResult?.MatchedEntry?.DumpStatus != RomDumpStatus.Good)
        {
            return $"Identified as {report.MatchResult?.MatchedEntry?.DumpStatus} dump in DAT database";
        }

        var issue = report.Issues.FirstOrDefault(i => i.Category == IssueCategory.Database);
        return issue?.Message ?? "Unknown issue";
    }
}

internal static class RomValidationDatParser
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "XML parsing requires multiple conditional checks")]
    public static List<DatFileEntry> ParseXmlDat(string content, string sourcePath)
    {
        var entries = new List<DatFileEntry>();
        try
        {
            var doc = XDocument.Parse(content);
            var header = doc.Root?.Element("header");
            var version = header?.Element("version")?.Value ?? header?.Element("date")?.Value ?? "Unknown";

            foreach (var game in doc.Root?.Elements("game") ?? doc.Root?.Elements("machine") ?? Enumerable.Empty<XElement>())
            {
                var rom = game.Element("rom");
                if (rom != null)
                {
                    entries.Add(new DatFileEntry
                    {
                        Name = game.Attribute("name")?.Value ?? "Unknown",
                        GameTitle = game.Element("description")?.Value,
                        Region = ExtractRegion(game.Attribute("name")?.Value),
                        Crc32 = rom.Attribute("crc")?.Value?.ToLowerInvariant(),
                        Md5 = rom.Attribute("md5")?.Value?.ToLowerInvariant(),
                        Sha1 = rom.Attribute("sha1")?.Value?.ToLowerInvariant(),
                        Size = long.TryParse(rom.Attribute("size")?.Value, out var size) ? size : 0,
                        SourceDat = Path.GetFileName(sourcePath),
                        DatVersion = version,
                        IsVerified = true,
                        CloneOf = game.Attribute("cloneof")?.Value,
                        DumpStatus = RomDumpStatus.Good
                    });
                }
            }
        }
        catch
        {
            // If XML parsing fails, return an empty list.
        }

        return entries;
    }

    public static List<DatFileEntry> ParseJsonDat(string content, string sourcePath)
    {
        return new List<DatFileEntry>();
    }

    public static List<DatFileEntry> ParseCsvDat(string content, string sourcePath)
    {
        return new List<DatFileEntry>();
    }

    private static string? ExtractRegion(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var regionPatterns = new Dictionary<string, string>
        {
            [@"\(USA\)"] = "USA",
            [@"\(Europe\)"] = "EUR",
            [@"\(Japan\)"] = "JPN",
            [@"\(World\)"] = "WLD"
        };

        foreach (var pattern in regionPatterns)
        {
            if (Regex.IsMatch(name, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }

        return null;
    }
}

internal static class RomFileIntegrityAnalyzer
{
    public static RomHeaderInfo? AnalyzeRomHeader(byte[] data, string extension)
    {
        if (data.Length < 16)
        {
            return null;
        }

        var header = new RomHeaderInfo { HasHeader = false };

        if (extension == ".nes" && data[0] == 'N' && data[1] == 'E' && data[2] == 'S' && data[3] == 0x1A)
        {
            header.HasHeader = true;
            header.HeaderSize = 16;
            header.HeaderType = "iNES";
            header.IsValidHeader = true;
        }
        else if (extension == ".smc" && data.Length % 1024 == 512)
        {
            header.HasHeader = true;
            header.HeaderSize = 512;
            header.HeaderType = "SMC";
            header.IsValidHeader = true;
        }
        else if (extension == ".smd" && data.Length > 8 && data[8] == 0xAA && data[9] == 0xBB)
        {
            header.HasHeader = true;
            header.HeaderSize = 512;
            header.HeaderType = "SMD";
            header.IsValidHeader = true;
        }

        return header;
    }
}

internal static class RomValidationExportManager
{
    public static string ExportToJson(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "{" };
        lines.Add("  \"reports\": [");
        lines.AddRange(reports.Select(r => $"    {{ \"romId\": \"{r.RomFileId}\", \"status\": \"{r.Status}\" }},"));
        lines.Add("  ]");
        lines.Add("}");
        return string.Join("\n", lines);
    }

    public static string ExportToCsv(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "RomFileId,Status,ValidatedAt" };
        lines.AddRange(reports.Select(r => $"{r.RomFileId},{r.Status},{r.ValidatedAt:yyyy-MM-dd HH:mm:ss}"));
        return string.Join("\n", lines);
    }

    public static string ExportToHtml(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var html = "<!DOCTYPE html><html><head><title>ROM Validation Report</title></head><body>";
        html += "<h1>ROM Validation Report</h1><table border='1'><tr><th>ROM ID</th><th>Status</th><th>Validated At</th></tr>";
        foreach (var report in reports)
        {
            html += $"<tr><td>{report.RomFileId}</td><td>{report.Status}</td><td>{report.ValidatedAt}</td></tr>";
        }

        html += "</table></body></html>";
        return html;
    }

    public static string ExportToMarkdown(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "# ROM Validation Report", "", "| ROM ID | Status | Validated At |", "|--------|--------|-------------|" };
        lines.AddRange(reports.Select(r => $"| {r.RomFileId} | {r.Status} | {r.ValidatedAt:yyyy-MM-dd HH:mm:ss} |"));
        return string.Join("\n", lines);
    }

    public static string ExportToDat(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE datafile>\n<datafile>\n  <header>\n    <name>Exported ROM Validation</name>\n    <description>ROM Validation Export</description>\n    <version>1.0</version>\n  </header>";
        foreach (var report in reports.Where(r => r.HashInfo != null))
        {
            xml += $"\n  <game name=\"ROM_{report.RomFileId}\">\n    <rom name=\"ROM_{report.RomFileId}.rom\" size=\"0\" crc=\"{report.HashInfo?.Crc32 ?? ""}\" md5=\"{report.HashInfo?.Md5 ?? ""}\" sha1=\"{report.HashInfo?.Sha1 ?? ""}\"/>\n  </game>";
        }

        xml += "\n</datafile>";
        return xml;
    }
}
