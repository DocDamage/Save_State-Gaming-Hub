using System;

namespace SaveState.Application.Mugen.Services.Educational;

/// <summary>
/// Educational-specific types.
/// </summary>
public static class EducationalTypes
{
    /// <summary>
    /// Educational progress update data.
/// </summary>
    public record ProgressUpdate(
        int CurrentStep,
        int TotalSteps,
        double CompletionPercentage);
}