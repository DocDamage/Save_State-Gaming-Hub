using System.Threading.Tasks;
using SaveState.Core.Services.Timeline;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Coordinates timeline operations including savepoints and what-if simulations.
    /// </summary>
    public class AiTimelineCoordinator : IAiTimelineCoordinator
    {
        private readonly ITimelineService _timelineService;
        private readonly IRewindService _rewindService;

        public AiTimelineCoordinator(
            ITimelineService timelineService,
            IRewindService rewindService)
        {
            _timelineService = timelineService;
            _rewindService = rewindService;
        }

        public void CreateSavePoint(string name, string? description = null)
        {
            _rewindService.CreateRewindPoint(name, description);
        }

        public async Task<WhatIfResult> SimulateWhatIfAsync(string scenario)
        {
            return await _timelineService.SimulateWhatIf(scenario, new System.Collections.Generic.List<StateDelta>());
        }
    }
}
