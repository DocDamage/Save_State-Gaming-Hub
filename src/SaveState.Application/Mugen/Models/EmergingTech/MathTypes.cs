namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// 2D vector for emerging tech calculations.
/// </summary>
public struct TechVector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public TechVector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// 3D vector for emerging tech calculations.
/// </summary>
public struct TechVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public TechVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>
/// Quaternion for rotation calculations.
/// </summary>
public struct TechQuaternion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public TechQuaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}

/// <summary>
/// Frequency range for haptic/audio calculations.
/// </summary>
public struct FrequencyRange
{
    public float Min { get; set; }
    public float Max { get; set; }

    public FrequencyRange(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
