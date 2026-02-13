namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Gesture recognition profile.
/// </summary>
public class GestureProfile
{
    public string ProfileId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public List<GestureDefinition> Gestures { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Gesture profile creation request.
/// </summary>
public class GestureProfileRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public List<GestureDefinitionRequest> Gestures { get; set; } = new();
}

/// <summary>
/// Gesture definition.
/// </summary>
public class GestureDefinition
{
    public string GestureId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<GestureInput> Inputs { get; set; } = new();
    public float Sensitivity { get; set; }
    public string ActionBinding { get; set; } = string.Empty;
}

/// <summary>
/// Gesture definition creation request.
/// </summary>
public class GestureDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<GestureInput> Inputs { get; set; } = new();
    public float Sensitivity { get; set; } = 0.5f;
    public string ActionBinding { get; set; } = string.Empty;
}

/// <summary>
/// Gesture input data point.
/// </summary>
public class GestureInput
{
    public float Timestamp { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Pressure { get; set; }
}

/// <summary>
/// Gesture recognition result.
/// </summary>
public class GestureRecognition
{
    public string GestureId { get; set; } = string.Empty;
    public string GestureName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public float MatchQuality { get; set; }
    public DateTime RecognizedAt { get; set; }
}
