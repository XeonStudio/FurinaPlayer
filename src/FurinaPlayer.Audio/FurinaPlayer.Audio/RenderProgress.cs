using System;

namespace FurinaPlayer.Audio;

public readonly record struct RenderProgress(double Fraction, TimeSpan Remaining);
