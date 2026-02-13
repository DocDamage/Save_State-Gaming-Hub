using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.Educational;

/// <summary>
/// Engine for generating educational content.
/// </summary>
public class ContentGenerator
{
    private readonly ILogger<ContentGenerator> _logger;

    public ContentGenerator(ILogger<ContentGenerator> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateTutorialContentAsync(string topic, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating tutorial content for topic: {Topic}", topic);
        return Task.FromResult($"Tutorial content for {topic}");
    }

    public Task<string> GenerateExerciseAsync(string topic, CancellationToken ct = default)
    {
        return Task.FromResult($"Practice exercise for {topic}");
    }

    public Task<string> GenerateExplanationAsync(string concept, CancellationToken ct = default)
    {
        return Task.FromResult($"Explanation of {concept}");
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class EducationalContentServiceContentGenerator : ContentGenerator
{
    public EducationalContentServiceContentGenerator(ILogger<ContentGenerator> logger) : base(logger) { }
}
