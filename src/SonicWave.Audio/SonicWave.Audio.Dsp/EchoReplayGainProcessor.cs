using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// 向后兼容包装类，指向全新重构的 FurinaPlayerReplayGainProcessor。
/// </summary>
[Obsolete("Use FurinaPlayerReplayGainProcessor instead")]
public class EchoReplayGainProcessor : FurinaPlayerReplayGainProcessor
{
	public EchoReplayGainProcessor() : base()
	{
	}
}
