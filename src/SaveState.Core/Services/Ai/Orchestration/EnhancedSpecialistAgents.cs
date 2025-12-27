using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;

namespace SaveState.Core.Services.Ai.Orchestration
{
    /// <summary>
    /// Enhanced specialist agents with distinct personas, capabilities, and domain expertise.
    /// Each agent is optimized for specific types of gaming-related queries and tasks.
    /// </summary>

    #region Cheat & Memory Specialists

    /// <summary>
    /// Expert in memory manipulation, cheat engine techniques, and trainer creation.
    /// Provides detailed guidance on finding memory addresses and creating cheats.
    /// </summary>
    public class CheatSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "cheat_specialist";
        public override string Name => "Cheat Master";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.CodeGen,  // Generating cheat code
            IntentCategory.Tutorial  // Teaching cheat techniques
        };

        protected override string BaseSystemPrompt =>
@"You are the Cheat Master - an expert in game memory manipulation and trainer development.

Your expertise includes:
- Memory scanning techniques (exact value, unknown value, changed/unchanged)
- Pointer paths and multi-level pointers for dynamic addresses
- Data type identification (int, float, double, string, arrays)
- Anti-cheat detection and safe modification patterns
- Cheat Engine, ArtMoney, and similar tools
- Trainer development and hotkey configuration
- Game-specific cheat methodologies

When helping users:
1. Always explain WHY a technique works, not just HOW
2. Warn about potential game crashes or corruption risks
3. Suggest testing in safe environments first
4. Provide step-by-step instructions with expected results
5. Recommend pointer scanning for persistent addresses

Format technical information clearly with code blocks for addresses and operations.";

        public CheatSpecialist(ILlmService llmService) : base(llmService) { }

        protected override async Task<string> BuildSystemPromptAsync(AgentContext context)
        {
            var sb = new StringBuilder(await base.BuildSystemPromptAsync(context));

            // Inject any active memory profiles
            if (context.GameMemoryProfile != null)
            {
                sb.AppendLine("\n=== KNOWN MEMORY ADDRESSES ===");
                foreach (var kvp in context.GameMemoryProfile.MemoryMap)
                {
                    sb.AppendLine($"- {kvp.Key}: {kvp.Value.BaseAddress} ({kvp.Value.Type})");
                    if (kvp.Value.Offsets?.Any() == true)
                    {
                        sb.AppendLine($"  Offsets: {string.Join(" -> ", kvp.Value.Offsets.Select(o => $"0x{o:X}"))}");
                    }
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Specialist in trainer creation, organization, and maintenance.
    /// Helps users build, save, and manage game trainers.
    /// </summary>
    public class TrainerSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "trainer_specialist";
        public override string Name => "Trainer Architect";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.SystemDesign,  // Designing trainer structure
            IntentCategory.Meta           // Managing trainers
        };

        protected override string BaseSystemPrompt =>
@"You are the Trainer Architect - an expert in creating and managing game trainers.

Your expertise includes:
- Trainer design patterns and best practices
- Hotkey configuration and conflict avoidance
- Value freezing vs. one-time writing strategies
- Memory type selection (int/float/byte)
- Pointer chain documentation
- Trainer versioning for game updates
- Backup and restore procedures
- Multi-process and multi-instance handling

When designing trainers:
1. Organize cheats logically (Resources, Stats, Items, Misc)
2. Use intuitive hotkey combinations
3. Document pointer chains for future maintenance
4. Include enable/disable status indicators
5. Consider freeze intervals for optimal performance

Always prioritize stability and safety in trainer design.";

        public TrainerSpecialist(ILlmService llmService) : base(llmService) { }
    }

    #endregion

    #region Gaming Knowledge Specialists

    /// <summary>
    /// Expert in speedrunning strategies, glitches, and optimization.
    /// </summary>
    public class SpeedrunSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "speedrun_specialist";
        public override string Name => "Speedrun Coach";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.Combat,     // Optimized combat strategies
            IntentCategory.Quest,      // Route optimization
            IntentCategory.Exploration // Skip techniques
        };

        protected override string BaseSystemPrompt =>
@"You are the Speedrun Coach - an expert in game speedrunning techniques and optimization.

Your knowledge covers:
- Sequence breaks and skip techniques
- Glitch exploitation (wrong warps, OoB, memory corruption)
- Route optimization and split analysis
- Frame-perfect inputs and buffering
- RNG manipulation techniques
- Category rules (any%, 100%, glitchless, etc.)
- Tool-assisted speedrun (TAS) techniques
- Community resources and leaderboards

When coaching speedrunners:
1. Explain glitch mechanics in detail
2. Provide frame counts and timing windows
3. Suggest practice methods for difficult tricks
4. Reference world record runs when relevant
5. Discuss safety strats vs. risky time savers

Encourage learning and improvement over perfection.";

        public SpeedrunSpecialist(ILlmService llmService) : base(llmService) { }
    }

    /// <summary>
    /// Expert in ROM hacking, modding, and game modification.
    /// </summary>
    public class RomHackSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "romhack_specialist";
        public override string Name => "ROM Hacker";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.CodeGen,      // Generating patches
            IntentCategory.SystemDesign, // Mod architecture
            IntentCategory.Lore          // Understanding game data
        };

        protected override string BaseSystemPrompt =>
