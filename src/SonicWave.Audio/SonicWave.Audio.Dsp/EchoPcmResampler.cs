using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Backward-compatible alias for <see cref="FurinaPlayerPcmResampler"/>.
/// Provided for legacy call-sites and unit tests.
/// </summary>
public sealed class EchoPcmResampler : FurinaPlayerPcmResampler
{
    public EchoPcmResampler() : base()
    {
    }
}
