using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SaveState.Docs.Sync;

public class Program
{
    public static int Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Docs Sync").Color(Color.Green));

        var projectRoot = FindProjectRoot();
        if (string.IsNullOrEmpty(projectRoot))
        {
            AnsiConsole.MarkupLine("[red]Error: Could not find project root (looking for .git or docs folder).[/]");
            return 1;
        }

        var configPath = Path.Combine(projectRoot, "docs", "sync-config.yaml");
        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Config file not found at {configPath}[/]");
            return 1;
        }

        try
        {
            var config = LoadConfig(configPath);
            var sourcePath = Path.Combine(projectRoot, config.SourceFile);

            if (!File.Exists(sourcePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: Source file not found at {sourcePath}[/]");
                return 1;
            }

            var sourceContent = File.ReadAllText(sourcePath);
            var metrics = ExtractMetrics(sourceContent, config.Metrics);

            AnsiConsole.MarkupLine($"[bold]Found {metrics.Count} metrics in {config.SourceFile}:[/]");
            foreach (var m in metrics)
            {
                AnsiConsole.MarkupLine($"  - [blue]{m.Key}[/]: {m.Value}");
            }

            // Check narratives
            // In a real run, we would compare against previous values.
            // For now, we'll just validate constraints.
            if (!ValidateMetrics(metrics, config.Metrics))
            {
                AnsiConsole.MarkupLine("[red]Validation failed. Aborting sync.[/]");
                return 1;
            }

            bool changesMade = false;
            foreach (var target in config.Targets)
            {
                var targetPath = Path.Combine(projectRoot, target.File);
                if (!File.Exists(targetPath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Target file {target.File} not found. Skipping.[/]");
                    continue;
                }

                var targetContent = File.ReadAllText(targetPath);
                var originalContent = targetContent;

                foreach (var update in target.Updates)
                {
                    if (metrics.TryGetValue(update.Metric, out var value))
                    {
                        var regex = new Regex(update.Regex);
                        if (regex.IsMatch(targetContent))
                        {
                            var newValue = update.Replace.Replace("{value}", value);
                            targetContent = regex.Replace(targetContent, newValue);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]  Warning: Pattern for '{update.Metric}' not found in {target.File}[/]");
                        }
                    }
                }

                if (targetContent != originalContent)
                {
                    File.WriteAllText(targetPath, targetContent);
                    AnsiConsole.MarkupLine($"[green]Updated {target.File}[/]");
                    changesMade = true;
                }
            }

            if (!changesMade)
            {
                AnsiConsole.MarkupLine("[grey]No changes needed. All docs are in sync.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold green]Sync completed successfully![/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static string? FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "docs"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null; // Fallback
    }

    private static SyncConfig LoadConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<SyncConfig>(yaml);
    }

    private static Dictionary<string, string> ExtractMetrics(string content, List<MetricDef> metricDefs)
    {
        var results = new Dictionary<string, string>();
        foreach (var def in metricDefs)
        {
            var match = Regex.Match(content, def.Pattern);
            if (match.Success)
            {
                results[def.Name] = match.Groups["value"].Value;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Could not extract metric '{def.Name}' using pattern '{Markup.Escape(def.Pattern)}'[/]");
            }
        }
        return results;
    }

    private static bool ValidateMetrics(Dictionary<string, string> values, List<MetricDef> defs)
    {
        bool isValid = true;
        foreach (var def in defs)
        {
            if (values.TryGetValue(def.Name, out var valStr))
            {
                if (def.Type == "number" || def.Type == "percentage")
                {
                    if (double.TryParse(valStr, out double val))
                    {
                        if (def.Validation?.Min.HasValue == true && val < def.Validation.Min.Value)
                        {
                            AnsiConsole.MarkupLine($"[red]Error: Metric '{def.Name}' value {val} is below min {def.Validation.Min}[/]");
                            isValid = false;
                        }
                        if (def.Validation?.Max.HasValue == true && val > def.Validation.Max.Value)
                        {
                            AnsiConsole.MarkupLine($"[red]Error: Metric '{def.Name}' value {val} is above max {def.Validation.Max}[/]");
                            isValid = false;
                        }
                    }
                }
            }
        }
        return isValid;
    }
}

// Config Models
public class SyncConfig
{
    public string SourceFile { get; set; } = "";
    public List<MetricDef> Metrics { get; set; } = new();
    public List<SyncTarget> Targets { get; set; } = new();
    public List<NarrativeCheck> NarrativeChecks { get; set; } = new();
}

public class NarrativeCheck
{
    public string Metric { get; set; } = "";
    public double ChangeThreshold { get; set; }
    public string Message { get; set; } = "";
}

public class MetricDef
{
    public string Name { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string Type { get; set; } = "string";
    public ValidationRule? Validation { get; set; }
}

public class ValidationRule
{
    public double? Min { get; set; }
    public double? Max { get; set; }
}

public class SyncTarget
{
    public string File { get; set; } = "";
    public List<TargetUpdate> Updates { get; set; } = new();
}

public class TargetUpdate
{
    public string Metric { get; set; } = "";
    public string Regex { get; set; } = "";
    public string Replace { get; set; } = "";
}
