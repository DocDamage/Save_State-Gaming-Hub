using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Services;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for AI coaching and training assistance across games.
/// </summary>
public class CoachingCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the coaching-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Coaching command group
        var coachCommand = new Command("coach", "AI coaching and training assistance");

        // Analyze replay
        var analyzeCommand = new Command("analyze", "Analyze a game replay for feedback");
        var replayPathArg = new Argument<string>("replayPath") { Description = "Path to the replay file" };
        analyzeCommand.AddArgument(replayPathArg);
        analyzeCommand.SetHandler(async (string replayPath) =>
        {
            if (!System.IO.File.Exists(replayPath))
            {
                AnsiConsole.MarkupLine($"[red]Replay file not found: {replayPath}[/]");
                return;
            }

            var coachService = Host.Services.GetService<IMugenCoachService>();
            if (coachService == null)
            {
                AnsiConsole.MarkupLine("[red]Coaching service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Analyzing replay...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await coachService.AnalyzeReplayAsync(replayPath).ConfigureAwait(false);
                    if (!result.IsSuccess || result.Value is null)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                        return;
                    }

                    var feedback = result.Value;
                    if (!feedback.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No specific feedback generated for this replay.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine("[blue]Replay Analysis Results[/]");
                    AnsiConsole.WriteLine();

                    foreach (var item in feedback)
                    {
                        // Determine feedback type based on content
                        var icon = item.ToLower() switch
                        {
                            var s when s.Contains("improve") || s.Contains("try") => "💡",
                            var s when s.Contains("good") || s.Contains("excellent") => "✅",
                            var s when s.Contains("avoid") || s.Contains("don't") => "⚠️",
                            _ => "📝"
                        };

                        AnsiConsole.MarkupLine($"  {icon} {item}");
                    }
                });
        }, replayPathArg);

        // Counter-picks
        var counterCommand = new Command("counter", "Get counter-pick recommendations for an opponent");
        var opponentIdArg = new Argument<string>("opponentId") { Description = "Opponent character ID (GUID)" };
        counterCommand.AddArgument(opponentIdArg);
        counterCommand.SetHandler(async (string opponentIdStr) =>
        {
            if (!Guid.TryParse(opponentIdStr, out var opponentId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid character ID: {opponentIdStr}[/]");
                return;
            }

            var coachService = Host.Services.GetService<IMugenCoachService>();
            if (coachService == null)
            {
                AnsiConsole.MarkupLine("[red]Coaching service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Finding counter-picks...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await coachService.GetCounterPicksAsync(opponentId).ConfigureAwait(false);
                    if (!result.IsSuccess || result.Value is null)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                        return;
                    }

                    var counterPicks = result.Value;
                    if (!counterPicks.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No counter-pick recommendations available.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine("[green]Recommended Counter-Picks:[/]");
                    AnsiConsole.WriteLine();

                    var rank = 1;
                    foreach (var pickId in counterPicks.Take(5))
                    {
                        var rankLabel = rank switch
                        {
                            1 => "🥇",
                            2 => "🥈",
                            3 => "🥉",
                            _ => $"  #{rank}"
                        };
                        AnsiConsole.MarkupLine($"  {rankLabel} Character ID: [dim]{pickId}[/]");
                        rank++;
                    }
                });
        }, opponentIdArg);

        // Training tips
        var tipsCommand = new Command("tips", "Get general training tips");
        var focusOption = new Option<string?>("--focus") { Description = "Focus area (combos, defense, spacing, execution)" };
        tipsCommand.AddOption(focusOption);
        tipsCommand.SetHandler((string? focus) =>
        {
            AnsiConsole.MarkupLine("[blue]🎮 Training Tips[/]");
            AnsiConsole.WriteLine();

            var tips = GetTrainingTips(focus);

            foreach (var tip in tips)
            {
                AnsiConsole.MarkupLine($"  💡 {tip}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Use 'coach analyze <replay>' for personalized feedback.[/]");
        }, focusOption);

        // Warm-up routine
        var warmupCommand = new Command("warmup", "Get a pre-session warm-up routine");
        var durationOption = new Option<int>("--minutes") { DefaultValueFactory = _ => 10, Description = "Warm-up duration in minutes" };
        warmupCommand.AddOption(durationOption);
        warmupCommand.SetHandler((int minutes) =>
        {
            AnsiConsole.MarkupLine("[blue]🔥 Pre-Session Warm-Up Routine[/]");
            AnsiConsole.MarkupLine($"[dim]Duration: ~{minutes} minutes[/]");
            AnsiConsole.WriteLine();

            var routine = GetWarmupRoutine(minutes);

            var table = new Table();
            table.AddColumn("Step");
            table.AddColumn("Exercise");
            table.AddColumn("Duration");
            table.AddColumn("Focus");

            var step = 1;
            foreach (var (exercise, duration, focus) in routine)
            {
                table.AddRow(
                    step.ToString(),
                    exercise,
                    duration,
                    focus);
                step++;
            }

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Remember: Consistency beats intensity. Train smart![/]");
        }, durationOption);

        // Progress report
        var progressCommand = new Command("progress", "View your training progress summary");
        progressCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[blue]📊 Training Progress[/]");
            AnsiConsole.WriteLine();

            // This would normally pull from actual training data
            var panel = new Panel(new Markup(
                "[bold]Sessions This Week:[/] 5\n" +
                "[bold]Total Training Time:[/] 3h 45m\n" +
                "[bold]Focus Areas Practiced:[/]\n" +
                "  • Combos: [green]██████████[/] 40%\n" +
                "  • Defense: [yellow]██████[/] 25%\n" +
                "  • Spacing: [blue]████████[/] 35%\n" +
                "[bold]Streak:[/] 🔥 5 days"))
            {
                Header = new PanelHeader("Weekly Summary"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Keep up the great work![/]");
        });

        // Add all subcommands
        coachCommand.AddCommand(analyzeCommand);
        coachCommand.AddCommand(counterCommand);
        coachCommand.AddCommand(tipsCommand);
        coachCommand.AddCommand(warmupCommand);
        coachCommand.AddCommand(progressCommand);

        // Register the main command
        rootCommand.AddCommandChecked(coachCommand);
    }

    private static IReadOnlyList<string> GetTrainingTips(string? focus)
    {
        var allTips = new Dictionary<string, List<string>>
        {
            ["general"] = new()
            {
                "Practice in short, focused sessions (15-30 minutes)",
                "Record and review your matches to identify patterns",
                "Focus on one skill at a time rather than everything at once",
                "Take breaks to prevent mental fatigue and frustration",
                "Watch high-level play to understand advanced strategies"
            },
            ["combos"] = new()
            {
                "Start with BnB (bread and butter) combos before advanced ones",
                "Practice combo starters from different situations",
                "Learn to hit-confirm before committing to full combos",
                "Use training mode's recording feature to practice against setups",
                "Break down long combos into smaller segments"
            },
            ["defense"] = new()
            {
                "Learn to block mix-ups before trying to punish them",
                "Practice your fastest punish options",
                "Understand frame data to know when it's your turn",
                "Use training mode's block settings to practice various scenarios",
                "Don't be afraid to just block and observe"
            },
            ["spacing"] = new()
            {
                "Know your character's optimal range",
                "Practice movement options and their recovery",
                "Learn to bait and whiff punish common moves",
                "Control the pace of the match with footsies",
                "Use projectiles and long-range moves to control space"
            },
            ["execution"] = new()
            {
                "Slow down inputs before speeding up",
                "Use training mode's input display to check accuracy",
                "Practice difficult inputs on both sides",
                "Warm up execution before ranked matches",
                "Focus on clean inputs rather than fast inputs"
            }
        };

        var category = focus?.ToLower() ?? "general";
        if (!allTips.ContainsKey(category))
            category = "general";

        return allTips[category];
    }

    private static IReadOnlyList<(string Exercise, string Duration, string Focus)> GetWarmupRoutine(int totalMinutes)
    {
        var routine = new List<(string, string, string)>
        {
            ("Basic movement practice", "1-2 min", "Mobility"),
            ("Simple combo repetition", "2-3 min", "Muscle memory"),
            ("Anti-air practice", "1-2 min", "Reactions"),
            ("Block training", "1-2 min", "Defense"),
            ("Punish practice", "2-3 min", "Offense")
        };

        // Scale routine based on available time
        if (totalMinutes >= 15)
        {
            routine.Add(("Mix-up practice", "2-3 min", "Offense"));
            routine.Add(("Situational training", "2-3 min", "Adaptation"));
        }

        if (totalMinutes >= 20)
        {
            routine.Add(("Advanced combo practice", "3-4 min", "Execution"));
        }

        return routine;
    }
}

