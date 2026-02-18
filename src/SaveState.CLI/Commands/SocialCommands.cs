using System.CommandLine;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for social features, friends, and leaderboards.
/// Note: Full implementation pending service updates.
/// </summary>
public class SocialCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the social-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Social command group
        var socialCommand = new Command("social", "Social features, friends, and leaderboards");

        // Friends subgroup
        var friendsCommand = new Command("friends", "Manage friends list");

        // List friends
        var listFriendsCommand = new Command("list", "List your friends");
        listFriendsCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[yellow]No friends added yet.[/]");
            AnsiConsole.MarkupLine("[dim]Use 'social friends add' to add friends.[/]");
        });

        // Add friend
        var addFriendCommand = new Command("add", "Send a friend request");
        var userIdArg = new Argument<string>("userId") { Description = "User ID to send friend request to" };
        addFriendCommand.AddArgument(userIdArg);
        addFriendCommand.SetHandler((string userIdStr) =>
        {
            AnsiConsole.MarkupLine("[yellow]Social features require online connectivity.[/]");
            AnsiConsole.MarkupLine("[dim]Online social features will be available in a future update.[/]");
        }, userIdArg);

        // Remove friend
        var removeFriendCommand = new Command("remove", "Remove a friend");
        var friendIdArg = new Argument<string>("friendId") { Description = "Friend ID to remove" };
        removeFriendCommand.AddArgument(friendIdArg);
        removeFriendCommand.SetHandler((string friendIdStr) =>
        {
            AnsiConsole.MarkupLine("[yellow]Friend not found.[/]");
        }, friendIdArg);

        friendsCommand.AddCommand(listFriendsCommand);
        friendsCommand.AddCommand(addFriendCommand);
        friendsCommand.AddCommand(removeFriendCommand);

        // Leaderboard subgroup
        var leaderboardCommand = new Command("leaderboard", "View leaderboards");
        leaderboardCommand.AddAlias("lb");

        // Global leaderboard
        var globalLbCommand = new Command("global", "View global leaderboard");
        var limitOption = new Option<int>("--limit") { DefaultValueFactory = _ => 20, Description = "Number of entries to show" };
        globalLbCommand.AddOption(limitOption);
        globalLbCommand.SetHandler((int limit) =>
        {
            AnsiConsole.MarkupLine("[yellow]Leaderboards require online connectivity.[/]");
            AnsiConsole.MarkupLine("[dim]Connect to view global rankings.[/]");
        }, limitOption);

        // Friends leaderboard
        var friendsLbCommand = new Command("friends", "View friends leaderboard");
        friendsLbCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[yellow]No friends to show on leaderboard.[/]");
            AnsiConsole.MarkupLine("[dim]Add friends to see their rankings.[/]");
        });

        // Weekly leaderboard
        var weeklyLbCommand = new Command("weekly", "View weekly leaderboard");
        weeklyLbCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[yellow]Weekly leaderboards require online connectivity.[/]");
        });

        leaderboardCommand.AddCommand(globalLbCommand);
        leaderboardCommand.AddCommand(friendsLbCommand);
        leaderboardCommand.AddCommand(weeklyLbCommand);

        // Share achievement
        var shareCommand = new Command("share", "Share an achievement");
        var achievementArg = new Argument<string>("achievement") { Description = "Achievement name to share" };
        var descriptionOption = new Option<string?>("--description") { Description = "Custom description" };
        shareCommand.AddArgument(achievementArg);
        shareCommand.AddOption(descriptionOption);
        shareCommand.SetHandler((string achievement, string? description) =>
        {
            AnsiConsole.MarkupLine($"[yellow]Sharing '{achievement}' requires online connectivity.[/]");
            AnsiConsole.MarkupLine("[dim]Connect to share achievements with friends.[/]");
        }, achievementArg, descriptionOption);

        // Add all subgroups
        socialCommand.AddCommand(friendsCommand);
        socialCommand.AddCommand(leaderboardCommand);
        socialCommand.AddCommand(shareCommand);

        // Register the main command
        rootCommand.AddCommandChecked(socialCommand);
    }
}

