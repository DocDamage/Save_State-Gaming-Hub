using System;
using System.Collections.Generic;
using System.Text;

namespace SaveState.Core.Services.Ai.Prompts
{
    /// <summary>
    /// Mutates prompts based on player/context.
    /// </summary>
    public class DifficultyLevel
    {
        public string Name { get; set; } = "Normal";
        public float ChallengeMultiplier { get; set; } = 1.0f;
        public float HintFrequency { get; set; } = 0.5f;
        public float DetailLevel { get; set; } = 0.7f;
    }

    public class EmotionalArc
    {
        public string CurrentMood { get; set; } = "neutral";
        public float Intensity { get; set; } = 0.5f;
        public string? PreviousMood { get; set; }
        public bool IsClimactic { get; set; }
    }

    public class PlayerProfile
    {
        public string PlayerId { get; set; } = string.Empty;
        public float AggressionScore { get; set; } = 0.5f;
        public float ExplorationTendency { get; set; } = 0.5f;
        public float HumorTolerance { get; set; } = 0.5f;
        public float MoralAlignment { get; set; } = 0f; // -1 to 1
        public float ComplexityPreference { get; set; } = 0.5f;
        public float PacingPreference { get; set; } = 0.5f; // 0=methodical, 1=rush
        public Dictionary<string, float> Preferences { get; set; } = new();
    }

    public interface IPromptMutator
    {
        string Mutate(string basePrompt, PlayerProfile? player = null, EmotionalArc? arc = null, DifficultyLevel? diff = null);
        string InjectTone(string prompt, string tone);
        string AdjustComplexity(string prompt, float complexity);
        string AddPacingHints(string prompt, float pacingPreference);
    }

    public class PromptMutator : IPromptMutator
    {
        public string Mutate(string basePrompt, PlayerProfile? player = null, EmotionalArc? arc = null, DifficultyLevel? diff = null)
        {
            var sb = new StringBuilder();
            
            // Base context mutations
            sb.AppendLine("=== RESPONSE CUSTOMIZATION ===");
            
            // Player preferences
            if (player != null)
            {
                sb.AppendLine("\nPLAYER PREFERENCES:");
                
                if (player.AggressionScore > 0.7f)
                    sb.AppendLine("- Player prefers action and combat. Emphasize tension and conflict.");
                else if (player.AggressionScore < 0.3f)
                    sb.AppendLine("- Player prefers diplomacy. Offer non-violent alternatives.");

                if (player.ExplorationTendency > 0.7f)
                    sb.AppendLine("- Player enjoys exploration. Include environmental details and hidden paths.");
                
                if (player.HumorTolerance > 0.7f)
                    sb.AppendLine("- Player appreciates humor. Include witty dialogue and lighter moments.");
                else if (player.HumorTolerance < 0.3f)
                    sb.AppendLine("- Keep tone serious. Minimize comedic elements.");

                if (player.MoralAlignment > 0.5f)
                    sb.AppendLine("- Player tends toward heroic choices. Highlight righteous options.");
                else if (player.MoralAlignment < -0.5f)
                    sb.AppendLine("- Player embraces darker choices. Present morally gray opportunities.");

                if (player.ComplexityPreference > 0.7f)
                    sb.AppendLine("- Player enjoys complexity. Include nuanced details and branching implications.");
                else if (player.ComplexityPreference < 0.3f)
                    sb.AppendLine("- Keep things straightforward. Clear objectives, simple paths.");

                if (player.PacingPreference > 0.7f)
                    sb.AppendLine("- Player likes fast pacing. Be concise, action-focused.");
                else if (player.PacingPreference < 0.3f)
                    sb.AppendLine("- Player is methodical. Include atmospheric details, allow breathing room.");
            }

            // Emotional arc
            if (arc != null)
            {
                sb.AppendLine($"\nEMOTIONAL CONTEXT:");
                sb.AppendLine($"- Current mood: {arc.CurrentMood} (intensity: {arc.Intensity:P0})");
                
                if (arc.IsClimactic)
                    sb.AppendLine("- This is a CLIMACTIC moment. Heighten drama and stakes.");
                
                if (arc.PreviousMood != null && arc.PreviousMood != arc.CurrentMood)
                    sb.AppendLine($"- Transition from {arc.PreviousMood} to {arc.CurrentMood}");
            }

            // Difficulty adjustments
            if (diff != null)
            {
                sb.AppendLine($"\nDIFFICULTY: {diff.Name}");
                
                if (diff.HintFrequency > 0.7f)
                    sb.AppendLine("- Provide helpful hints and guidance.");
                else if (diff.HintFrequency < 0.3f)
                    sb.AppendLine("- Be cryptic. Let player figure things out.");

                if (diff.DetailLevel > 0.7f)
                    sb.AppendLine("- Include rich mechanical details.");
            }

            sb.AppendLine("\n=== END CUSTOMIZATION ===\n");
            sb.AppendLine(basePrompt);

            return sb.ToString();
        }

        public string InjectTone(string prompt, string tone)
        {
            var toneInstruction = tone.ToLowerInvariant() switch
            {
                "stoic" => "Respond with restrained emotion, formal language, few words.",
                "hostile" => "Respond with suspicion, short sentences, defensive posture.",
                "broken" => "Respond with despair, fragmented speech, emotional vulnerability.",
                "friendly" => "Respond warmly, openly, with genuine care and enthusiasm.",
                "mysterious" => "Respond cryptically, hint at deeper truths, speak in riddles.",
                "comedic" => "Respond with wit, wordplay, and situational humor.",
                "urgent" => "Respond with brevity and intensity. Time is critical.",
                "reverent" => "Respond with awe and respect. This is sacred ground.",
                _ => ""
            };

            if (string.IsNullOrEmpty(toneInstruction))
                return prompt;

            return $"TONE: {toneInstruction}\n\n{prompt}";
        }

        public string AdjustComplexity(string prompt, float complexity)
        {
            string instruction;
            if (complexity > 0.8f)
                instruction = "Use sophisticated vocabulary, complex sentence structures, and nuanced implications.";
            else if (complexity > 0.5f)
                instruction = "Balance accessibility with depth. Clear but not simplistic.";
            else if (complexity > 0.2f)
                instruction = "Use straightforward language. Prioritize clarity over style.";
            else
                instruction = "Use simple, direct language. Short sentences. Easy vocabulary.";

            return $"COMPLEXITY: {instruction}\n\n{prompt}";
        }

        public string AddPacingHints(string prompt, float pacingPreference)
        {
            string instruction;
            if (pacingPreference > 0.8f)
                instruction = "Be extremely concise. Action over description. Get to the point.";
            else if (pacingPreference > 0.5f)
                instruction = "Maintain momentum. Brief descriptions, focus on what matters.";
            else if (pacingPreference > 0.2f)
                instruction = "Allow the scene to breathe. Include sensory details and atmosphere.";
            else
                instruction = "Take your time. Rich descriptions, contemplative pacing, immersive world-building.";

            return $"PACING: {instruction}\n\n{prompt}";
        }
    }
}
