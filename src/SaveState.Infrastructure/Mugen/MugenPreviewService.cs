using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using IMugenPreviewService = SaveState.Core.Mugen.Services.IMugenPreviewService;
using MoveProperties = SaveState.Core.Mugen.ValueObjects.MoveProperties;
using Position = SaveState.Core.Mugen.ValueObjects.Position;
using Hitbox = SaveState.Core.Mugen.ValueObjects.Hitbox;
using Hurtbox = SaveState.Core.Mugen.ValueObjects.Hurtbox;
using Projectile = SaveState.Core.Mugen.ValueObjects.Projectile;
using ParticleEffect = SaveState.Core.Mugen.ValueObjects.ParticleEffect;
using HitboxType = SaveState.Core.Mugen.ValueObjects.HitboxType;
using HurtboxType = SaveState.Core.Mugen.ValueObjects.HurtboxType;

namespace SaveState.Infrastructure.Mugen;

/// <summary>
/// Service for generating move preview data.
/// Creates frame-by-frame visualization data for move timing and hitbox display.
/// </summary>
public class MugenPreviewService : IMugenPreviewService
{
    private readonly ILogger<MugenPreviewService> _logger;

    public MugenPreviewService(ILogger<MugenPreviewService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<MovePreviewData>> GeneratePreviewAsync(
        MugenMoveDefinition move,
        PreviewOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating preview for move '{MoveName}' from frame {Start} to {End}",
                move.Name, options.StartFrame, options.EndFrame);

            var frames = new List<PreviewFrame>();
            var orderedStates = move.States.OrderBy(s => s.StateNumber).ToArray();

            var currentFrame = options.StartFrame;
            var maxFrame = Math.Min(options.EndFrame, move.TotalDuration);

            // Generate frames for each state
            var stateStartFrame = 0;
            foreach (var state in orderedStates)
            {
                for (var frameOffset = 0; frameOffset < state.Duration; frameOffset++)
                {
                    var absoluteFrame = stateStartFrame + frameOffset;

                    if (absoluteFrame < options.StartFrame || absoluteFrame > maxFrame)
                        continue;

                    if (ct.IsCancellationRequested)
                        break;

                    var frame = GenerateFrame(state, absoluteFrame, frameOffset, move, options);
                    frames.Add(frame);
                }

                stateStartFrame += state.Duration;
                if (stateStartFrame > maxFrame)
                    break;
            }

            var previewData = new MovePreviewData(
                Frames: frames,
                Properties: MapPropertiesForPreview(move.Properties),
                Metadata: new Dictionary<string, string>
                {
                    ["total_frames"] = move.TotalDuration.ToString(),
                    ["startup_frames"] = move.Properties.StartupFrames.ToString(),
                    ["active_frames"] = move.Properties.ActiveFrames.ToString(),
                    ["recovery_frames"] = move.Properties.RecoveryFrames.ToString(),
                    ["damage"] = move.Properties.Damage.ToString(),
                    ["move_type"] = move.MoveType.ToString(),
                    ["preview_quality"] = options.Quality.ToString()
                });

            _logger.LogInformation("Generated {FrameCount} preview frames for move '{MoveName}'",
                frames.Count, move.Name);

            return Result.Success(previewData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for move '{MoveName}'", move.Name);
            return Result.Failure<MovePreviewData>($"Failed to generate preview: {ex.Message}");
        }
    }

    private PreviewFrame GenerateFrame(
        MoveState state,
        int absoluteFrame,
        int frameOffset,
        MugenMoveDefinition move,
        PreviewOptions options)
    {
        // Determine sprite based on animation
        var sprite = GetSpriteForFrame(state, frameOffset, options.Quality);

        // Calculate position based on velocity and acceleration
        var position = CalculatePosition(state, frameOffset);

        // Get hitboxes for this frame (only during active frames)
        var hitboxes = new List<Hitbox>();
        if (options.ShowHitboxes && IsActiveFrame(move, absoluteFrame))
        {
            hitboxes = state.Hitboxes.Where(h => IsHitboxActive(h, frameOffset)).ToList();
        }

        // Get hurtboxes
        var hurtboxes = new List<Hurtbox>();
        if (options.ShowHurtboxes)
        {
            hurtboxes = state.Hurtboxes.ToList();
        }

        // Get projectiles
        var projectiles = new List<Projectile>();
        if (options.ShowProjectiles)
        {
            projectiles = state.Projectiles.Where(p => IsProjectileActive(p, frameOffset)).ToList();
        }

        // Get effects
        var effects = new List<ParticleEffect>();
        if (options.ShowEffects)
        {
            effects = state.Effects.Where(e => IsEffectActive(e, frameOffset)).ToList();
        }

        return new PreviewFrame(
            FrameNumber: absoluteFrame,
            Sprite: sprite,
            Position: position,
            Hitboxes: hitboxes,
            Hurtboxes: hurtboxes,
            Projectiles: projectiles,
            Effects: effects);
    }

