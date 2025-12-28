using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Emotion;
using SaveState.Core.Services.Ai.Orchestration;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Coordinates AI event publishing and subscription.
    /// </summary>
    public class AiEventCoordinator : IAiEventCoordinator
    {
        private readonly IAiEventBus _eventBus;
        private readonly IEmotionTagger _emotionTagger;

        public AiEventCoordinator(
            IAiEventBus eventBus,
            IEmotionTagger emotionTagger)
        {
            _eventBus = eventBus;
            _emotionTagger = emotionTagger;
        }

        public void SubscribeToEvent(string eventType, Events.EventHandler handler)
        {
            _eventBus.Subscribe(eventType, handler);
        }

        public async Task PublishEventAsync(AiEvent evt)
        {
            await _eventBus.PublishAsync(evt);
        }

        public async Task PublishResponseEventAsync(AiResponse response, IntentCategory intent, string emotion)
        {
            await _eventBus.PublishAsync(new AiEvent
            {
                EventType = "ai_response",
                Source = response.Agent ?? "unknown",
                Data = new Dictionary<string, object>
                {
                    ["intent"] = intent.ToString(),
                    ["emotion"] = emotion,
                    ["confidence"] = response.Confidence
                }
            });
        }
    }
}
