using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;

namespace SaveState.Presentation.Services;

/// <summary>
/// In-memory registry and search engine for command palette commands.
/// </summary>
public sealed class CommandPaletteService : ICommandPaletteService
{
    private const float MinimumMatchScore = 10f;
    private readonly ILogger<CommandPaletteService> _logger;
    private readonly Dictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public CommandPaletteService(ILogger<CommandPaletteService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterCommand(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ArgumentException("Command id is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Command name is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Category))
        {
            throw new ArgumentException("Command category is required.", nameof(command));
        }

        lock (_syncRoot)
        {
            _commands[command.Id] = command;
        }

        _logger.LogDebug("Registered command palette command {CommandId}", command.Id);
    }

    /// <inheritdoc />
    public void UnregisterCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return;
        }

        var removed = false;
        lock (_syncRoot)
        {
            removed = _commands.Remove(commandId);
        }

        if (removed)
        {
            _logger.LogDebug("Unregistered command palette command {CommandId}", commandId);
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<CommandItem>>> SearchAsync(
        string query,
        CommandContext context,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        context ??= CommandContext.Default;

        var normalizedQuery = query?.Trim() ?? string.Empty;
        var snapshot = GetSnapshot();
        var maxResults = context.MaxResults > 0 ? context.MaxResults : CommandContext.Default.MaxResults;

        var results = snapshot
            .Select(command => new
            {
                Command = command,
                Score = CalculateScore(command, normalizedQuery, context)
            })
            .Where(x => string.IsNullOrWhiteSpace(normalizedQuery) || x.Score >= MinimumMatchScore)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Command.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .Select(x => new CommandItem(
                x.Command.Id,
                x.Command.Name,
                x.Command.Description,
                x.Command.Category,
                x.Score,
                x.Command.Icon,
                x.Command.Shortcut,
                x.Command.Source))
            .ToArray();

        return Task.FromResult(Result.Success<IReadOnlyList<CommandItem>>(results));
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        string commandId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return Result.Failure("Command id is required.", ErrorType.Validation);
        }

        CommandDefinition? command;
        lock (_syncRoot)
        {
            _commands.TryGetValue(commandId, out command);
        }

        if (command is null)
        {
            return Result.Failure($"Command '{commandId}' was not found.", ErrorType.NotFound);
        }

        try
        {
            var result = await command.ExecuteAsync(ct).ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Command {CommandId} executed with failure: {Error}",
                    commandId,
                    result.Error);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure($"Command '{commandId}' was cancelled.", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandId} failed unexpectedly", commandId);
            return Result.Failure($"Command '{commandId}' failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private CommandDefinition[] GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _commands.Values.ToArray();
        }
    }

    private static float CalculateScore(
        CommandDefinition command,
        string query,
        CommandContext context)
    {
        if (!IsAllowedCategory(command, context))
        {
            return 0f;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return 1f;
        }

        var score = 0f;

        score += ScoreField(query, command.Name, exact: 120f, prefix: 90f, contains: 60f);
        score += ScoreField(query, command.Description, exact: 60f, prefix: 40f, contains: 30f);
        score += ScoreField(query, command.Id, exact: 80f, prefix: 50f, contains: 35f);
        score += ScoreField(query, command.Category, exact: 25f, prefix: 20f, contains: 15f);

        foreach (var keyword in command.Keywords)
        {
            score += ScoreField(query, keyword, exact: 50f, prefix: 35f, contains: 25f);
        }

        var fuzzySource = string.Join(
            ' ',
            command.Name,
            command.Description,
            command.Category,
            command.Id,
            string.Join(' ', command.Keywords));
        score += ComputeSubsequenceRatio(query, fuzzySource) * 35f;

        return score;
    }

    private static float ScoreField(
        string query,
        string? value,
        float exact,
        float prefix,
        float contains)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0f;
        }

        if (value.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return exact;
        }

        if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return prefix;
        }

        if (value.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return contains;
        }

        return 0f;
    }

    private static bool IsAllowedCategory(CommandDefinition command, CommandContext context)
    {
        if (context.AllowedCategories.Count == 0)
        {
            return true;
        }

        return context.AllowedCategories.Any(category =>
            string.Equals(category, command.Category, StringComparison.OrdinalIgnoreCase));
    }

    private static float ComputeSubsequenceRatio(string query, string source)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(source))
        {
            return 0f;
        }

        var q = query.ToLowerInvariant();
        var s = source.ToLowerInvariant();

        var qIndex = 0;
        var matched = 0;
        for (var i = 0; i < s.Length && qIndex < q.Length; i++)
        {
            if (s[i] != q[qIndex])
            {
                continue;
            }

            matched++;
            qIndex++;
        }

        return (float)matched / q.Length;
    }
}
