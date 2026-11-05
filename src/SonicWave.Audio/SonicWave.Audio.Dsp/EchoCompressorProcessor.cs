using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// 向后兼容包装类，指向全新重构的 FurinaPlayerCompressorProcessor。
/// </summary>
[Obsolete("Use FurinaPlayerCompressorProcessor instead")]
public class EchoCompressorProcessor : FurinaPlayerCompressorProcessor
{
	public EchoCompressorProcessor() : base()
	{
	}
}
