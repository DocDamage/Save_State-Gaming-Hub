using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Adapters
{
    public interface ILlmProvider
    {
        LlmProvider ProviderType { get; }
        string ProviderName { get; }
        bool IsAvailable { get; }
        Task<string> ChatAsync(string model, List<LlmMessage> messages, float temperature, int maxTokens);
        Task<bool> HealthCheckAsync();
    }

    public class LlmProviderException : Exception
    {
        public LlmProvider Provider { get; }
        public bool IsTransient { get; }

        public LlmProviderException(LlmProvider provider, string message, bool isTransient = false, Exception? inner = null) 
            : base(message, inner)
        {
            Provider = provider;
            IsTransient = isTransient;
        }
    }
}
