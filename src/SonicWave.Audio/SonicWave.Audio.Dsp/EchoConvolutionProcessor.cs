using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Legacy wrapper for Echo convolution processor, redirects to <see cref="FurinaPlayerConvolutionProcessor"/>.
/// </summary>
[Obsolete("Use FurinaPlayerConvolutionProcessor instead")]
public class EchoConvolutionProcessor : FurinaPlayerConvolutionProcessor
{
	public new const int MaxTaps = 131072;

	public EchoConvolutionProcessor() : base()
	{
	}
}
