using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Coordinates memory operations across short-term, episodic, and canonical memory layers.
    /// </summary>
    public class AiMemoryCoordinator : IAiMemoryCoordinator
    {
        private readonly IMemoryOrchestrator _memoryOrchestrator;

        public AiMemoryCoordinator(IMemoryOrchestrator memoryOrchestrator)
        {
            _memoryOrchestrator = memoryOrchestrator;
        }

        public async Task RecordInteractionAsync(string input, string output, string? context = null)
        {
            await _memoryOrchestrator.RecordInteraction(input, output, context);
        }

        public async Task<string> GetContextualMemoryAsync(string query)
        {
            var memories = await _memoryOrchestrator.Query(query);
            return string.Join("\n\n", memories);
        }

        public async Task<ConsolidatedContext> BuildMemoryContextAsync(string input, List<string>? relevantCharacters)
        {
            return await _memoryOrchestrator.BuildContext(input, relevantCharacters);
        }
    }
}
