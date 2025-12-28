using System;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IResourceMonitor
    {
        ResourceUsage GetCurrentUsage();
        bool IsUnderPressure();
        void IncrementActiveOperations();
        void DecrementActiveOperations();
    }

    public class ResourceMonitor : IResourceMonitor
    {
        private readonly EdgeCaseConfig _config;
        private int _activeOperations = 0;

        public ResourceMonitor(EdgeCaseConfig? config = null)
        {
            _config = config ?? new EdgeCaseConfig();
        }

        public ResourceUsage GetCurrentUsage()
        {
            return new ResourceUsage
            {
                MemoryBytes = GC.GetTotalMemory(false),
                ActiveRequests = _activeOperations,
                QueuedRequests = 0, // Placeholder
                CpuEstimate = 0, // Placeholder
                Timestamp = DateTime.UtcNow
            };
        }

        public bool IsUnderPressure()
        {
            var usage = GetCurrentUsage();

            // Check memory pressure
            if (usage.MemoryBytes > _config.MemoryPressureThresholdBytes)
                return true;

            // Check active operations
            if (usage.ActiveRequests > _config.MaxConcurrentOperations)
                return true;

            return false;
        }

        public void IncrementActiveOperations()
        {
            System.Threading.Interlocked.Increment(ref _activeOperations);
        }

        public void DecrementActiveOperations()
        {
             System.Threading.Interlocked.Decrement(ref _activeOperations);
        }
    }
}
