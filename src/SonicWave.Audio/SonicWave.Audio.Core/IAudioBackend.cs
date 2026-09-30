using System;
using System.Collections.Generic;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Core;

internal interface IAudioBackend : IDisposable
{
	string Name { get; }

	bool IsAvailable { get; }

	int SampleRate { get; }

	TimeSpan Position { get; }

	TimeSpan Duration { get; }

	bool IsActuallyPlaying { get; }

	int Volume { get; set; }

	double Speed { get; set; }

	event Action? Ended;

	event Action? Failed;

	void Configure(string? deviceId, string? asioDriver, int bufferMs);

	void SetApplyDspOnOpen(bool apply);

	void SetDspState(double[] eqGains, bool reverbEnabled, double reverbMix, string reverbPreset, double spatialWidth, double spatialModulation, double spatialPredelayMs, TimbreSettings timbre, IReadOnlyList<Vst3PluginState> vst3);

	bool Open(string filePath, bool startPaused);

	bool WaitUntilReady(int timeoutMs);

	void Play();

	void Pause();

	void Stop();

	bool Seek(TimeSpan position);

	void SetEqGains(double[] gains);

	void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs);

	void SetTimbre(TimbreSettings timbre);

	void SetVst3(IReadOnlyList<Vst3PluginState> plugins);
}
