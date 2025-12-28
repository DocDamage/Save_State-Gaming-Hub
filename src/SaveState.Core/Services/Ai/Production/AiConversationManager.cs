using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiConversationManager
    {
        void AddTurn(string conversationId, string role, string content);
        List<(string Role, string Content)> GetHistory(string conversationId);
        void Clear(string conversationId);
    }

    public class AiConversationManager : IAiConversationManager
    {
        private readonly ConcurrentDictionary<string, List<(string Role, string Content)>> _conversations = new();
        private readonly ProductionAiConfig _config;

        public AiConversationManager(ProductionAiConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void AddTurn(string conversationId, string role, string content)
        {
            if (string.IsNullOrEmpty(conversationId)) return;

            var conv = _conversations.GetOrAdd(conversationId, _ => new List<(string, string)>());
            lock (conv)
            {
                conv.Add((role, content));

                // Keep only last N turns (1 turn = user + assistant)
                while (conv.Count > _config.MaxConversationTurns * 2)
                {
                    conv.RemoveAt(0);
                }
            }
        }

        public List<(string Role, string Content)> GetHistory(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId)) return new List<(string, string)>();

            if (_conversations.TryGetValue(conversationId, out var conv))
            {
                lock (conv)
                {
                    return conv.ToList();
                }
            }
            return new List<(string, string)>();
        }

        public void Clear(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId)) return;
            _conversations.TryRemove(conversationId, out _);
        }
    }
}