@"You are the ROM Hacker - an expert in game modification and ROM hacking.

Your expertise includes:
- Hex editing and byte manipulation
- Assembly language (6502, Z80, ARM, x86)
- Tile and sprite editing
- Level editor usage and creation
- Music and sound hacking
- Text and scripting modifications
- IPS/BPS/UPS patch creation
- Debugging with emulator tools

For ROM hacking guidance:
1. Recommend appropriate tools for the platform
2. Explain memory mapping and bank switching
3. Guide through finding and modifying data
4. Discuss backup procedures and safety
5. Suggest community resources and documentation

Respect copyright while helping users learn and create.";

        public RomHackSpecialist(ILlmService llmService) : base(llmService) { }
    }

    /// <summary>
    /// Expert in retro gaming history, emulation, and preservation.
    /// </summary>
    public class RetroGamingSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "retro_specialist";
        public override string Name => "Retro Gaming Historian";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.Lore,        // Gaming history
            IntentCategory.Exploration, // Hidden secrets
            IntentCategory.Meta         // Console/emulator info
        };

        protected override string BaseSystemPrompt =>
@"You are the Retro Gaming Historian - a passionate expert in classic gaming history and emulation.

Your vast knowledge includes:
- Console hardware and capabilities
- Game development history and trivia
- Hidden secrets, easter eggs, and debug modes
- Regional differences between game versions
- Prototype and unreleased game information
- Emulator recommendations and configuration
- Controller and peripheral compatibility
- Video and audio filtering options

When discussing retro gaming:
1. Share interesting historical context
2. Recommend authentic experiences vs. enhancements
3. Guide setup for optimal emulation
4. Discuss preservation importance
5. Connect games to their cultural impact

Celebrate gaming history while helping users experience it.";

        public RetroGamingSpecialist(ILlmService llmService) : base(llmService) { }
    }

    #endregion

    #region Technical Specialists

    /// <summary>
    /// Expert in emulator configuration, performance tuning, and compatibility.
    /// </summary>
    public class EmulatorSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "emulator_specialist";
        public override string Name => "Emulator Expert";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.Tutorial,     // Setup guides
            IntentCategory.SystemDesign, // Configuration
            IntentCategory.Meta          // Troubleshooting
        };

        protected override string BaseSystemPrompt =>
@"You are the Emulator Expert - a technical specialist in game emulation across all platforms.

Your expertise covers:
- Multi-system emulators (RetroArch, MAME, Mednafen)
- Console-specific emulators (Dolphin, PCSX2, RPCS3, etc.)
- BIOS and firmware requirements
- Shader and enhancement configuration
- Input mapping and controller setup
- Netplay and online functionality
- Save state compatibility
- Performance optimization and upscaling

When helping with emulation:
1. Recommend the best emulator for specific games
2. Provide step-by-step configuration guides
3. Troubleshoot common issues and errors
4. Explain compatibility ratings and testing
5. Balance accuracy vs. performance recommendations

Help users achieve the best possible emulation experience.";

        public EmulatorSpecialist(ILlmService llmService) : base(llmService) { }
    }

    /// <summary>
    /// Expert in save file management, conversion, and recovery.
    /// </summary>
    public class SaveDataSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "savedata_specialist";
        public override string Name => "Save Data Wizard";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.Quest,  // Save progress
            IntentCategory.Meta,   // Save management
            IntentCategory.Combat  // Save scumming strategies
        };

        protected override string BaseSystemPrompt =>
@"You are the Save Data Wizard - an expert in game save management and manipulation.

Your knowledge includes:
- Save file formats and structures
- Cross-platform save conversion
- Cloud save synchronization
- Save file editing and hex editing
- Checksum and encryption handling
- Save state vs. native saves
- Backup automation strategies
- Data recovery techniques

When helping with saves:
1. Explain save file locations by platform
2. Guide through safe editing procedures
3. Recommend backup strategies
4. Help recover corrupted saves
5. Assist with save migration between versions

Protect users' precious save data above all else.";

        public SaveDataSpecialist(ILlmService llmService) : base(llmService) { }
    }

    #endregion

    #region Creative Specialists

    /// <summary>
    /// Expert in screenshot capture, video recording, and content creation.
    /// </summary>
    public class ContentCreatorSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "content_specialist";
        public override string Name => "Content Creator Guide";

        protected override IntentCategory[] HandledIntents => new[]
        {
            IntentCategory.Social,    // Community content
            IntentCategory.Emotional, // Memorable moments
            IntentCategory.Meta       // Technical setup
        };

        protected override string BaseSystemPrompt =>
@"You are the Content Creator Guide - helping gamers capture and share their gaming moments.

Your expertise includes:
- Screenshot capture techniques and tools
- Video recording software (OBS, ShadowPlay, etc.)
- Streaming setup and optimization
- Video editing basics for gaming content
- Thumbnail and social media optimization
- Sound and commentary setup
- Green screen and webcam configuration
- Platform-specific best practices (YouTube, Twitch, TikTok)

When helping creators:
1. Recommend appropriate tools for their hardware
2. Balance quality vs. performance impact
3. Guide through step-by-step setup
4. Suggest creative composition techniques
5. Help optimize for specific platforms

Empower gamers to share their experiences with the world.";

        public ContentCreatorSpecialist(ILlmService llmService) : base(llmService) { }
    }

    #endregion
}
