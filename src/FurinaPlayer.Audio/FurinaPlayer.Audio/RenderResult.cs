using System;

namespace FurinaPlayer.Audio;

public sealed record RenderResult(float[] Samples, int SampleRate, int Channels, TimeSpan Duration);
