using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.GameLibrary.Services;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for performance monitoring and profiling.
/// </summary>
public class PerformanceCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the performance-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Performance command group
        var perfCommand = new Command("performance", "Performance monitoring and profiling");
        perfCommand.AddAlias("perf");

        // Start profiling subcommand
        var startCommand = new Command("start", "Start performance profiling for a game");
        var gameIdArgument = new Argument<string>("gameId") { Description = "Game ID (GUID) to profile" };
        startCommand.AddArgument(gameIdArgument);
        startCommand.SetHandler(async (string gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            var profiler = Host.Services.GetService<IPerformanceProfiler>();
            if (profiler == null)
            {
                AnsiConsole.MarkupLine("[red]Performance profiler not available.[/]");
                return;
            }

            var result = await profiler.StartProfilingAsync(gameId).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine("[green]Performance profiling started![/]");
            AnsiConsole.MarkupLine("[dim]Use 'performance stop' to end profiling and generate a report.[/]");
        }, gameIdArgument);

        // Stop profiling subcommand
        var stopCommand = new Command("stop", "Stop performance profiling and generate report");
        stopCommand.SetHandler(async () =>
        {
            var profiler = Host.Services.GetService<IPerformanceProfiler>();
            if (profiler == null)
            {
                AnsiConsole.MarkupLine("[red]Performance profiler not available.[/]");
                return;
            }

            if (!profiler.IsProfiling)
            {
                AnsiConsole.MarkupLine("[yellow]No profiling session is active.[/]");
                return;
            }

            var stopResult = await profiler.StopProfilingAsync().ConfigureAwait(false);
            if (!stopResult.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error stopping profiler: {stopResult.Error}[/]");
                return;
            }

            var reportResult = await profiler.GenerateReportAsync().ConfigureAwait(false);
            if (!reportResult.IsSuccess || reportResult.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error generating report: {reportResult.Error ?? "Unknown error"}[/]");
                return;
            }

            var report = reportResult.Value;

            AnsiConsole.MarkupLine("[green]Performance profiling stopped![/]");
            AnsiConsole.WriteLine();

            // Display summary
            var panel = new Panel(new Markup(
                $"[bold]Duration:[/] {report.Duration:hh\\:mm\\:ss}\n" +
                $"[bold]Average FPS:[/] {report.AverageMetrics.Fps:F1}\n" +
                $"[bold]Avg CPU:[/] {report.AverageMetrics.CpuUsagePercent:F1}%\n" +
                $"[bold]Avg GPU:[/] {report.AverageMetrics.GpuUsagePercent:F1}%\n" +
                $"[bold]Avg Memory:[/] {report.AverageMetrics.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0):F2} GB"))
            {
                Header = new PanelHeader("[blue]Performance Summary[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            // Display issues if any
            if (report.Issues.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Issues Detected:[/]");
                var issueTable = new Table();
                issueTable.AddColumn("Issue");
                issueTable.AddColumn("Severity");
                issueTable.AddColumn("Description");

                foreach (var issue in report.Issues)
                {
                    var severityColor = issue.Severity switch
                    {
                        PerformanceSeverity.High => "red",
                        PerformanceSeverity.Medium => "yellow",
                        _ => "dim"
                    };
                    issueTable.AddRow(
                        issue.IssueType,
                        $"[{severityColor}]{issue.Severity}[/]",
                        issue.Description);
                }
                AnsiConsole.Write(issueTable);
            }

            // Display recommendations
            if (report.Recommendations.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]Recommendations:[/]");
                foreach (var rec in report.Recommendations.Take(5))
                {
                    AnsiConsole.MarkupLine($"  • [bold]{rec.Title}[/]: {rec.Description}");
                }
            }
        });

        // Current metrics subcommand
        var metricsCommand = new Command("metrics", "Show current performance metrics");
        metricsCommand.SetHandler(async () =>
        {
            var profiler = Host.Services.GetService<IPerformanceProfiler>();
            if (profiler == null)
            {
                AnsiConsole.MarkupLine("[red]Performance profiler not available.[/]");
                return;
            }

            if (!profiler.IsProfiling)
            {
                AnsiConsole.MarkupLine("[yellow]No profiling session is active. Start profiling first with 'performance start <gameId>'.[/]");
                return;
            }

            var result = await profiler.GetCurrentMetricsAsync().ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                return;
            }

            var metrics = result.Value;

            var table = new Table();
            table.AddColumn("Metric");
            table.AddColumn("Value");
            table.AddColumn("Status");

            // FPS
            var fpsStatus = metrics.Fps >= 60 ? "[green]Good[/]" : metrics.Fps >= 30 ? "[yellow]OK[/]" : "[red]Low[/]";
            table.AddRow("FPS", $"{metrics.Fps:F1}", fpsStatus);

            // Frame Time
            table.AddRow("Frame Time", $"{metrics.FrameTimeMs:F2} ms", "");

            // CPU
            var cpuStatus = metrics.CpuUsagePercent < 80 ? "[green]Good[/]" : metrics.CpuUsagePercent < 95 ? "[yellow]High[/]" : "[red]Critical[/]";
            table.AddRow("CPU Usage", $"{metrics.CpuUsagePercent:F1}%", cpuStatus);

            // GPU
            var gpuStatus = metrics.GpuUsagePercent < 80 ? "[green]Good[/]" : metrics.GpuUsagePercent < 95 ? "[yellow]High[/]" : "[red]Critical[/]";
            table.AddRow("GPU Usage", $"{metrics.GpuUsagePercent:F1}%", gpuStatus);

            // Memory
            var memoryGb = metrics.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0);
            table.AddRow("Memory Usage", $"{memoryGb:F2} GB", "");

            // GPU Memory
            var gpuMemoryGb = metrics.GpuMemoryBytes / (1024.0 * 1024.0 * 1024.0);
            table.AddRow("GPU Memory", $"{gpuMemoryGb:F2} GB", "");

            // Network
            if (metrics.NetworkLatencyMs > 0)
            {
                var netStatus = metrics.NetworkLatencyMs < 50 ? "[green]Good[/]" : metrics.NetworkLatencyMs < 100 ? "[yellow]OK[/]" : "[red]High[/]";
                table.AddRow("Network Latency", $"{metrics.NetworkLatencyMs:F1} ms", netStatus);
            }

            AnsiConsole.Write(table);
        });

        // Bottleneck analysis subcommand
        var bottlenecksCommand = new Command("bottlenecks", "Analyze performance bottlenecks");
        bottlenecksCommand.SetHandler(async () =>
        {
            var profiler = Host.Services.GetService<IPerformanceProfiler>();
            if (profiler == null)
            {
                AnsiConsole.MarkupLine("[red]Performance profiler not available.[/]");
                return;
            }

            var result = await profiler.AnalyzeBottlenecksAsync().ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                return;
            }

            var bottlenecks = result.Value;
            if (!bottlenecks.Any())
            {
                AnsiConsole.MarkupLine("[green]No significant bottlenecks detected![/]");
                return;
            }

            AnsiConsole.MarkupLine("[yellow]Bottlenecks Detected:[/]");
            AnsiConsole.WriteLine();

            foreach (var bottleneck in bottlenecks)
            {
                var severityColor = bottleneck.Severity switch
                {
                    BottleneckSeverity.Severe => "red",
                    BottleneckSeverity.Moderate => "yellow",
                    _ => "dim"
                };

                var panel = new Panel(new Markup(
                    $"[bold]Description:[/] {bottleneck.Description}\n" +
                    $"[bold]Impact:[/] {bottleneck.ImpactPercent}%\n" +
                    $"[bold]Solutions:[/]\n" +
                    string.Join("\n", bottleneck.Solutions.Select(s => $"  • {s}"))))
                {
                    Header = new PanelHeader($"[{severityColor}]{bottleneck.Component} - {bottleneck.Severity}[/]"),
                    Border = BoxBorder.Rounded
                };
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();
            }
        });

        // Add subcommands
        perfCommand.AddCommand(startCommand);
        perfCommand.AddCommand(stopCommand);
        perfCommand.AddCommand(metricsCommand);
        perfCommand.AddCommand(bottlenecksCommand);

        // Register the main command
        rootCommand.AddCommandChecked(perfCommand);
    }
}

