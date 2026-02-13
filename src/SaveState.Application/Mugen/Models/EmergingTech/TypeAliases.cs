// Type aliases for backward compatibility with existing code
// These aliases allow the refactored code to work with existing references

namespace SaveState.Application.Mugen.Services;

using SaveState.Application.Mugen.Models.EmergingTech;

// Model type aliases
public class EmergingTechnologiesServiceMotionController : MotionController { }
public class EmergingTechnologiesServiceMotionControllerRegistration : MotionControllerRegistration { }
public class EmergingTechnologiesServiceRawMotionData : RawMotionData { }
public class EmergingTechnologiesServiceMotionData : MotionData { }
public class EmergingTechnologiesServiceMotionGesture : MotionGesture { }
public class EmergingTechnologiesServiceMotionCalibration : MotionCalibration { }
public class EmergingTechnologiesServiceMotionSensitivity : MotionSensitivity { }
public class EmergingTechnologiesServiceMotionCalibrationResult : MotionCalibrationResult { }
public class EmergingTechnologiesServiceCalibrationSequence : CalibrationSequence { }
public class EmergingTechnologiesServiceCalibrationStep : CalibrationStep { }

public class EmergingTechnologiesServiceHapticDevice : HapticDevice { }
public class EmergingTechnologiesServiceHapticDeviceRegistration : HapticDeviceRegistration { }
public class EmergingTechnologiesServiceHapticActuator : HapticActuator { }
public class EmergingTechnologiesServiceHapticActuatorConfig : HapticActuatorConfig { }
public class EmergingTechnologiesServiceHapticFeedbackRequest : HapticFeedbackRequest { }
public class EmergingTechnologiesServiceHapticPattern : HapticPattern { }
public class EmergingTechnologiesServiceHapticPatternRequest : HapticPatternRequest { }
public class EmergingTechnologiesServiceHapticSequence : HapticSequence { }

public class EmergingTechnologiesServiceGestureProfile : GestureProfile { }
public class EmergingTechnologiesServiceGestureProfileRequest : GestureProfileRequest { }
public class EmergingTechnologiesServiceGestureDefinition : GestureDefinition { }
public class EmergingTechnologiesServiceGestureDefinitionRequest : GestureDefinitionRequest { }
public class EmergingTechnologiesServiceGestureInput : GestureInput { }
public class EmergingTechnologiesServiceGestureRecognition : GestureRecognition { }

public class EmergingTechnologiesServiceBiometricInput : BiometricInput { }
public class EmergingTechnologiesServiceBiometricData : BiometricData { }
public class EmergingTechnologiesServiceEyeTrackingInput : EyeTrackingInput { }
public class EmergingTechnologiesServiceEyeTrackingData : EyeTrackingData { }
public class EmergingTechnologiesServiceEyeData : EyeData { }
public class EmergingTechnologiesServiceBrainwaveInput : BrainwaveInput { }
public class EmergingTechnologiesServiceBrainwaveData : BrainwaveData { }
public class EmergingTechnologiesServiceUserContext : UserContext { }

public class EmergingTechnologiesServiceAdaptiveInterface : AdaptiveInterface { }
public class EmergingTechnologiesServiceAdaptiveLayout : AdaptiveLayout { }
public class EmergingTechnologiesServiceAdaptiveControl : AdaptiveControl { }
public class EmergingTechnologiesServiceAdaptiveFeedback : AdaptiveFeedback { }
public class EmergingTechnologiesServiceVisualFeedback : VisualFeedback { }
public class EmergingTechnologiesServiceAudioFeedback : AudioFeedback { }
public class EmergingTechnologiesServiceVrHapticFeedback : VrHapticFeedback { }
public class EmergingTechnologiesServiceVrAccessibilitySettings : VrAccessibilitySettings { }
public class EmergingTechnologiesServiceAccessibilityFeatures : AccessibilityFeatures { }

// Struct type aliases (using implicit conversion operators for compatibility)
public struct EmergingTechnologiesServiceEmergingTechVector2
{
    public float X { get; set; }
    public float Y { get; set; }
    public EmergingTechnologiesServiceEmergingTechVector2() { X = 0; Y = 0; }
    public EmergingTechnologiesServiceEmergingTechVector2(float x, float y) { X = x; Y = y; }
    public static implicit operator TechVector2(EmergingTechnologiesServiceEmergingTechVector2 v) => new(v.X, v.Y);
    public static implicit operator EmergingTechnologiesServiceEmergingTechVector2(TechVector2 v) => new(v.X, v.Y);
}

public struct EmergingTechnologiesServiceTechVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public EmergingTechnologiesServiceTechVector3() { X = 0; Y = 0; Z = 0; }
    public EmergingTechnologiesServiceTechVector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    public static implicit operator TechVector3(EmergingTechnologiesServiceTechVector3 v) => new(v.X, v.Y, v.Z);
    public static implicit operator EmergingTechnologiesServiceTechVector3(TechVector3 v) => new(v.X, v.Y, v.Z);
}

public struct EmergingTechnologiesServiceQuaternion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }
    public EmergingTechnologiesServiceQuaternion() { X = 0; Y = 0; Z = 0; W = 1; }
    public EmergingTechnologiesServiceQuaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    public static implicit operator TechQuaternion(EmergingTechnologiesServiceQuaternion v) => new(v.X, v.Y, v.Z, v.W);
    public static implicit operator EmergingTechnologiesServiceQuaternion(TechQuaternion v) => new(v.X, v.Y, v.Z, v.W);
}

public struct EmergingTechnologiesServiceFrequencyRange
{
    public float Min { get; set; }
    public float Max { get; set; }
    public EmergingTechnologiesServiceFrequencyRange() { Min = 0; Max = 100; }
    public EmergingTechnologiesServiceFrequencyRange(float min, float max) { Min = min; Max = max; }
    public static implicit operator FrequencyRange(EmergingTechnologiesServiceFrequencyRange v) => new(v.Min, v.Max);
    public static implicit operator EmergingTechnologiesServiceFrequencyRange(FrequencyRange v) => new(v.Min, v.Max);
}

// Engine type aliases
// Engine aliases removed - use Engines namespace directly
