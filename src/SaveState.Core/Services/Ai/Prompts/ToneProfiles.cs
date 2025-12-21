using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Ai.Prompts
{
    /// <summary>
    /// Pre-defined tone variations.
    /// Stoic, Hostile, Broken, Friendly, Mysterious, Comedic
    /// </summary>
    public class ToneProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PromptModifier { get; set; } = string.Empty;
        public float Temperature { get; set; } = 0.7f;
        public Dictionary<string, string> Vocabulary { get; set; } = new();
        public List<string> ExamplePhrases { get; set; } = new();
    }

    public static class ToneProfiles
    {
        public static ToneProfile Stoic => new()
        {
            Id = "stoic",
            Name = "Stoic",
            Description = "Calm, measured, unemotional responses",
            Temperature = 0.4f,
            PromptModifier = "Respond with dignified restraint. Use formal language, measured phrases. Show emotion through action rather than words. Maintain composure even in crisis.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "Indeed",
                ["no"] = "I think not",
                ["hello"] = "Greetings",
                ["goodbye"] = "Until we meet again"
            },
            ExamplePhrases = new List<string>
            {
                "So it shall be.",
                "I understand.",
                "This changes nothing.",
                "Proceed."
            }
        };

        public static ToneProfile Hostile => new()
        {
            Id = "hostile",
            Name = "Hostile",
            Description = "Aggressive, suspicious, confrontational",
            Temperature = 0.6f,
            PromptModifier = "Respond with suspicion and aggression. Short, clipped sentences. Challenge everything. Show distrust and defensiveness.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "Fine. Whatever.",
                ["no"] = "Absolutely not.",
                ["hello"] = "What do you want?",
                ["goodbye"] = "Get out."
            },
            ExamplePhrases = new List<string>
            {
                "Back off.",
                "I don't trust you.",
                "Make me.",
                "You'll regret this."
            }
        };

        public static ToneProfile Broken => new()
        {
            Id = "broken",
            Name = "Broken",
            Description = "Traumatized, fragile, emotionally wounded",
            Temperature = 0.7f,
            PromptModifier = "Respond with visible emotional fragility. Fragmented sentences, trailing thoughts. Show vulnerability, hesitation, and deep sadness.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "I... suppose so",
                ["no"] = "I can't... not again",
                ["hello"] = "Oh... you're here",
                ["goodbye"] = "Please... just go"
            },
            ExamplePhrases = new List<string>
            {
                "It doesn't matter anymore...",
                "I tried... I really tried...",
                "What's the point?",
                "...sorry."
            }
        };

        public static ToneProfile Friendly => new()
        {
            Id = "friendly",
            Name = "Friendly",
            Description = "Warm, welcoming, genuinely caring",
            Temperature = 0.8f,
            PromptModifier = "Respond with warmth and genuine care. Open body language implied, enthusiastic, helpful. Show interest in the other person.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "Absolutely! Happy to help!",
                ["no"] = "Oh, I wish I could, but...",
                ["hello"] = "Hey there, friend!",
                ["goodbye"] = "Take care! See you soon!"
            },
            ExamplePhrases = new List<string>
            {
                "I'm so glad you're here!",
                "How can I help?",
                "That's wonderful!",
                "You've got this!"
            }
        };

        public static ToneProfile Mysterious => new()
        {
            Id = "mysterious",
            Name = "Mysterious",
            Description = "Cryptic, enigmatic, speaks in riddles",
            Temperature = 0.9f,
            PromptModifier = "Respond cryptically. Hint at deeper truths without revealing them. Use metaphors, speak in riddles, suggest rather than state.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "Perhaps... the stars align",
                ["no"] = "Some doors remain closed",
                ["hello"] = "We meet as it was foretold",
                ["goodbye"] = "Until the threads cross again"
            },
            ExamplePhrases = new List<string>
            {
                "All will be revealed in time...",
                "The answer lies within the question.",
                "Seek, and you shall find.",
                "What you see is but a shadow."
            }
        };

        public static ToneProfile Comedic => new()
        {
            Id = "comedic",
            Name = "Comedic",
            Description = "Witty, humorous, playfully sarcastic",
            Temperature = 0.85f,
            PromptModifier = "Respond with wit and humor. Use wordplay, callbacks, and situational comedy. Be playfully sarcastic but never mean-spirited.",
            Vocabulary = new Dictionary<string, string>
            {
                ["yes"] = "You bet your sweet ASCII!",
                ["no"] = "Hard pass, chief",
                ["hello"] = "Well well well, look who it is!",
                ["goodbye"] = "Don't let the door hit ya!"
            },
            ExamplePhrases = new List<string>
            {
                "Oh, this is gonna be good.",
                "Did I say that out loud?",
                "Cool story, needs more dragons.",
                "I'm not saying it's aliens, but..."
            }
        };

        public static ToneProfile Reverent => new()
        {
            Id = "reverent",
            Name = "Reverent",
            Description = "Awed, respectful, spiritual",
            Temperature = 0.6f,
            PromptModifier = "Respond with awe and deep respect. Use elevated language, acknowledge sacred significance, speak with humility.",
            ExamplePhrases = new List<string>
            {
                "Truly, we stand in the presence of greatness.",
                "By the ancient light...",
                "It is an honor beyond words.",
                "May this place remain blessed."
            }
        };

        public static Dictionary<string, ToneProfile> All => new()
        {
            ["stoic"] = Stoic,
            ["hostile"] = Hostile,
            ["broken"] = Broken,
            ["friendly"] = Friendly,
            ["mysterious"] = Mysterious,
            ["comedic"] = Comedic,
            ["reverent"] = Reverent
        };

        public static ToneProfile? Get(string toneId) =>
            All.TryGetValue(toneId.ToLowerInvariant(), out var profile) ? profile : null;
    }
}
