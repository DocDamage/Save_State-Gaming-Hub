using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.AiCoOp.Models;
using SaveState.Core.AiCoOp.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

using GameStateSnapshot = SaveState.Core.AiCoOp.Models.GameStateSnapshot;

namespace SaveState.Infrastructure.AiCoOp.Services;

/// <summary>
/// Implementation of the AI Co-Op Companion Service using OpenAI GPT for intelligent decision making.
/// Provides contextual responses, adaptive playstyle learning, and voice interaction.
/// </summary>
public sealed class AiCoOpCompanionService : IAiCoOpCompanionService
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<AiCoOpCompanionService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<CompanionChatMessage> _chatHistory = new();
    private readonly List<PlayerBehaviorSample> _behaviorSamples = new();
    private CompanionConfiguration? _configuration;
    private bool _isVoiceEnabled;

    private readonly Dictionary<string, List<string>> _personalityPrompts = new()
    {
        ["Supportive"] = new()
        {
            "You are a supportive gaming companion. Encourage the player, offer helpful tips, and celebrate their successes.",
            "Be warm, friendly, and always ready to help. Use phrases like 'Great job!', 'You've got this!', and 'I'm here for you.'"
        },
        ["Competitive"] = new()
        {
            "You are a competitive gaming companion. Challenge the player to improve, compare scores, and push them to be their best.",
            "Be energetic and competitive. Use phrases like 'Can you beat that?', 'I bet you can't do better!', and 'Let's win this!'"
        },
        ["Humorous"] = new()
        {
            "You are a humorous gaming companion. Make jokes, keep things lighthearted, and don't take the game too seriously.",
            "Be funny and witty. Use humor to defuse tense situations. Make gaming fun above all else."
        },
        ["Tactical"] = new()
        {
            "You are a tactical gaming companion. Focus on strategy, positioning, and optimal decision making.",
            "Be analytical and precise. Offer strategic advice, call out enemy patterns, and suggest optimal approaches."
        },
        ["Silent"] = new()
        {
            "You are a silent gaming companion. Speak only when absolutely necessary - critical warnings or important discoveries.",
            "Keep communication minimal. One or two words at most. Let your actions speak louder than words."
        }
    };

    private readonly Dictionary<string, List<string>> _voiceLines = new()
    {
        ["Supportive"] = new()
        {
            "You're doing great! Keep it up!",
            "I'm here to help if you need anything.",
            "Nice move! I knew you had it in you!",
            "Don't worry, we'll get through this together.",
            "That was impressive! Well done!"
        },
        ["Competitive"] = new()
        {
            "Bet you can't beat my high score!",
            "Let's show them who's boss!",
            "Faster! We can do better than that!",
            "I'm winning this round!",
            "Challenge accepted?"
        },
        ["Humorous"] = new()
        {
            "Did you see that? Even my grandma plays better!",
            "Oops! Let's pretend that didn't happen.",
            "Is it hot in here or is it just this game?",
            "I've seen snails move faster!",
            "Achievement unlocked: Professional Button Masher!"
        },
        ["Tactical"] = new()
        {
            "Enemy flanking left. Recommend repositioning.",
            "Health critical. Suggest retreat to cover.",
            "Optimal path: North corridor, less resistance.",
            "Pattern detected: Boss charges after three attacks.",
            "Resource efficiency at 73%. Room for improvement."
        },
        ["Silent"] = new()
        {
            "Look out.",
            "Behind you.",
            "Health low.",
            "Item here.",
            "Danger."
        }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AiCoOpCompanionService"/> class.
    /// </summary>
    public AiCoOpCompanionService(
        ILlmProvider llmProvider,
        ILogger<AiCoOpCompanionService> logger,
        ITimeProvider timeProvider)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<Result> InitializeCompanionAsync(CompanionConfiguration config, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Initializing AI Co-Op Companion '{Name}' with personality {Personality} and skill level {SkillLevel}",
            config.Name, config.Personality, config.SkillLevel);

        _configuration = config;
        _isVoiceEnabled = config.VoiceEnabled;
        _chatHistory.Clear();

        // Add system message
        var systemMessage = new CompanionChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Sender = "System",
            Message = $"Companion '{config.Name}' initialized with {config.Personality} personality.",
            Timestamp = _timeProvider.UtcNow,
            IsVoice = false
        };
        _chatHistory.Add(systemMessage);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result<CompanionAction>> GetNextActionAsync(GameStateSnapshot gameState, CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Result<CompanionAction>.Failure("Companion not initialized", ErrorType.NotFound);
        }

        try
        {
            _logger.LogDebug("Getting next action for game state in {Level}", gameState.CurrentLevel);

            // Build context for LLM
            var personalityPrompt = GetPersonalityPrompt();
            var context = $"""
                {personalityPrompt}

                Current Game State:
                - Game: {gameState.GameId}
                - Level: {gameState.CurrentLevel}
                - Player Position: {gameState.PlayerPosition}
                - Player Health: {gameState.PlayerHealth:P0}
                - Enemy Count: {gameState.EnemyCount}
                - Current Objective: {gameState.CurrentObjective}
                - Nearby Items: {string.Join(", ", gameState.NearbyItems)}
                - Session Duration: {gameState.SessionDuration.TotalMinutes:F0} minutes

                Based on this state, what action should I take? Respond with:
                1. Action Type (Suggest, Warn, Assist, Celebrate, or None)
                2. Description
                3. Confidence (0.0-1.0)
                4. Voice line (brief, 1-2 sentences max)
                """;

            // Use LLM to determine action
            var messages = new List<ChatMessage>
            {
                new("system", context),
                new("user", "What should I do right now?")
            };
            var chatRequest = new ChatRequest(messages, "gpt-3.5-turbo", 150);

            var result = await _llmProvider.ChatAsync(chatRequest, ct);

            if (result.IsFailure)
            {
                // Fallback to rule-based action if LLM fails
                return Result.Success(GetFallbackAction(gameState));
            }

            // Parse LLM response
            var response = result.Value.Content;
            var action = ParseActionFromResponse(response, gameState);

            return Result.Success(action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next action");
            return Result.Success(GetFallbackAction(gameState));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ProcessVoiceCommandAsync(string voiceInput, CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Result<string>.Failure("Companion not initialized", ErrorType.NotFound);
        }

        try
        {
            _logger.LogInformation("Processing voice command: {Command}", voiceInput);

            var personalityPrompt = GetPersonalityPrompt();
            var messages = new List<ChatMessage>
            {
                new("system", $"{personalityPrompt}\n\nRespond to the player's voice command concisely and helpfully."),
                new("user", voiceInput)
            };
            var chatRequest = new ChatRequest(messages, "gpt-3.5-turbo", 100);

            var result = await _llmProvider.ChatAsync(chatRequest, ct);

            if (result.IsFailure)
            {
                return Result<string>.Failure(result.Error!, result.ErrorType);
            }

            var response = result.Value.Content;

            // Add to chat history
            AddChatMessage("Player", voiceInput, true);
            AddChatMessage("Companion", response, _isVoiceEnabled);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voice command");
            return Result<string>.Failure($"Error processing command: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result> LearnFromPlayerAsync(PlayerBehaviorSample sample, CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Task.FromResult(Result.Failure("Companion not initialized", ErrorType.NotFound));
        }

        _logger.LogDebug(
            "Learning from player behavior: {Action} in context {Context} - Success: {WasSuccessful}",
            sample.Action, sample.Context, sample.WasSuccessful);

        _behaviorSamples.Add(sample);

        // Keep only recent samples to avoid memory bloat
        if (_behaviorSamples.Count > 1000)
        {
            _behaviorSamples.RemoveAt(0);
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result<string>> GenerateResponseAsync(string playerMessage, CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Result<string>.Failure("Companion not initialized", ErrorType.NotFound);
        }

        try
        {
            var personalityPrompt = GetPersonalityPrompt();
            var recentHistory = _chatHistory
                .TakeLast(5)
                .Select(m => $"{m.Sender}: {m.Message}")
                .ToList();

            var context = $"""
                {personalityPrompt}

                Recent conversation:
                {string.Join("\n", recentHistory)}

                Respond to the player's message in character. Keep responses brief (1-2 sentences) unless detailed help is requested.
                """;

            var messages = new List<ChatMessage>
            {
                new("system", context),
                new("user", playerMessage)
            };
            var chatRequest = new ChatRequest(messages, "gpt-3.5-turbo", 150);

            var result = await _llmProvider.ChatAsync(chatRequest, ct);

            if (result.IsFailure)
            {
                return Result<string>.Failure(result.Error!, result.ErrorType);
            }

            var response = result.Value.Content;

            // Add to chat history
            AddChatMessage("Player", playerMessage, false);
            AddChatMessage("Companion", response, false);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return Result<string>.Failure($"Error generating response: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result> EnableVoiceAsync(CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Task.FromResult(Result.Failure("Companion not initialized", ErrorType.NotFound));
        }

        _isVoiceEnabled = true;
        _logger.LogInformation("Voice enabled for companion");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DisableVoiceAsync(CancellationToken ct = default)
    {
        _isVoiceEnabled = false;
        _logger.LogInformation("Voice disabled for companion");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<CompanionChatMessage>>> GetChatHistoryAsync(int count = 50, CancellationToken ct = default)
    {
        var history = _chatHistory
            .TakeLast(count)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(Result.Success<IReadOnlyList<CompanionChatMessage>>(history));
    }

    /// <inheritdoc />
    public Task<Result> ClearChatHistoryAsync(CancellationToken ct = default)
    {
        _chatHistory.Clear();
        _logger.LogInformation("Chat history cleared");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<CompanionConfiguration>> GetConfigurationAsync(CancellationToken ct = default)
    {
        if (_configuration == null)
        {
            return Task.FromResult(Result<CompanionConfiguration>.Failure("Companion not initialized", ErrorType.NotFound));
        }

        return Task.FromResult(Result.Success(_configuration));
    }

    /// <inheritdoc />
    public Task<Result> UpdateConfigurationAsync(CompanionConfiguration config, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating companion configuration: Name={Name}, Personality={Personality}, SkillLevel={SkillLevel}",
            config.Name, config.Personality, config.SkillLevel);

        _configuration = config;
        _isVoiceEnabled = config.VoiceEnabled;

        return Task.FromResult(Result.Success());
    }

    private string GetPersonalityPrompt()
    {
        if (_configuration == null) return string.Empty;

        var personalityName = _configuration.Personality.ToString();
        var prompts = _personalityPrompts.GetValueOrDefault(personalityName, _personalityPrompts["Supportive"]);

        var skillContext = _configuration.SkillLevel switch
        {
            SkillLevel.Beginner => "Adapt to the player's level and learn together. Be patient with mistakes.",
            SkillLevel.Equal => "Match the player's skill level and provide appropriate challenges.",
            SkillLevel.Mentor => "Play slightly better than the player and teach them advanced techniques.",
            SkillLevel.Professional => "Demonstrate high-level play and carry when necessary, but explain your decisions.",
            _ => string.Empty
        };

        return $"{string.Join(" ", prompts)}\n\nSkill Level Context: {skillContext}";
    }

    private CompanionAction GetFallbackAction(GameStateSnapshot gameState)
    {
        if (_configuration == null)
        {
            return new CompanionAction
            {
                ActionType = "None",
                Description = "Companion not initialized",
                Confidence = 0f,
                VoiceLine = null,
                Parameters = new Dictionary<string, object>()
            };
        }

        var personalityName = _configuration.Personality.ToString();
        var voiceLines = _voiceLines.GetValueOrDefault(personalityName, _voiceLines["Supportive"]);
        var randomLine = voiceLines[System.Random.Shared.Next(voiceLines.Count)];

        // Rule-based fallback logic
        if (gameState.PlayerHealth < 0.25f)
        {
            return new CompanionAction
            {
                ActionType = "Warn",
                Description = "Player health is critically low",
                Confidence = 0.9f,
                VoiceLine = _configuration.Personality == CompanionPersonality.Tactical
                    ? "Health critical. Recommend immediate retreat."
                    : "Watch out! Your health is really low!",
                Parameters = new Dictionary<string, object> { ["urgency"] = "high" }
            };
        }

        if (gameState.EnemyCount > 5)
        {
            return new CompanionAction
            {
                ActionType = "Warn",
                Description = "Multiple enemies detected",
                Confidence = 0.85f,
                VoiceLine = _configuration.Personality == CompanionPersonality.Silent
                    ? "Many enemies."
                    : "That's a lot of enemies! Be careful!",
                Parameters = new Dictionary<string, object> { ["enemyCount"] = gameState.EnemyCount }
            };
        }

        if (gameState.NearbyItems.Any())
        {
            var item = gameState.NearbyItems.First();
            return new CompanionAction
            {
                ActionType = "Suggest",
                Description = $"Item available: {item}",
                Confidence = 0.7f,
                VoiceLine = _configuration.Personality == CompanionPersonality.Silent
                    ? $"{item} here."
                    : $"Hey, there's a {item} nearby! Might be useful!",
                Parameters = new Dictionary<string, object> { ["item"] = item }
            };
        }

        // Default proactive suggestion
        if (_configuration.ProactiveSuggestions && gameState.SessionDuration.TotalMinutes > 30)
        {
            return new CompanionAction
            {
                ActionType = "Suggest",
                Description = "Session duration check",
                Confidence = 0.5f,
                VoiceLine = randomLine,
                Parameters = new Dictionary<string, object> { ["sessionDuration"] = gameState.SessionDuration }
            };
        }

        return new CompanionAction
        {
            ActionType = "None",
            Description = "No action needed at this time",
            Confidence = 0.5f,
            VoiceLine = null,
            Parameters = new Dictionary<string, object>()
        };
    }

    private CompanionAction ParseActionFromResponse(string response, GameStateSnapshot gameState)
    {
        // Simplified parsing
        var actionType = "Suggest";
        var voiceLine = response.Split('\n').FirstOrDefault()?.Trim() ?? response[..Math.Min(100, response.Length)];
        var confidence = 0.75f;

        if (response.Contains("Warn", StringComparison.OrdinalIgnoreCase))
            actionType = "Warn";
        else if (response.Contains("Assist", StringComparison.OrdinalIgnoreCase))
            actionType = "Assist";
        else if (response.Contains("Celebrate", StringComparison.OrdinalIgnoreCase))
            actionType = "Celebrate";
        else if (response.Contains("None", StringComparison.OrdinalIgnoreCase))
            actionType = "None";

        return new CompanionAction
        {
            ActionType = actionType,
            Description = response[..Math.Min(200, response.Length)],
            Confidence = confidence,
            VoiceLine = _isVoiceEnabled ? voiceLine : null,
            Parameters = new Dictionary<string, object>()
        };
    }

    private void AddChatMessage(string sender, string message, bool isVoice)
    {
        _chatHistory.Add(new CompanionChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Sender = sender,
            Message = message,
            Timestamp = _timeProvider.UtcNow,
            IsVoice = isVoice
        });

        // Keep chat history manageable
        if (_chatHistory.Count > 200)
        {
            _chatHistory.RemoveAt(0);
        }
    }
}
