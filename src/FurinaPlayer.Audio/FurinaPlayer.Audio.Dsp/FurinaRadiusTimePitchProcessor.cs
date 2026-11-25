using System;
using NAudio.Wave;

namespace FurinaPlayer.Audio.Dsp;

/// <summary>
/// FurinaPlayer 高保真相位一致 WSOLA (Waveform Similarity Overlap-Add) 变速与变调处理器：
/// 采用立体声联动归一化互相关 (NCC) 波形对齐 + 50% 严格归一 Hann 窗叠加 + 4 点 Catmull-Rom 三次插值重采样，
/// 彻底消除传统固定步长 OLA 的相位抵消与金属失真。
/// </summary>
public sealed class FurinaRadiusTimePitchProcessor
{
	private readonly object _lock = new();
	private double _speed = 1.0;
	private double _pitchSemitones = 0.0;
	private int _sampleRate = 44100;
	private int _channels = 2;

	private float[] _inRing = Array.Empty<float>();
	private int _inWritePos;
	private int _inBufferedFrames;

	private float[] _outRing = Array.Empty<float>();
	private int _outReadPos;
	private int _outAvailableFrames;

	// Natural continuation reference (last hop's continuation for WSOLA NCC alignment)
	private float[] _refMono = Array.Empty<float>();
	private bool _hasRef;

	private double _readPhase;
	private int _windowFrames = 2048;
	private int _hopOutFrames = 1024;
	private int _searchRadius = 384;
	private float[] _hannWindow = Array.Empty<float>();

	public double Speed
	{
		get => _speed;
		set => _speed = Math.Clamp(value, 0.50, 2.00);
	}

	public double PitchSemitones
	{
		get => _pitchSemitones;
		set => _pitchSemitones = Math.Clamp(value, -12.0, 12.0);
	}

	public bool IsActive => Math.Abs(_speed - 1.0) > 0.005 || Math.Abs(_pitchSemitones) > 0.05;

