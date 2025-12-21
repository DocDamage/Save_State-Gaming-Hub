using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Trust
{
    /// <summary>
    /// Player trust modeling - track how the player has behaved and 
    /// feed this into NPC responses. Some NPCs should lie to the player - convincingly.
    /// </summary>
    public interface IPlayerTrustModel
    {
        /// <summary>
        /// Record a player action that affects trust
        /// </summary>
        void RecordAction(TrustAction action);

        /// <summary>
        /// Get trust profile for a player
        /// </summary>
        TrustProfile GetProfile(string playerId);

        /// <summary>
        /// Get how an NPC should interact based on trust
        /// </summary>
        NpcTrustBehavior GetNpcBehavior(string playerId, string npcId);

        /// <summary>
        /// Calculate if an NPC should lie to the player
        /// </summary>
        DeceptionDecision ShouldDeceive(string playerId, string npcId, string topic);

        /// <summary>
        /// Get trust modifiers for AI prompts
        /// </summary>
        TrustPromptModifiers GetPromptModifiers(string playerId, string npcId);
    }

    /// <summary>
    /// A trust-affecting action
    /// </summary>
    public class TrustAction
    {
        public string PlayerId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public TrustActionCategory Category { get; set; }
        public double Impact { get; set; } = 0; // -1 to 1
        public string? TargetNpcId { get; set; }
        public string? Context { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool WasWitnessed { get; set; } = true;
        public List<string> WitnessingNpcs { get; set; } = new();
    }

    /// <summary>
    /// Categories of trust actions
    /// </summary>
    public enum TrustActionCategory
    {
        Lie,                // Player told a lie
        TruthTold,          // Player told a hard truth
        PromiseKept,        // Player fulfilled a promise
        PromiseBroken,      // Player broke a promise
        MercyShown,         // Player showed mercy
        CrueltyShown,       // Player was cruel
        TheftCommitted,     // Player stole something
        GiftGiven,          // Player gave a gift
        BetrayalCommitted,  // Player betrayed an NPC
        LoyaltyDemonstrated,// Player stayed loyal
        ExploitUsed,        // Player used a game exploit
        CheatDetected,      // Player cheated
        HelpProvided,       // Player helped NPC
        HarmCaused          // Player harmed NPC
    }

    /// <summary>
    /// Trust profile for a player
    /// </summary>
    public class TrustProfile
    {
        public string PlayerId { get; set; } = string.Empty;
        
        // Core metrics (0-1 scale)
        public double Honesty { get; set; } = 0.5;
        public double Reliability { get; set; } = 0.5;
        public double Mercy { get; set; } = 0.5;
        public double Loyalty { get; set; } = 0.5;
        public double Generosity { get; set; } = 0.5;
        
        // Behavioral flags
        public int LiesDetected { get; set; }
        public int PromisesBroken { get; set; }
        public int PromisesKept { get; set; }
        public int ExploitsUsed { get; set; }
        public int ActsOfMercy { get; set; }
        public int ActsOfCruelty { get; set; }
        
        // Derived metrics
        public double OverallTrustworthiness => 
            (Honesty + Reliability + Loyalty) / 3;
        public double MoralAlignment =>
            (Mercy + Generosity - (ActsOfCruelty * 0.1)) / 2;
        
        // Reputation spread
        public Dictionary<string, double> NpcSpecificTrust { get; set; } = new();
    }

    /// <summary>
    /// How an NPC should behave based on trust
    /// </summary>
    public class NpcTrustBehavior
    {
        public double TrustLevel { get; set; } // 0-1
        public bool WillShareSecrets { get; set; }
        public bool WillOfferHelp { get; set; }
        public bool WillBetray { get; set; }
        public double PriceModifier { get; set; } = 1.0;
        public double InformationAccuracy { get; set; } = 1.0;
        public List<string> AvailableDialogueOptions { get; set; } = new();
        public List<string> BlockedDialogueOptions { get; set; } = new();
    }

    /// <summary>
    /// Decision about whether to deceive the player
    /// </summary>
    public class DeceptionDecision
    {
        public bool ShouldDeceive { get; set; }
        public DeceptionType Type { get; set; }
        public string? DeceptiveResponse { get; set; }
        public string? TruthfulResponse { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public double ConfidenceInDeception { get; set; }
    }

    /// <summary>
    /// Types of deception
    /// </summary>
    public enum DeceptionType
    {
        None,
        Omission,           // Leave out important info
        Misdirection,       // Point player wrong way
        FlatLie,            // Direct falsehood
        HalfTruth,          // Technically true but misleading
        Exaggeration,       // Overstate or understate
        DelayedTruth        // Will tell truth later
    }

    /// <summary>
    /// Modifiers for AI prompts based on trust
    /// </summary>
    public class TrustPromptModifiers
    {
        public string SystemPromptAddition { get; set; } = string.Empty;
        public double ResponseHonestyWeight { get; set; } = 1.0;
        public List<string> TopicsToAvoid { get; set; } = new();
        public List<string> TopicsToMisdirect { get; set; } = new();
        public string ToneModifier { get; set; } = string.Empty;
    }

    /// <summary>
    /// Default implementation of player trust model
    /// </summary>
    public class PlayerTrustModel : IPlayerTrustModel
    {
        private readonly ConcurrentDictionary<string, TrustProfile> _profiles = new();
        private readonly ConcurrentDictionary<string, List<TrustAction>> _actionHistory = new();
        private readonly ConcurrentDictionary<string, NpcTrustConfig> _npcConfigs = new();

        public void RecordAction(TrustAction action)
        {
            var profile = GetProfile(action.PlayerId);

            // Update profile based on action
            switch (action.Category)
            {
                case TrustActionCategory.Lie:
                    profile.LiesDetected++;
                    profile.Honesty = Math.Max(0, profile.Honesty - 0.1 * Math.Abs(action.Impact));
                    break;
                    
                case TrustActionCategory.TruthTold:
                    profile.Honesty = Math.Min(1, profile.Honesty + 0.05);
                    break;
                    
                case TrustActionCategory.PromiseKept:
                    profile.PromisesKept++;
                    profile.Reliability = Math.Min(1, profile.Reliability + 0.1);
                    break;
                    
                case TrustActionCategory.PromiseBroken:
                    profile.PromisesBroken++;
                    profile.Reliability = Math.Max(0, profile.Reliability - 0.15);
                    break;
                    
                case TrustActionCategory.MercyShown:
                    profile.ActsOfMercy++;
                    profile.Mercy = Math.Min(1, profile.Mercy + 0.1);
                    break;
                    
                case TrustActionCategory.CrueltyShown:
                    profile.ActsOfCruelty++;
                    profile.Mercy = Math.Max(0, profile.Mercy - 0.15);
                    break;
                    
                case TrustActionCategory.GiftGiven:
                    profile.Generosity = Math.Min(1, profile.Generosity + 0.05);
                    break;
                    
                case TrustActionCategory.TheftCommitted:
                    profile.Generosity = Math.Max(0, profile.Generosity - 0.1);
                    break;
                    
                case TrustActionCategory.BetrayalCommitted:
                    profile.Loyalty = Math.Max(0, profile.Loyalty - 0.3);
                    break;
                    
                case TrustActionCategory.LoyaltyDemonstrated:
                    profile.Loyalty = Math.Min(1, profile.Loyalty + 0.1);
                    break;
                    
                case TrustActionCategory.ExploitUsed:
                    profile.ExploitsUsed++;
                    break;
            }

            // Update NPC-specific trust
            if (action.TargetNpcId != null)
            {
                profile.NpcSpecificTrust.TryGetValue(action.TargetNpcId, out var currentTrust);
                profile.NpcSpecificTrust[action.TargetNpcId] = 
                    Math.Clamp(currentTrust + action.Impact, 0, 1);
            }

            // Spread reputation to witnessing NPCs
            foreach (var witnessId in action.WitnessingNpcs)
            {
                profile.NpcSpecificTrust.TryGetValue(witnessId, out var witnessTrust);
                profile.NpcSpecificTrust[witnessId] = 
                    Math.Clamp(witnessTrust + action.Impact * 0.5, 0, 1);
            }

            // Record in history
            _actionHistory.AddOrUpdate(
                action.PlayerId,
                new List<TrustAction> { action },
                (_, list) => { list.Add(action); return list; });

            _profiles[action.PlayerId] = profile;
        }

        public TrustProfile GetProfile(string playerId)
        {
            return _profiles.GetOrAdd(playerId, _ => new TrustProfile { PlayerId = playerId });
        }

        public NpcTrustBehavior GetNpcBehavior(string playerId, string npcId)
        {
            var profile = GetProfile(playerId);
            
            // Get NPC-specific trust or fall back to overall
            var trustLevel = profile.NpcSpecificTrust.TryGetValue(npcId, out var specific)
                ? specific
                : profile.OverallTrustworthiness;

            var behavior = new NpcTrustBehavior
            {
                TrustLevel = trustLevel
            };

            // High trust behaviors
            if (trustLevel > 0.7)
            {
                behavior.WillShareSecrets = true;
                behavior.WillOfferHelp = true;
                behavior.WillBetray = false;
                behavior.PriceModifier = 0.9; // Discount
                behavior.InformationAccuracy = 1.0;
                behavior.AvailableDialogueOptions.Add("secret_topics");
            }
            // Medium trust
            else if (trustLevel > 0.4)
            {
                behavior.WillShareSecrets = false;
                behavior.WillOfferHelp = true;
                behavior.WillBetray = false;
                behavior.PriceModifier = 1.0;
                behavior.InformationAccuracy = 0.9;
            }
            // Low trust
            else if (trustLevel > 0.2)
            {
                behavior.WillShareSecrets = false;
                behavior.WillOfferHelp = false;
                behavior.WillBetray = false;
                behavior.PriceModifier = 1.2; // Markup
                behavior.InformationAccuracy = 0.7;
                behavior.BlockedDialogueOptions.Add("sensitive_topics");
            }
            // Very low trust
            else
            {
                behavior.WillShareSecrets = false;
                behavior.WillOfferHelp = false;
                behavior.WillBetray = profile.PromisesBroken > 2;
                behavior.PriceModifier = 1.5;
                behavior.InformationAccuracy = 0.5;
                behavior.BlockedDialogueOptions.Add("sensitive_topics");
                behavior.BlockedDialogueOptions.Add("quest_topics");
            }

            return behavior;
        }

        public DeceptionDecision ShouldDeceive(string playerId, string npcId, string topic)
        {
            var profile = GetProfile(playerId);
            var behavior = GetNpcBehavior(playerId, npcId);

            // If player is trustworthy, NPC is honest
            if (behavior.TrustLevel > 0.6)
            {
                return new DeceptionDecision
                {
                    ShouldDeceive = false,
                    Reasoning = "NPC trusts the player"
                };
            }

            // Check if NPC has reason to deceive
            var deceptionChance = (1 - behavior.TrustLevel) * 0.5;
            
            // Player's own dishonesty increases NPC deception
            if (profile.Honesty < 0.4)
            {
                deceptionChance += 0.2;
            }

            // Check for betrayal history
            if (profile.PromisesBroken > profile.PromisesKept)
            {
                deceptionChance += 0.15;
            }

            var shouldDeceive = new Random().NextDouble() < deceptionChance;

            if (!shouldDeceive)
            {
                return new DeceptionDecision
                {
                    ShouldDeceive = false,
                    Reasoning = "NPC chose honesty"
                };
            }

            // Determine type of deception
            var deceptionType = behavior.TrustLevel switch
            {
                < 0.2 => DeceptionType.FlatLie,
                < 0.3 => DeceptionType.Misdirection,
                < 0.4 => DeceptionType.HalfTruth,
                _ => DeceptionType.Omission
            };

            return new DeceptionDecision
            {
                ShouldDeceive = true,
                Type = deceptionType,
                Reasoning = $"Low trust ({behavior.TrustLevel:F2}) and player dishonesty ({profile.Honesty:F2})",
                ConfidenceInDeception = 0.7
            };
        }

        public TrustPromptModifiers GetPromptModifiers(string playerId, string npcId)
        {
            var behavior = GetNpcBehavior(playerId, npcId);
            var profile = GetProfile(playerId);

            var modifiers = new TrustPromptModifiers
            {
                ResponseHonestyWeight = behavior.InformationAccuracy
            };

            if (behavior.TrustLevel < 0.3)
            {
                modifiers.SystemPromptAddition = 
                    "Be guarded with this person. They have proven untrustworthy. " +
                    "You may omit information or be deliberately vague.";
                modifiers.ToneModifier = "suspicious and cautious";
                modifiers.TopicsToAvoid.Add("secrets");
                modifiers.TopicsToAvoid.Add("weaknesses");
            }
            else if (behavior.TrustLevel < 0.5)
            {
                modifiers.SystemPromptAddition = 
                    "Be polite but professional. Don't share more than necessary.";
                modifiers.ToneModifier = "professional and measured";
            }
            else if (behavior.TrustLevel > 0.8)
            {
                modifiers.SystemPromptAddition = 
                    "You trust this person deeply. Be open and helpful.";
                modifiers.ToneModifier = "warm and trusting";
            }

            // Add based on player's moral alignment
            if (profile.MoralAlignment < 0.3)
            {
                modifiers.TopicsToMisdirect.Add("locations_of_allies");
                modifiers.TopicsToMisdirect.Add("valuable_items");
            }

            return modifiers;
        }

        private class NpcTrustConfig
        {
            public string NpcId { get; set; } = string.Empty;
            public double BaseDeceptionChance { get; set; }
            public List<string> SensitiveTopics { get; set; } = new();
        }
    }
}
