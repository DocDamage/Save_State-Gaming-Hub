using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

public enum ScreenFilterType
{
    CRT,
    Scanlines,
    Bloom
}

public sealed class ScreenFilterConfig
{
    public ScreenFilterType FilterType { get; init; }
    public float Intensity { get; init; }
}

public sealed class AudioMixConfig
{
    public float Volume { get; init; } = 1f;
    public float Bass { get; init; } = 0f;
    public float Treble { get; init; } = 0f;
}

public sealed class AudioAnalysis
{
    public double Duration { get; init; }
    public double PeakLevelDb { get; init; }
    public double RmsLevelDb { get; init; }
    public LoudnessInfo Loudness { get; init; } = new();
}

public sealed class LoudnessInfo
{
    public double Integrated { get; init; }
}
