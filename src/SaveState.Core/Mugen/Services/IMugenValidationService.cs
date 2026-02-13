using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service interface for validating MUGEN moves and characters.
/// Ensures moves meet balance and technical requirements.
/// </summary>
public interface IMugenValidationService
{
    /// <summary>
    /// Validates a move definition for correctness and balance.
    /// </summary>
    /// <param name="move">The move to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<Result<ValidationResult>> ValidateMoveAsync(MugenMoveDefinition move, ValidationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a complete character definition.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<Result<ValidationResult>> ValidateCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates frame data for a move.
    /// </summary>
    /// <param name="frameData">The frame data to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<Result<ValidationResult>> ValidateFrameDataAsync(FrameData frameData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates hitbox definitions for overlaps and issues.
    /// </summary>
    /// <param name="hitboxes">The hitboxes to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    Task<Result<ValidationResult>> ValidateHitboxesAsync(IReadOnlyList<Hitbox> hitboxes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a move is balanced according to game rules.
    /// </summary>
    /// <param name="move">The move to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if balanced, false otherwise.</returns>
    Task<Result<bool>> IsMoveBalancedAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested fixes for validation errors.
    /// </summary>
    /// <param name="validationResult">The validation result with errors.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of suggested fixes.</returns>
    Task<Result<IReadOnlyList<string>>> GetSuggestedFixesAsync(ValidationResult validationResult, CancellationToken cancellationToken = default);
}
