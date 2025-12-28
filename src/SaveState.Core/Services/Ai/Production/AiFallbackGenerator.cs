using System;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiFallbackGenerator
    {
        string GenerateFallbackResponse(string input, string? intent);
    }

    public class AiFallbackGenerator : IAiFallbackGenerator
    {
        public string GenerateFallbackResponse(string input, string? intent)
        {
            return intent switch
            {
                "Combat" => "Understood. Ready for your next combat action.",
                "Lore" => "That's an interesting topic. Let me share what I know...",
                "Quest" => "Let me check on your current objectives...",
                "Economy" => "Here to help with your trading needs.",
                "Social" => "How can I help with this conversation?",
                _ => "I understand. How may I assist you?"
            };
        }
    }
}
