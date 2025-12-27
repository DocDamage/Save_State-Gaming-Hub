using System;
using System.Collections.Concurrent;
using Serilog;

namespace SaveState.Core.Services.Ai.Governance
{
    public interface IGlobalKillSwitch
    {
        bool IsFeatureAllowed(string featureKey);
        void KillFeature(string featureKey, string reason);
        void ReviveFeature(string featureKey);
        void EmergencyShutdown();
    }

    public class GlobalKillSwitch : IGlobalKillSwitch
    {
        private readonly ILogger _logger = Log.ForContext<GlobalKillSwitch>();
        private readonly ConcurrentDictionary<string, bool> _disabledFeatures = new();
        private bool _emergencyShutdown = false;

        public bool IsFeatureAllowed(string featureKey)
        {
            if (_emergencyShutdown) return false;
            return !_disabledFeatures.ContainsKey(featureKey);
        }

        public void KillFeature(string featureKey, string reason)
        {
            _disabledFeatures[featureKey] = true;
            _logger.Warning("[KILL SWITCH] Feature '{FeatureKey}' disabled. Reason: {Reason}", featureKey, reason);
        }

        public void ReviveFeature(string featureKey)
        {
            _disabledFeatures.TryRemove(featureKey, out _);
            _logger.Information("[KILL SWITCH] Feature '{FeatureKey}' re-enabled.", featureKey);
        }

        public void EmergencyShutdown()
        {
            _emergencyShutdown = true;
            _logger.Fatal("[KILL SWITCH] EMERGENCY SHUTDOWN ACTIVATED. ALL AI SERVICES SUSPENDED.");
        }
    }
}
