using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Prompts
{
    /// <summary>
    /// Template management with variable injection.
    /// </summary>
    public class PromptTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
        public string Template { get; set; } = string.Empty;
        public List<string> RequiredVariables { get; set; } = new();
        public List<string> OptionalVariables { get; set; } = new();
        public Dictionary<string, string> DefaultValues { get; set; } = new();
        public string? Description { get; set; }
    }

    public interface IPromptTemplateService
    {
        string Render(string templateId, Dictionary<string, object> variables);
        string RenderTemplate(PromptTemplate template, Dictionary<string, object> variables);
        void RegisterTemplate(PromptTemplate template);
        PromptTemplate? GetTemplate(string templateId);
        IEnumerable<PromptTemplate> GetTemplatesByCategory(string category);
        Task LoadTemplatesFromDirectory(string path);
        Task SaveTemplates(string path);
    }

    public class PromptTemplateService : IPromptTemplateService
    {
        private readonly Dictionary<string, PromptTemplate> _templates = new();
        private readonly Regex _variablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        public PromptTemplateService()
        {
            RegisterDefaultTemplates();
        }

        private void RegisterDefaultTemplates()
        {
            RegisterTemplate(new PromptTemplate
            {
                Id = "narrative_scene",
                Name = "Narrative Scene",
                Category = "narrative",
                Template = @"Describe the scene at {{location}}.

CONTEXT:
- Current mood: {{mood}}
- Time of day: {{time_of_day}}
- Weather: {{weather}}

The player {{player_action}}.

Write an immersive description that:
- Uses sensory details ({{senses}})
- Maintains the {{tone}} tone
- Advances the story naturally",
                RequiredVariables = new() { "location", "player_action" },
                OptionalVariables = new() { "mood", "time_of_day", "weather", "senses", "tone" },
                DefaultValues = new()
                {
                    ["mood"] = "neutral",
                    ["time_of_day"] = "day",
                    ["weather"] = "clear",
                    ["senses"] = "sight, sound, smell",
                    ["tone"] = "atmospheric"
                }
            });

            RegisterTemplate(new PromptTemplate
            {
                Id = "combat_narration",
                Name = "Combat Narration",
                Category = "combat",
                Template = @"Narrate this combat action:

ATTACKER: {{attacker}}
TARGET: {{target}}
ACTION: {{action}}
RESULT: {{result}} ({{damage}} damage)
STATUS: {{status_effects}}

Write a visceral, impactful description of this combat moment.
Tone: {{tone}}",
                RequiredVariables = new() { "attacker", "target", "action", "result", "damage" },
                OptionalVariables = new() { "status_effects", "tone" },
                DefaultValues = new()
                {
                    ["status_effects"] = "none",
                    ["tone"] = "intense"
                }
            });

            RegisterTemplate(new PromptTemplate
            {
                Id = "dialogue_response",
                Name = "Dialogue Response",
                Category = "dialogue",
                Template = @"Generate a response for {{character_name}}.

CHARACTER PROFILE:
- Personality: {{personality}}
- Relationship with player: {{relationship}}
- Current emotion: {{emotion}}

CONTEXT: {{context}}

PLAYER SAID: ""{{player_dialogue}}""

Respond in character. Voice: {{voice_style}}",
                RequiredVariables = new() { "character_name", "player_dialogue" },
                OptionalVariables = new() { "personality", "relationship", "emotion", "context", "voice_style" },
                DefaultValues = new()
                {
                    ["personality"] = "neutral",
                    ["relationship"] = "acquaintance",
                    ["emotion"] = "calm",
                    ["context"] = "casual conversation",
                    ["voice_style"] = "natural"
                }
            });

            RegisterTemplate(new PromptTemplate
            {
                Id = "lore_explanation",
                Name = "Lore Explanation",
                Category = "lore",
                Template = @"Explain this lore topic: {{topic}}

CANONICAL FACTS:
{{canonical_context}}

PRESENTATION STYLE: {{style}}
DETAIL LEVEL: {{detail_level}}

Explain this to the player as if you were a {{narrator_role}}.",
                RequiredVariables = new() { "topic" },
                OptionalVariables = new() { "canonical_context", "style", "detail_level", "narrator_role" },
                DefaultValues = new()
                {
                    ["canonical_context"] = "(no specific lore provided)",
                    ["style"] = "informative",
                    ["detail_level"] = "moderate",
                    ["narrator_role"] = "wise scholar"
                }
            });

            RegisterTemplate(new PromptTemplate
            {
                Id = "quest_guidance",
                Name = "Quest Guidance",
                Category = "quest",
                Template = @"Provide guidance for quest: {{quest_name}}

CURRENT OBJECTIVE: {{objective}}
PROGRESS: {{progress}}
LOCATION: {{location}}
HINT LEVEL: {{hint_level}} (vague/moderate/specific)

Help the player understand what to do next without spoiling the experience.",
                RequiredVariables = new() { "quest_name", "objective" },
                OptionalVariables = new() { "progress", "location", "hint_level" },
                DefaultValues = new()
                {
                    ["progress"] = "in progress",
                    ["location"] = "unknown",
                    ["hint_level"] = "moderate"
                }
            });
        }

        public string Render(string templateId, Dictionary<string, object> variables)
        {
            if (!_templates.TryGetValue(templateId, out var template))
            {
                return $"[Template '{templateId}' not found]";
            }

            return RenderTemplate(template, variables);
        }

        public string RenderTemplate(PromptTemplate template, Dictionary<string, object> variables)
        {
            var result = template.Template;

            // Apply default values first
            foreach (var (key, value) in template.DefaultValues)
            {
                if (!variables.ContainsKey(key))
                {
                    variables[key] = value;
                }
            }

            // Check required variables
            foreach (var required in template.RequiredVariables)
            {
                if (!variables.ContainsKey(required))
                {
                    throw new ArgumentException($"Missing required variable: {required}");
                }
            }

            // Replace all variables
            result = _variablePattern.Replace(result, match =>
            {
                var varName = match.Groups[1].Value;
                if (variables.TryGetValue(varName, out var value))
                {
                    return value?.ToString() ?? "";
                }
                return match.Value; // Keep original if not found
            });

            return result;
        }

        public void RegisterTemplate(PromptTemplate template)
        {
            _templates[template.Id] = template;
        }

        public PromptTemplate? GetTemplate(string templateId)
        {
            return _templates.TryGetValue(templateId, out var template) ? template : null;
        }

        public IEnumerable<PromptTemplate> GetTemplatesByCategory(string category)
        {
            return _templates.Values.Where(t => t.Category == category);
        }

        public async Task LoadTemplatesFromDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var template = JsonSerializer.Deserialize<PromptTemplate>(json);
                    if (template != null)
                    {
                        _templates[template.Id] = template;
                    }
                }
                catch { /* Skip invalid files */ }
            }
        }

        public async Task SaveTemplates(string path)
        {
            Directory.CreateDirectory(path);

            foreach (var template in _templates.Values)
            {
                var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                var filePath = Path.Combine(path, $"{template.Id}.json");
                await File.WriteAllTextAsync(filePath, json);
            }
        }
    }
}
