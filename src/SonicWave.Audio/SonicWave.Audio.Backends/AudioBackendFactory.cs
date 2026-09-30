using SonicWave.Audio.Core;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Backends;

internal static class AudioBackendFactory
{
	public static IAudioBackend Create(OutputMode mode, string? deviceId, string? asioDriver, int bufferMs)
	{
		if (mode == OutputMode.WasapiShared || mode == OutputMode.WasapiExclusive || mode == OutputMode.Asio)
		{
			return new NaudioAudioBackend(deviceId, asioDriver, bufferMs);
		}
		return new VlcAudioBackend();
	}

	public static NaudioAudioBackend CreateRenderedBackend(string? deviceId, string? asioDriver, int bufferMs)
	{
		NaudioAudioBackend naudioAudioBackend = new NaudioAudioBackend(deviceId, asioDriver, bufferMs);
		naudioAudioBackend.SetApplyDspOnOpen(apply: false);
		return naudioAudioBackend;
	}
}
