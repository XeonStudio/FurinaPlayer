using System;
using FurinaPlayer.Audio.Dsp;
using NAudio.Wave;

namespace FurinaPlayer.Audio.Output;

/// <summary>
/// Live NAudio ISampleProvider that executes FurinaPlayerPcmResampler (multi-stage 2x SIMD cascade
/// or arbitrary polyphase FIR + noise-shaped dither) between DspSampleProvider and SampleToWaveProvider.
/// Thread-safe for real-time filter/dither/compute/A-B comparison updates and zero-GC on steady-state audio callbacks.
/// </summary>
public sealed class FurinaSrcSampleProvider : ISampleProvider
{
	private readonly ISampleProvider _source;
	private readonly FurinaPlayerPcmResampler _resampler;
	private readonly int _sourceSampleRate;
	private readonly int _targetSampleRate;
	private readonly int _channels;
	private readonly bool _isFallbackRate;

	private float[] _inScratch = new float[4096 * 2];
	private float[] _outScratch = new float[4096 * 8 * 2];
	private float[] _pendingOut = new float[8192 * 2];
	private int _pendingOutOffsetSamples;
	private int _pendingOutCountSamples;
	private volatile bool _resetRequested;

	public WaveFormat WaveFormat { get; }

	public int SourceSampleRate => _sourceSampleRate;
	public int TargetSampleRate => _targetSampleRate;
	public bool IsFallbackRate => _isFallbackRate;
	public FurinaPlayerPcmResampler Resampler => _resampler;

	public FurinaSrcSampleProvider(
		ISampleProvider source,
		int targetSampleRate,
		FurinaSrcRuntimeConfig config,
		bool isFallbackRate = false,
		FurinaPlayerPcmResampler? sharedResampler = null)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_sourceSampleRate = Math.Max(8000, source.WaveFormat.SampleRate);
		_channels = Math.Max(1, source.WaveFormat.Channels);
		_targetSampleRate = Math.Clamp(targetSampleRate > 0 ? targetSampleRate : _sourceSampleRate, 8000, 384000);
		_isFallbackRate = isFallbackRate;
		_resampler = sharedResampler ?? new FurinaPlayerPcmResampler();

		WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_targetSampleRate, _channels);
		_resampler.ApplyConfig(config ?? new FurinaSrcRuntimeConfig(), _sourceSampleRate, _targetSampleRate);
	}

	public void ApplyConfig(FurinaSrcRuntimeConfig config)
	{
		if (config == null) return;
		_resampler.ApplyConfig(config, _sourceSampleRate, _targetSampleRate);
	}

	public void RequestReset()
	{
		_resetRequested = true;
	}

	public FurinaSrcRuntimeTelemetry GetTelemetry() =>
		_resampler.GetTelemetry(_targetSampleRate, _isFallbackRate);

	public int Read(float[] buffer, int offset, int count)
	{
		if (count <= 0) return 0;

		if (_resetRequested)
		{
			_resetRequested = false;
			_pendingOutOffsetSamples = 0;
			_pendingOutCountSamples = 0;
			_resampler.Reset();
		}

		// Native pass-through when target rate matches source rate and SRC is disabled
		if (_targetSampleRate == _sourceSampleRate)
		{
			return _source.Read(buffer, offset, count);
		}

		int channels = _channels;
		int outSamplesWritten = 0;
		int alignedCount = (count / channels) * channels;
		if (alignedCount <= 0) return 0;

		// 1. Drain any buffered output samples from the previous conversion batch
		if (_pendingOutCountSamples > 0)
		{
			int toCopy = Math.Min(alignedCount, _pendingOutCountSamples);
			CopySamplesSafe(_pendingOut, _pendingOutOffsetSamples, buffer, offset, toCopy);
			outSamplesWritten += toCopy;
			_pendingOutOffsetSamples += toCopy;
			_pendingOutCountSamples -= toCopy;
			if (_pendingOutCountSamples == 0)
			{
				_pendingOutOffsetSamples = 0;
			}
		}

		int factor = _resampler.ActiveStageCount > 0 &&
		             _targetSampleRate > _sourceSampleRate &&
		             _sourceSampleRate * (1 << _resampler.ActiveStageCount) == _targetSampleRate
			? (1 << _resampler.ActiveStageCount)
			: 0;

		double ratio = (double)_sourceSampleRate / _targetSampleRate;

		// 2. Pull source blocks and resample until caller buffer is satisfied or stream ends
		while (outSamplesWritten < alignedCount)
		{
			int remainingOutFrames = (alignedCount - outSamplesWritten) / channels;
			int desiredInFrames = factor > 0
				? Math.Max(16, (remainingOutFrames + factor - 1) / factor)
				: Math.Max(32, (int)Math.Ceiling(remainingOutFrames * ratio) + 4);
			desiredInFrames = Math.Min(desiredInFrames, 2048);

			int neededInSamples = desiredInFrames * channels;
			if (_inScratch.Length < neededInSamples)
			{
				_inScratch = new float[neededInSamples + channels * 64];
			}

			int readSamples = _source.Read(_inScratch, 0, neededInSamples);
			if (readSamples <= 0)
			{
				break;
			}

			int readFrames = readSamples / channels;
			if (readFrames <= 0)
			{
				break;
			}

			int maxProducedFrames = factor > 0
				? readFrames * factor
				: (int)Math.Ceiling(readFrames / ratio) + 16;
			int requiredOutCap = maxProducedFrames * channels;
			if (_outScratch.Length < requiredOutCap)
			{
				_outScratch = new float[requiredOutCap + channels * 128];
			}

			int producedFrames;
			if (factor > 0 && !_resampler.CompareBypassed)
			{
				producedFrames = _resampler.ProcessExactCascade(_inScratch, 0, readFrames, _outScratch, 0, channels);
			}
			else
			{
				producedFrames = _resampler.ProcessArbitraryBlock(_inScratch, 0, readFrames, _outScratch, 0, maxProducedFrames, channels);
			}

			int producedSamples = producedFrames * channels;
			if (producedSamples <= 0)
			{
				continue;
			}

			int neededOutSamples = alignedCount - outSamplesWritten;
			int copyNow = Math.Min(neededOutSamples, producedSamples);
			CopySamplesSafe(_outScratch, 0, buffer, offset + outSamplesWritten, copyNow);
			outSamplesWritten += copyNow;

			int leftover = producedSamples - copyNow;
			if (leftover > 0)
			{
				EnsurePendingCapacity(leftover);
				Array.Copy(_outScratch, copyNow, _pendingOut, 0, leftover);
				_pendingOutOffsetSamples = 0;
				_pendingOutCountSamples = leftover;
			}
		}

		return outSamplesWritten;
	}

	private static void CopySamplesSafe(float[] src, int srcOffset, float[] dst, int dstOffset, int count)
	{
		// NAudio's SampleToWaveProvider aliases a byte[] as float[] via WaveBuffer.FloatBuffer;
		// Array.Copy and Array.AsSpan throw ArrayTypeMismatchException on that backing array.
		for (int i = 0; i < count; i++)
		{
			dst[dstOffset + i] = src[srcOffset + i];
		}
	}

	private void EnsurePendingCapacity(int neededSamples)
	{
		if (_pendingOut.Length < neededSamples)
		{
			_pendingOut = new float[neededSamples + _channels * 512];
		}
	}
}
