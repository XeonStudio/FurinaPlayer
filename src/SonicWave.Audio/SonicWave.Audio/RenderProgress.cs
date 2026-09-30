using System;

namespace SonicWave.Audio;

public readonly record struct RenderProgress(double Fraction, TimeSpan Remaining);