    private string GetSpriteForFrame(MoveState state, int frameOffset, PreviewQuality quality)
    {
        // In a real implementation, this would interpolate between animation frames
        // For now, return the base sprite
        return $"{state.SpriteGroup},{state.SpriteNumber}";
    }

    private Position CalculatePosition(MoveState state, int frameOffset)
    {
        // Simple position calculation based on velocity and acceleration
        var velocity = state.Properties.Velocity;
        var acceleration = state.Properties.Acceleration;
        var baseX = state.Position.X;
        var baseY = state.Position.Y;

        var x = baseX + velocity.X * frameOffset + 0.5 * acceleration.X * frameOffset * frameOffset;
        var y = baseY + velocity.Y * frameOffset + 0.5 * acceleration.Y * frameOffset * frameOffset;

        return new Position((int)x, (int)y);
    }

    private bool IsActiveFrame(MugenMoveDefinition move, int frameNumber)
    {
        // Active frames are after startup but before recovery ends
        return frameNumber >= move.Properties.StartupFrames &&
               frameNumber < (move.Properties.StartupFrames + move.Properties.ActiveFrames);
    }

    private bool IsHitboxActive(Hitbox hitbox, int frameOffset)
    {
        // Hitboxes are active during their lifetime
        // This is a simplified implementation - real MUGEN would have more complex timing
        return true;
    }

    private bool IsProjectileActive(Projectile projectile, int frameOffset)
    {
        // Projectiles are active from creation until they expire or are destroyed
        return frameOffset >= 0 && frameOffset < projectile.Time;
    }

    private bool IsEffectActive(ParticleEffect effect, int frameOffset)
    {
        // Effects are active for their duration
        return frameOffset >= 0 && frameOffset < effect.Duration;
    }

    private MoveProperties MapPropertiesForPreview(MoveProperties value)
    {
        return value;
    }

    public async Task<Result<string>> GenerateThumbnailAsync(MugenMoveDefinition move, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating thumbnail for move '{MoveName}'", move.Name);
        return await Task.FromResult(Result.Success("base64_thumbnail_placeholder"));
    }

    public async Task<Result<bool>> ValidatePreviewAssetsAsync(MugenMoveDefinition move, CancellationToken ct = default)
    {
        _logger.LogInformation("Validating preview assets for move '{MoveName}'", move.Name);
        return await Task.FromResult(Result.Success(true));
    }

    /// <summary>
    /// Generates a timing diagram for the move.
    /// </summary>
    public async Task<Result<string>> GenerateTimingDiagramAsync(
        MugenMoveDefinition move,
        CancellationToken ct = default)
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("Move Timing Diagram");
            sb.AppendLine("==================");
            sb.AppendLine();

            sb.AppendLine($"Total Duration: {move.TotalDuration} frames");
            sb.AppendLine($"Startup: {move.Properties.StartupFrames} frames");
            sb.AppendLine($"Active: {move.Properties.ActiveFrames} frames");
            sb.AppendLine($"Recovery: {move.Properties.RecoveryFrames} frames");
            sb.AppendLine();

            // ASCII timing diagram
            var timeline = new char[move.TotalDuration + 1];
            Array.Fill(timeline, '-');

            // Mark startup frames
            for (int i = 0; i < move.Properties.StartupFrames && i < timeline.Length; i++)
            {
                timeline[i] = 'S';
            }

            // Mark active frames
            for (int i = move.Properties.StartupFrames;
                 i < move.Properties.StartupFrames + move.Properties.ActiveFrames && i < timeline.Length;
                 i++)
            {
                timeline[i] = 'A';
            }

