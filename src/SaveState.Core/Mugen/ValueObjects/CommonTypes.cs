namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Common 2D position using integers (pixels).
/// </summary>
public sealed record Position(int X, int Y);

/// <summary>
/// Common 2D size using integers (pixels).
/// </summary>
public sealed record Size(int Width, int Height);

/// <summary>
/// Common 2D velocity using integers.
/// </summary>
public sealed record Velocity(int X, int Y);

/// <summary>
/// Common 2D acceleration using integers.
/// </summary>
public sealed record Acceleration(int X, int Y);

/// <summary>
/// Common rectangle bounds using integers.
/// </summary>
public sealed record Rectangle(int X, int Y, int Width, int Height);
