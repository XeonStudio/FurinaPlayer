using System;
using System.IO;

namespace SonicWave.Audio.Dsp;

public sealed class EchoConvolutionProcessor
{
	public const int MaxTaps = 8192;

	private double _sampleRate = 44100.0;
	private bool _enabled = false;
	private float _trimDb = 0.0f;
	private float _trimGain = 1.0f;
	private string _irName = string.Empty;

	// Impulse response taps per channel: [channel, tap]
	private float[][] _taps = Array.Empty<float[]>();
	private int _tapCount = 0;

	// Circular ring buffers per channel: [channel][MaxTaps]
	private float[][] _history = new float[2][];
	private int _historyWriteIndex = 0;
	private bool _clippingRisk = false;

	public bool Enabled
	{
		get => _enabled && _tapCount > 0;
		set => _enabled = value;
	}

	public float TrimDb
	{
		get => _trimDb;
		set
		{
			_trimDb = Math.Clamp(value, -24.0f, 24.0f);
			_trimGain = (float)Math.Pow(10.0, _trimDb / 20.0);
		}
	}

	public string IrName => _irName;
	public int TapCount => _tapCount;
	public bool ClippingRisk => _clippingRisk;

	public EchoConvolutionProcessor()
	{
		for (int i = 0; i < _history.Length; i++)
		{
			_history[i] = new float[MaxTaps];
		}
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Reset();
	}

	public void Reset()
	{
		_historyWriteIndex = 0;
		_clippingRisk = false;
		for (int i = 0; i < _history.Length; i++)
		{
			Array.Clear(_history[i], 0, _history[i].Length);
		}
	}

	public bool LoadImpulseResponse(float[][] taps, double sourceSampleRate, string name = "Custom IR")
	{
		if (taps == null || taps.Length == 0 || taps[0].Length == 0)
		{
			ClearImpulseResponse();
			return false;
		}

		_irName = name;
		int channels = Math.Min(2, taps.Length);
		double safeSourceRate = sourceSampleRate > 0 ? sourceSampleRate : _sampleRate;
		double safeTargetRate = _sampleRate > 0 ? _sampleRate : safeSourceRate;

		int rawSamples = taps[0].Length;
		int targetSamples = Math.Min(MaxTaps, (int)Math.Ceiling(rawSamples * safeTargetRate / safeSourceRate));
		double ratio = safeSourceRate / safeTargetRate;

		_taps = new float[channels][];
		for (int ch = 0; ch < channels; ch++)
		{
			_taps[ch] = new float[targetSamples];
			float[] srcCh = taps[ch];
			for (int s = 0; s < targetSamples; s++)
			{
				double pos = s * ratio;
				int idx0 = (int)Math.Floor(pos);
				int idx1 = Math.Min(idx0 + 1, rawSamples - 1);
				float frac = (float)(pos - idx0);

				float v0 = idx0 < rawSamples ? srcCh[idx0] : 0f;
				float v1 = idx1 < rawSamples ? srcCh[idx1] : 0f;
				float interp = v0 + (v1 - v0) * frac;
				_taps[ch][s] = float.IsFinite(interp) ? interp : 0f;
			}
		}

		_tapCount = targetSamples;
		Reset();
		return true;
	}

	public bool LoadWavFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath)) return false;

			using var stream = File.OpenRead(filePath);
			using var reader = new BinaryReader(stream);

			// RIFF header
			string riff = new string(reader.ReadChars(4));
			if (riff != "RIFF") return false;
			reader.ReadInt32(); // fileSize
			string wave = new string(reader.ReadChars(4));
			if (wave != "WAVE") return false;

			int channels = 1;
			int sampleRate = 44100;
			int bitsPerSample = 16;
			float[][]? rawTaps = null;

			while (stream.Position < stream.Length - 8)
			{
				string chunkId = new string(reader.ReadChars(4));
				int chunkSize = reader.ReadInt32();

				if (chunkId == "fmt ")
				{
					short formatTag = reader.ReadInt16();
					channels = reader.ReadInt16();
					sampleRate = reader.ReadInt32();
					reader.ReadInt32(); // byteRate
					reader.ReadInt16(); // blockAlign
					bitsPerSample = reader.ReadInt16();
					int extra = chunkSize - 16;
					if (extra > 0) reader.ReadBytes(extra);
				}
				else if (chunkId == "data")
				{
					int sampleCount = chunkSize / (channels * (bitsPerSample / 8));
					rawTaps = new float[channels][];
					for (int c = 0; c < channels; c++)
					{
						rawTaps[c] = new float[sampleCount];
					}

					for (int s = 0; s < sampleCount; s++)
					{
						for (int c = 0; c < channels; c++)
						{
							if (bitsPerSample == 16)
							{
								short sample = reader.ReadInt16();
								rawTaps[c][s] = sample / 32768.0f;
							}
							else if (bitsPerSample == 24)
							{
								byte b0 = reader.ReadByte();
								byte b1 = reader.ReadByte();
								byte b2 = reader.ReadByte();
								int val = (b2 << 24) | (b1 << 16) | (b0 << 8);
								rawTaps[c][s] = val / 2147483648.0f;
							}
							else if (bitsPerSample == 32)
							{
								rawTaps[c][s] = reader.ReadSingle();
							}
							else
							{
								reader.ReadBytes(bitsPerSample / 8);
							}
						}
					}
					break;
				}
				else
				{
					reader.ReadBytes(chunkSize);
				}
			}

			if (rawTaps != null)
			{
				return LoadImpulseResponse(rawTaps, sampleRate, Path.GetFileNameWithoutExtension(filePath));
			}
		}
		catch
		{
			// file error
		}

		return false;
	}

	public void ClearImpulseResponse()
	{
		_taps = Array.Empty<float[]>();
		_tapCount = 0;
		_irName = string.Empty;
		Reset();
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || _tapCount <= 0 || count <= 0 || channels <= 0 || _taps.Length == 0)
		{
			_clippingRisk = false;
			return;
		}

		int frames = count / channels;
		int activeChannels = Math.Min(channels, _history.Length);
		int tapsChannels = _taps.Length;
		int tapsCount = Math.Min(_tapCount, MaxTaps);
		float trim = _trimGain;
		bool risk = false;

		for (int frame = 0; frame < frames; frame++)
		{
			int frameOffset = offset + frame * channels;

			// Write current input into circular history
			for (int ch = 0; ch < activeChannels; ch++)
			{
				float input = buffer[frameOffset + ch];
				_history[ch][_historyWriteIndex] = float.IsFinite(input) ? input : 0f;
			}

			// Compute FIR convolution
			for (int ch = 0; ch < activeChannels; ch++)
			{
				int irCh = tapsChannels > 1 ? Math.Min(ch, tapsChannels - 1) : 0;
				float[] irTaps = _taps[irCh];
				float[] chHistory = _history[ch];
				double sum = 0.0;

				for (int tap = 0; tap < tapsCount; tap++)
				{
					int hIdx = _historyWriteIndex - tap;
					if (hIdx < 0) hIdx += MaxTaps;
					sum += irTaps[tap] * chHistory[hIdx];
				}

				float outSample = (float)sum * trim;
				if (Math.Abs(outSample) > 0.98f)
				{
					risk = true;
				}

				buffer[frameOffset + ch] = outSample;
			}

			_historyWriteIndex = (_historyWriteIndex + 1) % MaxTaps;
		}

		_clippingRisk = risk;
	}
}
