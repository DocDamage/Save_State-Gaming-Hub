using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Repository interface for managing MUGEN move templates.
/// Provides access to pre-built move templates for character creation.
/// </summary>
public interface IMugenTemplateRepository
{
    /// <summary>
    /// Gets move templates, optionally filtered by category.
    /// </summary>
    /// <param name="category">The template category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of move templates.</returns>
    Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesAsync(MoveCategory? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available move templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of move templates.</returns>
    Task<Result<IReadOnlyList<MoveTemplate>>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific move template by ID.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The move template.</returns>
    Task<Result<MoveTemplate>> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates filtered by category name.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of templates.</returns>
    Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates filtered by difficulty level.
    /// </summary>
    /// <param name="difficulty">The difficulty level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of templates.</returns>
    Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesByDifficultyAsync(DifficultyLevel difficulty, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new custom template.
    /// </summary>
    /// <param name="template">The template to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved template.</returns>
    Task<Result<MoveTemplate>> SaveTemplateAsync(MoveTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result<bool>> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
}
