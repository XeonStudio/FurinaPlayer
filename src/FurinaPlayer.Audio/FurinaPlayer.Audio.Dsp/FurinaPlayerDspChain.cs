using System;

namespace FurinaPlayer.Audio.Dsp;

public sealed class FurinaPlayerDspChain
{
	private const float SafetyLimiterCeilingDb = -1.0f;
	private static readonly float SafetyCeilingLinear = (float)Math.Pow(10.0, SafetyLimiterCeilingDb / 20.0);

	private double _sampleRate = 44100.0;
	private bool _safetyLimiterEnabled = true;

	public FurinaPlayerEqProcessor Eq { get; }
	public FurinaPlayerConvolutionProcessor Convolution { get; }
	public FurinaPlayerReplayGainProcessor ReplayGain { get; }
	public FurinaPlayerCompressorProcessor Compressor { get; }
	public FurinaPlayerMultibandProcessor Multiband { get; }
	public FurinaPlayerDeEsserProcessor DeEsser { get; }
	public FurinaPlayerLimiterProcessor Limiter { get; }
	public FurinaPlayerSpatialProcessor Spatial { get; }
	public FurinaPlayerHeadroomProcessor Headroom { get; }
	public FurinaPlayerPcmResampler Resampler { get; }
	public FurinaPlayerSdmModulator SdmModulator { get; }
	public DspRackOrder RackOrder { get; }
	public LockFreeAudioVisualBuffer VisualBuffer => LockFreeAudioVisualBuffer.Instance;

	public bool SafetyLimiterEnabled
	{
		get => _safetyLimiterEnabled;
		set => _safetyLimiterEnabled = value;
	}

	public FurinaPlayerDspChain(double sampleRate = 44100.0)
	{
		_sampleRate = sampleRate;
		Eq = new FurinaPlayerEqProcessor(sampleRate);
		Convolution = new FurinaPlayerConvolutionProcessor();
		ReplayGain = new FurinaPlayerReplayGainProcessor();
		Compressor = new FurinaPlayerCompressorProcessor();
		Multiband = new FurinaPlayerMultibandProcessor(sampleRate);
		DeEsser = new FurinaPlayerDeEsserProcessor(sampleRate);
		Limiter = new FurinaPlayerLimiterProcessor(sampleRate);
		Spatial = new FurinaPlayerSpatialProcessor();
		Headroom = new FurinaPlayerHeadroomProcessor();
		Resampler = new FurinaPlayerPcmResampler();
		SdmModulator = new FurinaPlayerSdmModulator();
		RackOrder = new DspRackOrder();

		Prepare(sampleRate);
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Eq.Prepare(_sampleRate);
		Convolution.Prepare(_sampleRate);
		ReplayGain.Prepare(_sampleRate);
		Compressor.Prepare(_sampleRate);
		Multiband.Prepare(_sampleRate);
		DeEsser.Prepare(_sampleRate);
		Limiter.Prepare(_sampleRate);
		Spatial.Prepare(_sampleRate);
		Headroom.Prepare(_sampleRate);
		Resampler.Prepare((int)_sampleRate);
		SdmModulator.Reset();
	}

	public void Reset()
	{
		Eq.Reset();
		Convolution.Reset();
		ReplayGain.Reset();
		Compressor.Reset();
		Multiband.Reset();
		DeEsser.Reset();
		Limiter.Reset();
		Spatial.Reset();
		Headroom.Reset();
		Resampler.Reset();
		SdmModulator.Reset();
	}

	public void ProcessBlock(float[] buffer, int offset, int count, int channels)
	{
		if (count <= 0 || channels <= 0) return;

		var modules = RackOrder.Order;
		for (int i = 0; i < modules.Count; i++)
		{
			var mod = modules[i];
			if (RackOrder.IsBypassed(mod)) continue;

			switch (mod)
			{
				case DspRackModuleId.ReplayGain:
					ReplayGain.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Equalizer:
					Eq.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Convolution:
					Convolution.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Compressor:
					Compressor.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.MultibandCompressor:
					Multiband.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.DeEsser:
					DeEsser.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Crossfeed:
					Spatial.ProcessCrossfeed(buffer, offset, count, channels);
					Spatial.ProcessVirtualStage(buffer, offset, count, channels);
					break;
				case DspRackModuleId.StereoField:
					Spatial.ProcessStereoField(buffer, offset, count, channels);
					break;
				case DspRackModuleId.ChannelMatrix:
					Spatial.ProcessChannelMatrix(buffer, offset, count, channels);
					Spatial.ProcessChannelTool(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Headroom:
					Headroom.ProcessInterleaved(buffer, offset, count, channels);
					break;
				case DspRackModuleId.Resampler:
					// Resampler applies when sample rate conversion is engaged
					break;
				case DspRackModuleId.SdmModulator:
					// SDM engages for DSD streaming mode
					break;
				case DspRackModuleId.Limiter:
					Limiter.ProcessInterleaved(buffer, offset, count, channels);
					break;
			}
		}

		if (_safetyLimiterEnabled)
		{
			ApplySafetyLimiter(buffer, offset, count);
		}

		// Push to real-time 60fps lock-free visual buffer for waveform, spectrum, and loudness metering
		LockFreeAudioVisualBuffer.Instance.PushSamples(buffer, offset, count, channels);
	}

	private static void ApplySafetyLimiter(float[] buffer, int offset, int count)
	{
		float ceiling = SafetyCeilingLinear;
		for (int i = offset; i < offset + count; i++)
		{
			float s = buffer[i];
			if (float.IsNaN(s) || float.IsInfinity(s))
			{
				buffer[i] = 0f;
				continue;
			}
			float abs = Math.Abs(s);
			if (abs > ceiling)
			{
				float sign = Math.Sign(s);
				// Smooth soft-knee saturation
				float over = abs - ceiling;
				float saturated = ceiling + (1.0f - (float)Math.Exp(-over)) * (1.0f - ceiling);
				buffer[i] = sign * Math.Min(0.999f, saturated);
			}
		}
	}
}
