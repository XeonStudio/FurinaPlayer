using System;
using System.Collections.Generic;
using NAudio.Wave;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Output;

public sealed class DspSampleProvider : ISampleProvider
{
	private readonly ISampleProvider _source;

	private readonly Equalizer _equalizer;

	private readonly Equalizer _timbreEqualizer;

	private readonly StereoWidener _widener;

	private readonly SpatialAudioEffect _spatial;

	private TimbreSettings _timbre = new TimbreSettings();

	private IReadOnlyList<Vst3PluginState> _vst3 = Array.Empty<Vst3PluginState>();

	private Vst3PluginState? _exclusiveVst3;

	private float[] _scratch = Array.Empty<float>();

	public WaveFormat WaveFormat { get; }

	public Equalizer Equalizer => _equalizer;

	public SpatialAudioEffect Spatial => _spatial;

	public DspSampleProvider(ISampleProvider source, int sampleRate, int channels)
	{
		_source = source;
		WaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		_equalizer = new Equalizer(sampleRate);
		_timbreEqualizer = new Equalizer(sampleRate);
		_widener = new StereoWidener();
		_spatial = new SpatialAudioEffect(sampleRate);
	}

	public void SetEqGains(double[] gains)
	{
		if (gains.Length == 10)
		{
			_equalizer.Gains = gains;
		}
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_timbre = timbre ?? new TimbreSettings();
		_timbreEqualizer.Gains = _timbre.BuildEqGains();
		_widener.Amount = _timbre.WidenerAmount;
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		_vst3 = plugins ?? Array.Empty<Vst3PluginState>();
		Vst3PluginState exclusiveVst = null;
		foreach (Vst3PluginState item in _vst3)
		{
			if (item != null && item.IsActive)
			{
				exclusiveVst = item;
				break;
			}
		}
		_exclusiveVst3 = exclusiveVst;
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		if (enabled)
		{
			_spatial.SetPreset(string.IsNullOrEmpty(preset) ? "大厅" : preset);
			_spatial.WetMix = mix;
			_spatial.Width = width;
			_spatial.Modulation = modulation;
			_spatial.PredelayMs = predelayMs;
		}
		else
		{
			_spatial.WetMix = 0.0;
		}
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SpatialParameters spatialParameters = SpatialAudioEffect.PresetDefaults(string.IsNullOrEmpty(preset) ? "大厅" : preset);
		SetSpatialAudio(enabled, mix, preset, spatialParameters.Width, spatialParameters.Modulation, spatialParameters.PredelayMs);
	}

	public int Read(float[] buffer, int offset, int count)
	{
		int num = _source.Read(buffer, offset, count);
		if (num <= 0)
		{
			return num;
		}
		if (_scratch.Length != num)
		{
			_scratch = new float[num];
		}
		Span<float> destination = buffer.AsSpan(offset, num);
		float[] scratch = _scratch;
		destination.CopyTo(scratch);
		Vst3PluginState exclusiveVst = _exclusiveVst3;
		if (exclusiveVst != null && Vst3NativeHost.Instance.IsLoadedFor(exclusiveVst.Path))
		{
			Vst3NativeHost.Instance.Process(scratch, WaveFormat.Channels);
		}
		else
		{
			_equalizer.ProcessInterleaved(scratch, WaveFormat.Channels);
			_timbreEqualizer.ProcessInterleaved(scratch, WaveFormat.Channels);
			_widener.ProcessInterleaved(scratch, WaveFormat.Channels);
			_spatial.ProcessInterleaved(scratch, WaveFormat.Channels);
			ApplyVst3(scratch);
		}
		scratch.CopyTo(destination);
		return num;
	}

	private void ApplyVst3(float[] buffer)
	{
		if (_vst3 == null || _vst3.Count == 0)
		{
			return;
		}
		foreach (Vst3PluginState item in _vst3)
		{
			if (item != null && item.IsActive)
			{
				Vst3SimulatedProcessor.Process(buffer, WaveFormat.Channels, new Vst3Parameters(item.Drive, item.Tone, item.Mix));
			}
		}
	}
}
