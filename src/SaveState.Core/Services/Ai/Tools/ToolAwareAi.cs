using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Tools
{
    /// <summary>
    /// Tool-aware AI with sandboxed execution.
    /// AI knows what tools exist but doesn't have unrestricted access.
    /// </summary>
    public interface IToolAwareAi
    {
        /// <summary>
        /// Get available tools for the current context
        /// </summary>
        IEnumerable<ToolDescription> GetAvailableTools(ToolContext context);

        /// <summary>
        /// Execute a tool with sandboxed restrictions
        /// </summary>
        Task<ToolResult> ExecuteToolAsync(ToolRequest request);

        /// <summary>
        /// Register a tool
        /// </summary>
        void RegisterTool(ToolDefinition tool);

        /// <summary>
        /// Get a tool description for AI prompting
        /// </summary>
        string GetToolManifest(ToolContext context);

        /// <summary>
        /// Validate a tool invocation before execution
        /// </summary>
        ToolValidationResult ValidateInvocation(ToolRequest request);
    }

    /// <summary>
    /// Context for tool availability
    /// </summary>
    public class ToolContext
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public ToolAccessLevel AccessLevel { get; set; } = ToolAccessLevel.Basic;
        public string? CurrentGameId { get; set; }
        public List<string> ActiveQuests { get; set; } = new();
        public Dictionary<string, object> State { get; set; } = new();
    }

    /// <summary>
    /// Tool access levels
    /// </summary>
    public enum ToolAccessLevel
    {
        None = 0,
        Basic = 1,          // Read-only tools
        Standard = 2,       // Common tools
        Elevated = 3,       // Powerful tools
        Administrator = 4   // Full access
    }

    /// <summary>
    /// Description of a tool for AI consumption
    /// </summary>
    public class ToolDescription
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<ToolParameter> Parameters { get; set; } = new();
        public string? ReturnType { get; set; }
        public List<string> Examples { get; set; } = new();
    }

    /// <summary>
    /// A tool parameter
    /// </summary>
    public class ToolParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; }
        public List<object>? AllowedValues { get; set; }
    }

    /// <summary>
    /// Full tool definition
    /// </summary>
    public class ToolDefinition
    {
        public string ToolId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
        public ToolAccessLevel RequiredLevel { get; set; } = ToolAccessLevel.Standard;
        public List<ToolParameter> Parameters { get; set; } = new();
        public Func<Dictionary<string, object>, Task<object?>>? Handler { get; set; }
        public bool RequiresConfirmation { get; set; } = false;
        public bool IsDestructive { get; set; } = false;
        public int? RateLimitPerMinute { get; set; }
        public List<string> RequiredPermissions { get; set; } = new();
    }

    /// <summary>
    /// Request to execute a tool
    /// </summary>
    public class ToolRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public ToolContext Context { get; set; } = new();
        public bool BypassConfirmation { get; set; } = false;
    }

    /// <summary>
    /// Result of tool execution
    /// </summary>
    public class ToolResult
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool WasSandboxed { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Validation result for tool invocation
    /// </summary>
    public class ToolValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool RequiresConfirmation { get; set; }
    }

    /// <summary>
    /// Default implementation of tool-aware AI
    /// </summary>
    public class ToolAwareAi : IToolAwareAi
    {
        private readonly ConcurrentDictionary<string, ToolDefinition> _tools = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastExecutions = new();
        private readonly ConcurrentDictionary<string, int> _executionCounts = new();

        public ToolAwareAi()
        {
            RegisterDefaultTools();
        }

        public IEnumerable<ToolDescription> GetAvailableTools(ToolContext context)
        {
            return _tools.Values
                .Where(t => t.RequiredLevel <= context.AccessLevel)
                .Select(t => new ToolDescription
                {
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    Parameters = t.Parameters,
                    Examples = GetExamplesForTool(t.Name)
                });
        }

        public async Task<ToolResult> ExecuteToolAsync(ToolRequest request)
        {
            var startTime = DateTime.UtcNow;

            // Validate first
            var validation = ValidateInvocation(request);
            if (!validation.IsValid)
            {
                return new ToolResult
                {
                    Success = false,
                    Error = string.Join("; ", validation.Errors),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }

            if (!_tools.TryGetValue(request.ToolName, out var tool))
            {
                return new ToolResult
                {
                    Success = false,
                    Error = $"Tool '{request.ToolName}' not found",
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }

            // Check confirmation
            if (tool.RequiresConfirmation && !request.BypassConfirmation)
            {
                return new ToolResult
                {
                    Success = false,
                    Error = "Tool requires confirmation before execution",
                    Warnings = new() { "CONFIRMATION_REQUIRED" }
                };
            }

            // Check rate limit
            if (tool.RateLimitPerMinute.HasValue)
            {
                var key = $"{request.Context.UserId}:{request.ToolName}";
                _executionCounts.TryGetValue(key, out var count);
                
                if (count >= tool.RateLimitPerMinute.Value)
                {
                    return new ToolResult
                    {
                        Success = false,
                        Error = "Rate limit exceeded for this tool",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }
                
                _executionCounts.AddOrUpdate(key, 1, (_, c) => c + 1);
            }

            // Execute
            try
            {
                if (tool.Handler == null)
                {
                    return new ToolResult
                    {
                        Success = false,
                        Error = "Tool has no handler",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                var result = await tool.Handler(request.Parameters);
                
                return new ToolResult
                {
                    Success = true,
                    Result = result,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    WasSandboxed = true,
                    Warnings = validation.Warnings
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    Error = $"Tool execution failed: {ex.Message}",
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        public void RegisterTool(ToolDefinition tool)
        {
            _tools[tool.Name] = tool;
        }

        public string GetToolManifest(ToolContext context)
        {
            var tools = GetAvailableTools(context);
            var manifest = "Available tools:\n\n";

            foreach (var tool in tools)
            {
                manifest += $"**{tool.Name}**: {tool.Description}\n";
                if (tool.Parameters.Any())
                {
                    manifest += "  Parameters:\n";
                    foreach (var param in tool.Parameters)
                    {
                        var required = param.Required ? " (required)" : "";
                        manifest += $"    - {param.Name}: {param.Type}{required} - {param.Description}\n";
                    }
                }
                manifest += "\n";
            }

            return manifest;
        }

        public ToolValidationResult ValidateInvocation(ToolRequest request)
        {
            var result = new ToolValidationResult { IsValid = true };

            if (!_tools.TryGetValue(request.ToolName, out var tool))
            {
                result.IsValid = false;
                result.Errors.Add($"Unknown tool: {request.ToolName}");
                return result;
            }

            // Check access level
            if (tool.RequiredLevel > request.Context.AccessLevel)
            {
                result.IsValid = false;
                result.Errors.Add($"Insufficient access level. Required: {tool.RequiredLevel}");
            }

            // Check required parameters
            foreach (var param in tool.Parameters.Where(p => p.Required))
            {
                if (!request.Parameters.ContainsKey(param.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Missing required parameter: {param.Name}");
                }
            }

            // Check allowed values
            foreach (var param in tool.Parameters.Where(p => p.AllowedValues != null))
            {
                if (request.Parameters.TryGetValue(param.Name, out var value))
                {
                    if (!param.AllowedValues!.Contains(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid value for {param.Name}. Allowed: {string.Join(", ", param.AllowedValues)}");
                    }
                }
            }

            // Add warnings
            if (tool.IsDestructive)
            {
                result.Warnings.Add("This is a destructive operation");
            }

            result.RequiresConfirmation = tool.RequiresConfirmation;

            return result;
        }

        private List<string> GetExamplesForTool(string toolName)
        {
            var examples = new Dictionary<string, List<string>>
            {
                { "simulate_outcome", new() { "simulate_outcome(scenario='What if I attack the guard?')" } },
                { "check_archives", new() { "check_archives(topic='Dragon War history')" } },
                { "get_hint", new() { "get_hint(quest_id='main_quest_3')" } }
            };

            return examples.TryGetValue(toolName, out var ex) ? ex : new();
        }

        private void RegisterDefaultTools()
        {
            // Simulation tool - AI can simulate outcomes
            RegisterTool(new ToolDefinition
            {
                Name = "simulate_outcome",
                Description = "Simulate a hypothetical outcome without actually performing the action",
                Category = "analysis",
                RequiredLevel = ToolAccessLevel.Basic,
                Parameters = new()
                {
                    new ToolParameter { Name = "scenario", Type = "string", Required = true, Description = "Description of scenario to simulate" }
                },
                Handler = (p) =>
                {
                    var scenario = p["scenario"].ToString();
                    return Task.FromResult<object?>($"Simulation of '{scenario}': [This would trigger actual simulation logic]");
                }
            });

            // Archive lookup - AI can check historical records
            RegisterTool(new ToolDefinition
            {
                Name = "check_archives",
                Description = "Search the game's lore archives for information",
                Category = "research",
                RequiredLevel = ToolAccessLevel.Basic,
                Parameters = new()
                {
                    new ToolParameter { Name = "topic", Type = "string", Required = true, Description = "Topic to search for" },
                    new ToolParameter { Name = "era", Type = "string", Required = false, Description = "Historical era to focus on" }
                },
                Handler = (p) =>
                {
                    var topic = p["topic"].ToString();
                    return Task.FromResult<object?>($"Archive search for '{topic}': [Would return relevant lore]");
                }
            });

            // Hint system - AI can provide contextual hints
            RegisterTool(new ToolDefinition
            {
                Name = "get_hint",
                Description = "Get a hint for the current quest or puzzle",
                Category = "assistance",
                RequiredLevel = ToolAccessLevel.Basic,
                RateLimitPerMinute = 5,
                Parameters = new()
                {
                    new ToolParameter { Name = "quest_id", Type = "string", Required = false, Description = "Specific quest to get hint for" }
                },
                Handler = (p) =>
                {
                    return Task.FromResult<object?>("Consider examining your surroundings more carefully...");
                }
            });

            // NPC memory lookup
            RegisterTool(new ToolDefinition
            {
                Name = "recall_npc_memory",
                Description = "Recall what this NPC knows about a topic or character",
                Category = "npc",
                RequiredLevel = ToolAccessLevel.Standard,
                Parameters = new()
                {
                    new ToolParameter { Name = "npc_id", Type = "string", Required = true, Description = "NPC to query" },
                    new ToolParameter { Name = "topic", Type = "string", Required = true, Description = "Topic to recall" }
                },
                Handler = (p) =>
                {
                    var npc = p["npc_id"].ToString();
                    var topic = p["topic"].ToString();
                    return Task.FromResult<object?>($"NPC {npc}'s memories about {topic}: [Memory retrieval]");
                }
            });

            // Request clarification - AI can ask for more context
            RegisterTool(new ToolDefinition
            {
                Name = "request_clarification",
                Description = "Request clarification from the system about ambiguous situations",
                Category = "meta",
                RequiredLevel = ToolAccessLevel.Basic,
                Parameters = new()
                {
                    new ToolParameter { Name = "question", Type = "string", Required = true, Description = "What needs clarification" }
                },
                Handler = (p) =>
                {
                    return Task.FromResult<object?>($"Clarification requested: {p["question"]}");
                }
            });
        }
    }
}
