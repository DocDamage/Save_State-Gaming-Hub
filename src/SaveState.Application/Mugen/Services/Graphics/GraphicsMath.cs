namespace SaveState.Application.Mugen.Services.Graphics;

/// <summary>
/// 2D vector for graphics operations.
/// </summary>
public class GraphicsVector2
{
    public GraphicsVector2() { }
    public GraphicsVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// 3D vector for graphics operations.
/// </summary>
public class GraphicsVector3
{
    public GraphicsVector3() { }
    public GraphicsVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public static implicit operator Mugen.Vector3(GraphicsVector3 v)
        => new Mugen.Vector3(v.X, v.Y, v.Z);

    public static implicit operator GraphicsVector3(Mugen.Vector3 v)
        => new GraphicsVector3((float)v.X, (float)v.Y, (float)v.Z);
}

/// <summary>
/// Color representation for graphics.
/// </summary>
public class GraphicsColor
{
    public GraphicsColor() { }
    public GraphicsColor(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
}
