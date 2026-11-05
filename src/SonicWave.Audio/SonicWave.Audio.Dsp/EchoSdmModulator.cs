using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// 向后兼容包装类，指向全新重构的 FurinaPlayerSdmModulator。
/// </summary>
public sealed class EchoSdmModulator : FurinaPlayerSdmModulator
{
	public EchoSdmModulator() : base()
	{
	}
}
