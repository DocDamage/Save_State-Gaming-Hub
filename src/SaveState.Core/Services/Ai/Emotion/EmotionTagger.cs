using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Ai.Emotion
{
    /// <summary>
    /// Tag memories with emotional context.
    /// </summary>
    public class EmotionTag
    {
        public string PrimaryEmotion { get; set; } = "neutral";
        public float Intensity { get; set; } = 0.5f;
        public List<string> SecondaryEmotions { get; set; } = new();
        public float Valence { get; set; } = 0f; // -1 negative to +1 positive
        public float Arousal { get; set; } = 0.5f; // 0 calm to 1 excited
    }

    public interface IEmotionTagger
    {
        EmotionTag Tag(string text);
        string GetEmotionLabel(EmotionTag tag);
        float CalculateMoodShift(EmotionTag from, EmotionTag to);
    }

    public class EmotionTagger : IEmotionTagger
    {
        private readonly Dictionary<string, (float valence, float arousal)> _emotionMap = new()
        {
            ["joy"] = (0.9f, 0.7f),
            ["excitement"] = (0.7f, 0.9f),
            ["contentment"] = (0.6f, 0.3f),
            ["serenity"] = (0.5f, 0.1f),
            ["anger"] = (-0.6f, 0.9f),
            ["fear"] = (-0.7f, 0.8f),
            ["sadness"] = (-0.6f, 0.2f),
            ["disgust"] = (-0.5f, 0.5f),
            ["surprise"] = (0.1f, 0.8f),
            ["anticipation"] = (0.3f, 0.6f),
            ["trust"] = (0.5f, 0.3f),
            ["neutral"] = (0f, 0.3f)
        };

        private readonly Dictionary<string, List<string>> _emotionKeywords = new()
        {
            ["joy"] = new() { "happy", "glad", "delighted", "pleased", "wonderful", "amazing" },
            ["excitement"] = new() { "excited", "thrilled", "eager", "pumped", "energized" },
            ["anger"] = new() { "angry", "furious", "enraged", "irritated", "annoyed" },
            ["fear"] = new() { "afraid", "scared", "terrified", "anxious", "worried" },
            ["sadness"] = new() { "sad", "depressed", "grief", "sorrow", "heartbroken" },
            ["surprise"] = new() { "surprised", "shocked", "astonished", "amazed", "unexpected" },
            ["disgust"] = new() { "disgusted", "revolted", "sickened", "repulsed" },
            ["trust"] = new() { "trust", "believe", "faith", "confident", "reliable" },
            ["anticipation"] = new() { "waiting", "expecting", "hoping", "looking forward" }
        };

        public EmotionTag Tag(string text)
        {
            var tag = new EmotionTag();
            var textLower = text.ToLowerInvariant();
            var emotionScores = new Dictionary<string, float>();

            // Score each emotion
            foreach (var (emotion, keywords) in _emotionKeywords)
            {
                float score = 0;
                foreach (var keyword in keywords)
                {
                    if (textLower.Contains(keyword))
                    {
                        score += 1.0f;
                    }
                }
                if (score > 0)
                {
                    emotionScores[emotion] = score;
                }
            }

            if (emotionScores.Count == 0)
            {
                return tag; // neutral
            }

            // Find primary emotion
            string primaryEmotion = "neutral";
            float maxScore = 0;
            foreach (var (emotion, score) in emotionScores)
            {
                if (score > maxScore)
                {
                    maxScore = score;
                    primaryEmotion = emotion;
                }
            }

            tag.PrimaryEmotion = primaryEmotion;
            tag.Intensity = Math.Min(1.0f, maxScore / 3.0f);

            // Get valence and arousal
            if (_emotionMap.TryGetValue(primaryEmotion, out var coords))
            {
                tag.Valence = coords.valence;
                tag.Arousal = coords.arousal;
            }

            // Find secondary emotions
            foreach (var (emotion, score) in emotionScores)
            {
                if (emotion != primaryEmotion && score > 0)
                {
                    tag.SecondaryEmotions.Add(emotion);
                }
            }

            // Check for intensity modifiers
            if (textLower.Contains("very") || textLower.Contains("extremely") || text.Contains("!"))
            {
                tag.Intensity = Math.Min(1.0f, tag.Intensity + 0.2f);
                tag.Arousal = Math.Min(1.0f, tag.Arousal + 0.1f);
            }

            return tag;
        }

        public string GetEmotionLabel(EmotionTag tag)
        {
            var intensity = tag.Intensity switch
            {
                >= 0.8f => "Intense",
                >= 0.5f => "Moderate",
                >= 0.2f => "Mild",
                _ => "Subtle"
            };

            var valenceLabel = tag.Valence switch
            {
                >= 0.3f => "Positive",
                <= -0.3f => "Negative",
                _ => "Mixed"
            };

            return $"{intensity} {tag.PrimaryEmotion} ({valenceLabel})";
        }

        public float CalculateMoodShift(EmotionTag from, EmotionTag to)
        {
            var valenceDiff = Math.Abs(to.Valence - from.Valence);
            var arousalDiff = Math.Abs(to.Arousal - from.Arousal);
            return (valenceDiff + arousalDiff) / 2.0f;
        }
    }
}
