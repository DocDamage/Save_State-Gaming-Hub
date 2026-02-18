namespace SaveState.Application.Mugen.Services.EmergingTechnologies.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.EmergingTech;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for recognizing and learning custom gestures.
/// </summary>
public class GestureRecognitionEngine
{
    private readonly ILogger<GestureRecognitionEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public GestureRecognitionEngine(ILogger<GestureRecognitionEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Recognizes a gesture from inputs.
    /// </summary>
    public Task<GestureRecognition?> RecognizeGestureAsync(
        GestureProfile profile,
        List<GestureInput> inputs,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Recognizing gesture from {InputCount} inputs for profile {ProfileId}",
            inputs.Count, profile.ProfileId);

        if (profile.Gestures.Count == 0 || inputs.Count == 0)
            return Task.FromResult<GestureRecognition?>(null);

        // Find best matching gesture
        GestureDefinition? bestMatch = null;
        var bestConfidence = 0.0f;

        foreach (var gesture in profile.Gestures)
        {
            var confidence = CalculateMatchConfidence(gesture, inputs);
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestMatch = gesture;
            }
        }

        if (bestMatch == null || bestConfidence < 0.6f)
            return Task.FromResult<GestureRecognition?>(null);

        var recognition = new GestureRecognition
        {
            GestureId = bestMatch.GestureId,
            GestureName = bestMatch.Name,
            Confidence = bestConfidence,
            MatchQuality = bestConfidence,
            RecognizedAt = _timeProvider.UtcNow
        };

        return Task.FromResult<GestureRecognition?>(recognition);
    }

    /// <summary>
    /// Learns a new gesture.
    /// </summary>
    public Task<bool> LearnGestureAsync(
        GestureProfile profile,
        string gestureName,
        List<GestureInput> inputs,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Learning gesture '{GestureName}' with {InputCount} inputs for profile {ProfileId}",
            gestureName, inputs.Count, profile.ProfileId);

        if (inputs.Count < 3)
        {
            _logger.LogWarning("Not enough inputs to learn gesture");
            return Task.FromResult(false);
        }

        var gesture = new GestureDefinition
        {
            GestureId = Guid.NewGuid().ToString(),
            Name = gestureName,
            Type = "Custom",
            Inputs = inputs,
            Sensitivity = 0.8f,
            ActionBinding = $"Custom_{gestureName}"
        };

        profile.Gestures.Add(gesture);
        profile.LastModified = _timeProvider.UtcNow;

        _logger.LogInformation("Gesture '{GestureName}' learned with ID {GestureId}", gestureName, gesture.GestureId);
        return Task.FromResult(true);
    }

    private static float CalculateMatchConfidence(GestureDefinition gesture, List<GestureInput> inputs)
    {
        if (gesture.Inputs.Count == 0 || inputs.Count == 0)
            return 0;

        // Simple matching: compare input sequences by comparing positions
        var matchedInputs = 0;

        for (int i = 0; i < Math.Min(gesture.Inputs.Count, inputs.Count); i++)
        {
            var gInput = gesture.Inputs[i];
            var input = inputs[i];
            var distance = MathF.Sqrt(
                MathF.Pow(gInput.X - input.X, 2) +
                MathF.Pow(gInput.Y - input.Y, 2) +
                MathF.Pow(gInput.Z - input.Z, 2)
            );

            if (distance < 0.5f) // Threshold for match
                matchedInputs++;
        }

        var confidence = (float)matchedInputs / Math.Max(gesture.Inputs.Count, inputs.Count);
        return confidence * gesture.Sensitivity;
    }
}
