using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Infrastructure.Mugen;

/// <summary>
/// Repository for MUGEN move templates. Simplified to align with current value object shapes.
/// </summary>
public class MugenTemplateRepository : IMugenTemplateRepository
{
    private readonly ILogger<MugenTemplateRepository> _logger;
    private readonly IReadOnlyList<MoveTemplate> _templates;

    public MugenTemplateRepository(ILogger<MugenTemplateRepository> logger)
    {
        _logger = logger;
        _templates = CreateTemplates();
    }

    public Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesAsync(
        MoveCategory? category = null,
        CancellationToken ct = default)
    {
        try
        {
            var templates = category.HasValue
                ? _templates.Where(t => t.Category == category.Value).ToList()
                : _templates.ToList();

            _logger.LogInformation("Retrieved {Count} move templates for category {Category}",
                templates.Count, category?.ToString() ?? "all");

            return Task.FromResult(Result.Success<IReadOnlyList<MoveTemplate>>(templates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving move templates");
            return Task.FromResult(Result.Failure<IReadOnlyList<MoveTemplate>>($"Failed to get templates: {ex.Message}"));
        }
    }

    private IReadOnlyList<MoveTemplate> CreateTemplates()
    {
        // Minimal set of templates using current value object signatures.
        return new List<MoveTemplate>
        {
            CreateBasicTemplate(
                id: "fireball_basic",
                name: "Basic Fireball",
                description: "A standard projectile that travels horizontally.",
                category: MoveCategory.Special,
                type: MoveType.Special,
                difficulty: DifficultyLevel.Beginner),
            CreateBasicTemplate(
                id: "uppercut_basic",
                name: "Basic Uppercut",
                description: "A vertical strike with invincibility on startup.",
                category: MoveCategory.Special,
                type: MoveType.Special,
                difficulty: DifficultyLevel.Intermediate)
        };
    }

    private static MoveTemplate CreateBasicTemplate(
        string id,
        string name,
        string description,
        MoveCategory category,
        MoveType type,
        DifficultyLevel difficulty)
    {
        // MoveTemplate is a simple metadata class for move templates.
        // Detailed state/properties are defined when the template is applied to a character.
        return new MoveTemplate
        {
            Id = id,
            Name = name,
            Description = description,
            Category = category,
            Type = type,
            Difficulty = difficulty,
            Tags = new[] { "template", "basic" }
        };
    }
    public async Task<Result<IReadOnlyList<MoveTemplate>>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all move templates");
        return Result.Success(_templates);
    }

    public async Task<Result<MoveTemplate>> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving template {TemplateId}", templateId);
        var template = _templates.FirstOrDefault(); // Simplified
        return template != null ? Result.Success(template) : Result.Failure<MoveTemplate>("Template not found");
    }

    public async Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving templates for category {Category}", category);
        return Result.Success<IReadOnlyList<MoveTemplate>>(_templates);
    }

    public async Task<Result<IReadOnlyList<MoveTemplate>>> GetTemplatesByDifficultyAsync(DifficultyLevel difficulty, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving templates for difficulty {Difficulty}", difficulty);
        return Result.Success<IReadOnlyList<MoveTemplate>>(_templates);
    }

    public async Task<Result<MoveTemplate>> SaveTemplateAsync(MoveTemplate template, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving new template '{TemplateName}'", template.Name);
        return Result.Success(template);
    }

    public async Task<Result<bool>> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting template {TemplateId}", templateId);
        return Result.Success(true);
    }
}