	public void Prepare(int sampleRate, int channels = 2)
	{
		lock (_lock)
		{
			_sampleRate = Math.Max(8000, sampleRate);
			_channels = Math.Clamp(channels, 1, 8);
			// ~42ms window for music fidelity, rounded to even integer
			_windowFrames = Math.Clamp((int)(_sampleRate * 0.042) & ~1, 1024, 4096);
			_hopOutFrames = _windowFrames / 2;
			_searchRadius = Math.Clamp(_hopOutFrames / 3, 128, 512);

			// Periodic Hann window: w[i] + w[i + hop] == 1.0 exactly at 50% overlap!
			_hannWindow = new float[_windowFrames];
			for (int i = 0; i < _windowFrames; i++)
			{
				_hannWindow[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / _windowFrames));
			}

			int ringCap = _windowFrames * 16;
			_inRing = new float[ringCap * _channels];
			_outRing = new float[ringCap * _channels];
			_refMono = new float[_hopOutFrames];
			ResetInternal();
		}
	}

	public void Reset()
	{
		lock (_lock)
		{
			ResetInternal();
		}
	}

	private void ResetInternal()
	{
		if (_inRing.Length > 0) Array.Clear(_inRing, 0, _inRing.Length);
		if (_outRing.Length > 0) Array.Clear(_outRing, 0, _outRing.Length);
		if (_refMono.Length > 0) Array.Clear(_refMono, 0, _refMono.Length);
		_inWritePos = 0;
		_inBufferedFrames = 0;
		_outReadPos = 0;
		_outAvailableFrames = 0;
		_readPhase = 0.0;
		_hasRef = false;
	}

	private float ReadChannelCatmullRom(int startBase, int ringFrames, double framePos, int ch, int c)
	{
		int maxF = Math.Max(0, _inBufferedFrames - 1);
		int i1 = Math.Clamp((int)Math.Floor(framePos), 0, maxF);
		float t = (float)(framePos - i1);
		int i0 = Math.Max(0, i1 - 1);
		int i2 = Math.Min(maxF, i1 + 1);
		int i3 = Math.Min(maxF, i1 + 2);

		float y0 = _inRing[((startBase + i0) % ringFrames) * ch + c];
		float y1 = _inRing[((startBase + i1) % ringFrames) * ch + c];
		float y2 = _inRing[((startBase + i2) % ringFrames) * ch + c];
		float y3 = _inRing[((startBase + i3) % ringFrames) * ch + c];

		float t2 = t * t;
		float t3 = t2 * t;
		return 0.5f * ((2f * y1) + (-y0 + y2) * t + (2f * y0 - 5f * y1 + 4f * y2 - y3) * t2 + (-y0 + 3f * y1 - 3f * y2 + y3) * t3);
	}

	private float ReadMonoAt(int startBase, int ringFrames, int relFrame, int ch)
	{
		int idx = ((startBase + Math.Clamp(relFrame, 0, Math.Max(0, _inBufferedFrames - 1))) % ringFrames) * ch;
		if (ch == 1) return _inRing[idx];
		float sum = 0f;
		for (int c = 0; c < ch; c++) sum += _inRing[idx + c];
		return sum / ch;
	}

	public int ProcessPull(ISampleProvider source, float[] buffer, int offset, int count)
	{
		if (!IsActive)
		{
			return source.Read(buffer, offset, count);
		}

		lock (_lock)
		{
			if (_inRing.Length == 0) Prepare(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
			int ch = _channels;
			int wantedFrames = count / ch;
			if (wantedFrames <= 0) return 0;

			double speed = Math.Clamp(_speed, 0.50, 2.00);
			double pitchRatio = Math.Pow(2.0, Math.Clamp(_pitchSemitones, -12.0, 12.0) / 12.0);
			double resampleStep = pitchRatio;
			double analysisHop = _hopOutFrames * speed / Math.Max(0.25, pitchRatio);

			float[] pullScratch = new float[Math.Max(2048, _windowFrames * 4) * ch];
			int ringFrames = _inRing.Length / ch;
			int outCapFrames = _outRing.Length / ch;

			bool eof = false;
			int guard = 0;
			while (_outAvailableFrames < wantedFrames && guard++ < 64)
			{
				int neededInputFrames = (int)Math.Ceiling(_windowFrames * resampleStep) + _searchRadius + 16;
				while (_inBufferedFrames < neededInputFrames && !eof)
				{
					int pullSamples = Math.Min(pullScratch.Length, (ringFrames - _inBufferedFrames - 4) * ch);
					if (pullSamples <= 0) break;
					int got = source.Read(pullScratch, 0, pullSamples);
					if (got <= 0)
					{
						eof = true;
						break;
					}
					int gotFrames = got / ch;
					for (int f = 0; f < gotFrames; f++)
					{
						int dstIdx = ((_inWritePos + f) % ringFrames) * ch;
						int srcIdx = f * ch;
						for (int c = 0; c < ch; c++)
						{
							_inRing[dstIdx + c] = pullScratch[srcIdx + c];
						}
					}
					_inWritePos = (_inWritePos + gotFrames) % ringFrames;
					_inBufferedFrames += gotFrames;
				}

				if (_inBufferedFrames < Math.Min(neededInputFrames, 96) && eof)
				{
					break;
				}

				int startBase = _inWritePos - _inBufferedFrames;
				if (startBase < 0) startBase += ringFrames;

				// WSOLA: find optimal stereo-linked lag in [0 .. 2*searchRadius] maximizing NCC with _refMono
				int bestLag = 0;
				int maxLagAvail = Math.Max(0, _inBufferedFrames - (int)Math.Ceiling(_windowFrames * resampleStep) - 4);
				int searchSpan = Math.Min(_searchRadius, maxLagAvail);
				if (_hasRef && searchSpan > 8)
				{
					double bestScore = double.NegativeInfinity;
					int step = 2; // fast 2-sample coarse search then +/-1 refine
					int cmpLen = Math.Min(_hopOutFrames, 512);
					for (int lag = 0; lag <= searchSpan; lag += step)
					{
						double dot = 0.0, eRef = 1e-12, eCand = 1e-12;
						for (int k = 0; k < cmpLen; k += 2)
						{
							double r = _refMono[k];
							double s = ReadMonoAt(startBase, ringFrames, lag + (int)Math.Round(k * resampleStep), ch);
							dot += r * s;
							eRef += r * r;
							eCand += s * s;
						}
						double ncc = dot / Math.Sqrt(eRef * eCand);
						if (ncc > bestScore)
						{
							bestScore = ncc;
							bestLag = lag;
						}
					}
					// Fine ±1 refine
					int centerLag = bestLag;
					for (int lag = Math.Max(0, centerLag - 1); lag <= Math.Min(searchSpan, centerLag + 1); lag++)
					{
						double dot = 0.0, eRef = 1e-12, eCand = 1e-12;
						for (int k = 0; k < cmpLen; k++)
						{
							double r = _refMono[k];
							double s = ReadMonoAt(startBase, ringFrames, lag + (int)Math.Round(k * resampleStep), ch);
							dot += r * s;
							eRef += r * r;
							eCand += s * s;
						}
						double ncc = dot / Math.Sqrt(eRef * eCand);
						if (ncc > bestScore)
						{
							bestScore = ncc;
							bestLag = lag;
						}
					}
				}

				int outWriteBase = (_outReadPos + _outAvailableFrames) % outCapFrames;
				bool useCubic = Math.Abs(resampleStep - 1.0) > 1e-4;
				for (int wf = 0; wf < _windowFrames; wf++)
				{
					double srcPos = bestLag + wf * resampleStep;
					float win = _hannWindow[wf];
					int outIdx = ((outWriteBase + wf) % outCapFrames) * ch;

					if (useCubic)
					{
						for (int c = 0; c < ch; c++)
						{
							float val = ReadChannelCatmullRom(startBase, ringFrames, srcPos, ch, c);
							if (wf >= _hopOutFrames) _outRing[outIdx + c] = val * win;
							else _outRing[outIdx + c] += val * win;
						}
					}
					else
					{
						int fIdx = (startBase + Math.Min((int)srcPos, Math.Max(0, _inBufferedFrames - 1))) % ringFrames;
						for (int c = 0; c < ch; c++)
						{
							float val = _inRing[fIdx * ch + c];
							if (wf >= _hopOutFrames) _outRing[outIdx + c] = val * win;
							else _outRing[outIdx + c] += val * win;
						}
					}
				}

				// Store natural continuation tail (the second half of this grain) as reference for the next hop
				for (int k = 0; k < _hopOutFrames; k++)
				{
					int rel = bestLag + (int)Math.Round((_hopOutFrames + k) * resampleStep);
					_refMono[k] = ReadMonoAt(startBase, ringFrames, rel, ch);
				}
				_hasRef = true;

				_outAvailableFrames += _hopOutFrames;
				_readPhase += analysisHop;
				int consume = Math.Min(_inBufferedFrames, (int)Math.Floor(_readPhase));
				if (consume > 0)
				{
					_readPhase -= consume;
					_inBufferedFrames -= consume;
				}
			}

			int emitFrames = Math.Min(wantedFrames, _outAvailableFrames);
			for (int f = 0; f < emitFrames; f++)
			{
				int srcIdx = ((_outReadPos + f) % outCapFrames) * ch;
				int dstIdx = offset + f * ch;
				for (int c = 0; c < ch; c++)
				{
					buffer[dstIdx + c] = Math.Clamp(_outRing[srcIdx + c], -1.25f, 1.25f);
					_outRing[srcIdx + c] = 0f;
				}
			}
			_outReadPos = (_outReadPos + emitFrames) % outCapFrames;
			_outAvailableFrames -= emitFrames;
			return emitFrames * ch;
		}
	}

	public static float[] RenderOffline(float[] interleavedInput, int sampleRate, int channels, double speed, double pitchSemitones)
	{
		if (interleavedInput == null || interleavedInput.Length == 0 || channels <= 0)
		{
			return Array.Empty<float>();
		}

		double safeSpeed = Math.Clamp(speed, 0.50, 2.00);
		double safePitch = Math.Clamp(pitchSemitones, -12.0, 12.0);
		if (Math.Abs(safeSpeed - 1.0) <= 0.005 && Math.Abs(safePitch) <= 0.05)
		{
			var copy = new float[interleavedInput.Length];
			Array.Copy(interleavedInput, copy, interleavedInput.Length);
			return copy;
		}

		var memProvider = new ArraySampleProvider(interleavedInput, sampleRate, channels);
		var proc = new FurinaRadiusTimePitchProcessor { Speed = safeSpeed, PitchSemitones = safePitch };
		proc.Prepare(sampleRate, channels);

		int inFrames = interleavedInput.Length / channels;
		int estOutFrames = Math.Max(1, (int)Math.Ceiling(inFrames / safeSpeed) + 2048);
		float[] outBuf = new float[estOutFrames * channels];
		int written = 0;
		float[] chunk = new float[4096 * channels];
		while (true)
		{
			int got = proc.ProcessPull(memProvider, chunk, 0, chunk.Length);
			if (got <= 0) break;
			if (written + got > outBuf.Length)
			{
				Array.Resize(ref outBuf, Math.Max(outBuf.Length * 2, written + got));
			}
			Array.Copy(chunk, 0, outBuf, written, got);
			written += got;
		}

		if (written != outBuf.Length)
		{
			Array.Resize(ref outBuf, written);
		}
		return outBuf;
	}

	private sealed class ArraySampleProvider : ISampleProvider
	{
		private readonly float[] _data;
		private int _pos;
		public WaveFormat WaveFormat { get; }

		public ArraySampleProvider(float[] data, int sampleRate, int channels)
		{
			_data = data;
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int avail = Math.Min(count, _data.Length - _pos);
			if (avail <= 0) return 0;
			Array.Copy(_data, _pos, buffer, offset, avail);
			_pos += avail;
			return avail;
		}
	}
}
