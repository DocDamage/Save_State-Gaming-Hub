namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// 3D vector for positions.
/// </summary>
public class Vector3
{
    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
    public float Z { get; set; } = default!;

    /// <summary>
    /// Calculates the length (magnitude) of the vector.
    /// </summary>
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    /// <summary>
    /// Returns a normalized copy of this vector.
    /// </summary>
    public static Vector3 Normalize(Vector3 value)
    {
        var length = value.Length();
        if (length == 0)
            return new Vector3 { X = 0, Y = 0, Z = 0 };
        return new Vector3 { X = value.X / length, Y = value.Y / length, Z = value.Z / length };
    }
}
