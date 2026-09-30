namespace SonicWave.Audio.Vst3;

internal struct AudioBusBuffers
{
	public int NumChannels;

	public ulong SilenceFlags;

	public nint ChannelBuffers;
}