            // Mark recovery frames
            for (int i = move.Properties.StartupFrames + move.Properties.ActiveFrames;
                 i < timeline.Length;
                 i++)
            {
                timeline[i] = 'R';
            }

            sb.AppendLine("Timeline:");
            sb.AppendLine("S = Startup, A = Active, R = Recovery, - = Inactive");
            sb.AppendLine();

            // Print timeline in chunks
            const int chunkSize = 50;
            for (int i = 0; i < timeline.Length; i += chunkSize)
            {
                var chunk = new string(timeline.Skip(i).Take(chunkSize).ToArray());
                sb.AppendLine($"{i,3}: {chunk}");
            }

            sb.AppendLine();
            sb.AppendLine("Frame Data:");
            sb.AppendLine($"- Frame Advantage on Hit: {move.Properties.FrameAdvantageOnHit:+#;-#;0}");
            sb.AppendLine($"- Frame Advantage on Block: {move.Properties.FrameAdvantageOnBlock:+#;-#;0}");
            sb.AppendLine($"- Hit Stun: {move.Properties.HitStun}");
            sb.AppendLine($"- Block Stun: {move.Properties.BlockStun}");

            return Result.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating timing diagram for move '{MoveName}'", move.Name);
            return Result.Failure<string>($"Failed to generate timing diagram: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates hitbox visualization data.
    /// </summary>
    public async Task<Result<IReadOnlyList<HitboxVisualization>>> GenerateHitboxVisualizationAsync(
        MugenMoveDefinition move,
        CancellationToken ct = default)
    {
        try
        {
            var visualizations = new List<HitboxVisualization>();

            foreach (var state in move.States)
            {
                foreach (var hitbox in state.Hitboxes)
                {
                    visualizations.Add(new HitboxVisualization(
                        StateNumber: state.StateNumber,
                        Hitbox: hitbox,
                        Color: GetHitboxColor(hitbox.Type),
                        Label: $"{hitbox.Type} Hitbox ({hitbox.Bounds.Width}x{hitbox.Bounds.Height})"));
                }

                foreach (var hurtbox in state.Hurtboxes)
                {
                    visualizations.Add(new HitboxVisualization(
                        StateNumber: state.StateNumber,
                        Hitbox: null, // Hurtboxes don't have Hitbox properties
                        Hurtbox: hurtbox,
                        Color: GetHurtboxColor(hurtbox.Type),
                        Label: $"{hurtbox.Type} Hurtbox ({hurtbox.Bounds.Width}x{hurtbox.Bounds.Height})"));
                }

                foreach (var projectile in state.Projectiles)
                {
                    visualizations.Add(new HitboxVisualization(
                        StateNumber: state.StateNumber,
                        Hitbox: null,
                        Hurtbox: null,
                        Projectile: projectile,
                        Color: "#FF6B35", // Orange for projectiles
                        Label: $"Projectile {projectile.Id} ({projectile.Damage} dmg)"));
                }
            }

            return Result.Success<IReadOnlyList<HitboxVisualization>>(visualizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hitbox visualization for move '{MoveName}'", move.Name);
            return Result.Failure<IReadOnlyList<HitboxVisualization>>($"Failed to generate hitbox visualization: {ex.Message}");
        }
    }

    private string GetHitboxColor(HitboxType type)
    {
        return type switch
        {
            HitboxType.Attack => "#FF0000",      // Red
            HitboxType.Projectile => "#FFA500",  // Orange
            HitboxType.Throw => "#800080",      // Purple
            _ => "#FF0000"
        };
    }

    private string GetHurtboxColor(HurtboxType type)
    {
        return type switch
        {
            HurtboxType.Body => "#00FF00",      // Green
            HurtboxType.Head => "#0000FF",      // Blue
            HurtboxType.Legs => "#FFFF00",      // Yellow
            HurtboxType.Projectile => "#FF69B4", // Pink
            _ => "#00FF00"
        };
    }
}

/// <summary>
/// Hitbox visualization data.
/// </summary>
public sealed record HitboxVisualization(
    int StateNumber,
    Hitbox? Hitbox,
    Hurtbox? Hurtbox = null,
    Projectile? Projectile = null,
    string Color = "#000000",
    string Label = "");
