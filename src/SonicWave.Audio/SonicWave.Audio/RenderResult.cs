using System;

namespace SonicWave.Audio;

public sealed record RenderResult(float[] Samples, int SampleRate, int Channels, TimeSpan Duration);
