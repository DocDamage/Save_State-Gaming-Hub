using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.Educational;

/// <summary>
/// Engine for interactive tutorial processing.
/// </summary>
public class TutorialEngine
{
    private readonly ILogger<TutorialEngine> _logger;

    public TutorialEngine(ILogger<TutorialEngine> logger)
    {
        _logger = logger;
    }

    public Task<bool> ValidateStepAsync(string stepId, string action, CancellationToken ct = default)
    {
        _logger.LogDebug("Validating step {StepId} with action {Action}", stepId, action);
        return Task.FromResult(true);
    }

    public Task<string> GenerateFeedbackAsync(string stepId, bool isCorrect, CancellationToken ct = default)
    {
        var feedback = isCorrect
            ? "Correct! Great job!"
            : "Not quite. Try again!";
        return Task.FromResult(feedback);
    }

    public Task<string?> GenerateHintAsync(string stepId, CancellationToken ct = default)
    {
        return Task.FromResult<string?>("Hint: Check the instructions carefully.");
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class EducationalContentServiceTutorialEngine : TutorialEngine
{
    public EducationalContentServiceTutorialEngine(ILogger<TutorialEngine> logger) : base(logger) { }
}
