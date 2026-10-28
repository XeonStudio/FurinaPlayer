using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Dsp;

public sealed class EchoEqProcessor
{
	private readonly List<EchoBiquadFilter> _bands = new();
	private double _sampleRate = 44100.0;
	private double _preampDb = 0.0;
	private float _preampLinear = 1.0f;
	private bool _enabled = true;

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public double PreampDb
	{
		get => _preampDb;
		set
		{
			_preampDb = Math.Clamp(value, -30.0, 30.0);
			_preampLinear = (float)Math.Pow(10.0, _preampDb / 20.0);
		}
	}

	public IReadOnlyList<EchoBiquadFilter> Bands => _bands;

	public EchoEqProcessor(double sampleRate = 44100.0)
	{
		_sampleRate = sampleRate;
		InitializeDefaultBands();
	}

	public void InitializeDefaultBands()
	{
		_bands.Clear();
		// 10-band ISO standard default frequencies
		double[] freqs = { 31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };
		foreach (double f in freqs)
		{
			var band = new EchoBiquadFilter
			{
				FilterType = EchoFilterType.Peaking,
				FrequencyHz = f,
				GainDb = 0.0,
				Q = 1.414,
				Enabled = true
			};
			band.UpdateCoefficients(_sampleRate);
			_bands.Add(band);
		}
	}

	public void SetBands(IEnumerable<EchoBiquadFilter> bands)
	{
		_bands.Clear();
		foreach (var b in bands)
		{
			b.UpdateCoefficients(_sampleRate);
			_bands.Add(b);
		}
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		foreach (var band in _bands)
		{
			band.UpdateCoefficients(_sampleRate);
			band.Reset();
		}
	}

	public void Reset()
	{
		foreach (var band in _bands)
		{
			band.Reset();
		}
	}

	public void SetBandGain(int index, double gainDb)
	{
		if (index >= 0 && index < _bands.Count)
		{
			_bands[index].GainDb = gainDb;
			_bands[index].UpdateCoefficients(_sampleRate);
		}
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || count <= 0 || channels <= 0) return;

		int totalSamples = count;
		int frames = totalSamples / channels;

		for (int frame = 0; frame < frames; frame++)
		{
			int frameOffset = offset + frame * channels;
			for (int ch = 0; ch < channels; ch++)
			{
				float s = buffer[frameOffset + ch] * _preampLinear;
				for (int b = 0; b < _bands.Count; b++)
				{
					s = _bands[b].ProcessSample(s, ch);
				}
				buffer[frameOffset + ch] = s;
			}
		}
	}
}
