using System;

namespace SonicWave.Audio.Dsp;

public sealed class EchoDspChain
{
	private const float SafetyLimiterCeilingDb = -1.0f;
	private static readonly float SafetyCeilingLinear = (float)Math.Pow(10.0, SafetyLimiterCeilingDb / 20.0);

	private double _sampleRate = 44100.0;
	private bool _safetyLimiterEnabled = true;

	public EchoEqProcessor Eq { get; }
	public EchoConvolutionProcessor Convolution { get; }
	public EchoReplayGainProcessor ReplayGain { get; }
	public EchoCompressorProcessor Compressor { get; }
	public EchoSpatialProcessor Spatial { get; }
	public EchoHeadroomProcessor Headroom { get; }
	public EchoPcmResampler Resampler { get; }
	public EchoSdmModulator SdmModulator { get; }
	public DspRackOrder RackOrder { get; }

	public bool SafetyLimiterEnabled
	{
		get => _safetyLimiterEnabled;
		set => _safetyLimiterEnabled = value;
	}

	public EchoDspChain(double sampleRate = 44100.0)
	{
		_sampleRate = sampleRate;
		Eq = new EchoEqProcessor(sampleRate);
		Convolution = new EchoConvolutionProcessor();
		ReplayGain = new EchoReplayGainProcessor();
		Compressor = new EchoCompressorProcessor();
		Spatial = new EchoSpatialProcessor();
		Headroom = new EchoHeadroomProcessor();
		Resampler = new EchoPcmResampler();
		SdmModulator = new EchoSdmModulator();
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
				case DspRackModuleId.Crossfeed:
					Spatial.ProcessCrossfeed(buffer, offset, count, channels);
					break;
				case DspRackModuleId.StereoField:
					Spatial.ProcessStereoField(buffer, offset, count, channels);
					break;
				case DspRackModuleId.ChannelMatrix:
					Spatial.ProcessChannelMatrix(buffer, offset, count, channels);
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
			}
		}

		if (_safetyLimiterEnabled)
		{
			ApplySafetyLimiter(buffer, offset, count);
		}
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
