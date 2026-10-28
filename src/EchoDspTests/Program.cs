using System;
using System.IO;
using SonicWave.Audio.Dsp;

namespace EchoDspTests;

class Program
{
	static int s_passCount = 0;
	static int s_failCount = 0;

	static void Assert(bool condition, string testName)
	{
		if (condition)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[PASS] {testName}");
			s_passCount++;
		}
		else
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"[FAIL] {testName}");
			s_failCount++;
		}
		Console.ResetColor();
	}

	static void Main(string[] args)
	{
		Console.WriteLine("=================================================");
		Console.WriteLine("  FurinaPlayer - ECHO DSP Suite Unit Test Suite  ");
		Console.WriteLine("=================================================");

		TestBiquadEq();
		TestAutoEqParser();
		TestHeadroomProcessor();
		TestCompressorProcessor();
		TestSpatialCrossfeedAndStereoField();
		TestChannelMatrix();
		TestReplayGain();
		TestConvolutionFir();
		TestPcmResampler();
		TestSdmModulator();
		TestDspRackOrderAndChain();

		Console.WriteLine("=================================================");
		Console.WriteLine($"Tests Completed: {s_passCount + s_failCount} | Passed: {s_passCount} | Failed: {s_failCount}");
		Console.WriteLine("=================================================");

		Environment.Exit(s_failCount == 0 ? 0 : 1);
	}

	static void TestBiquadEq()
	{
		Console.WriteLine("\n--- Testing EchoBiquadFilter & EchoEqProcessor ---");
		var filter = new EchoBiquadFilter
		{
			FilterType = EchoFilterType.Peaking,
			FrequencyHz = 1000.0,
			GainDb = 6.0,
			Q = 1.0,
			Enabled = true
		};
		filter.UpdateCoefficients(44100.0);

		// Run 1kHz sine wave vs 10kHz sine wave
		double sumIn1k = 0.0, sumOut1k = 0.0;
		for (int i = 0; i < 4410; i++)
		{
			float inSample = (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / 44100.0);
			float outSample = filter.ProcessSample(inSample, 0);
			sumIn1k += inSample * inSample;
			sumOut1k += outSample * outSample;
		}

		double gainRatio = Math.Sqrt(sumOut1k / sumIn1k);
		// +6dB is approx 2.0x gain
		Assert(gainRatio > 1.8 && gainRatio < 2.2, $"Peaking filter boosted 1kHz by ~6dB (ratio={gainRatio:F3})");

		// Test LowPass filter
		var lp = new EchoBiquadFilter
		{
			FilterType = EchoFilterType.LowPass,
			FrequencyHz = 1000.0,
			Q = 0.7071
		};
		lp.UpdateCoefficients(44100.0);
		double sumOut10k = 0.0;
		for (int i = 0; i < 4410; i++)
		{
			float inSample = (float)Math.Sin(2.0 * Math.PI * 10000.0 * i / 44100.0);
			float outSample = lp.ProcessSample(inSample, 0);
			sumOut10k += outSample * outSample;
		}
		Assert(sumOut10k / sumIn1k < 0.01, "LowPass filter severely attenuated 10kHz content (< -20dB)");
	}

	static void TestAutoEqParser()
	{
		Console.WriteLine("\n--- Testing AutoEq / Equalizer APO Parser ---");
		string apoPreset = @"# AutoEq profile for Sennheiser HD600
Preamp: -6.5 dB
Filter 1: ON PK Fc 20 Hz Gain -3.2 dB Q 1.41
Filter 2: ON LS Fc 105 Hz Gain 5.5 dB Q 0.71
Filter 3: ON PK Fc 1250 Hz Gain -1.8 dB Q 2.0
Filter 4: ON HS Fc 10000 Hz Gain -2.0 dB Q 0.71
Filter 5: OFF NO Fc 60 Hz Gain 0.0 dB Q 10.0
";
		var preset = AutoEqParser.Parse(apoPreset, "HD600 AutoEq");
		Assert(Math.Abs(preset.PreampDb - (-6.5)) < 0.001, "AutoEq Preamp correctly parsed as -6.5 dB");
		Assert(preset.Bands.Count == 5, $"AutoEq parsed 5 bands (actual: {preset.Bands.Count})");
		Assert(preset.Bands[0].FilterType == EchoFilterType.Peaking && Math.Abs(preset.Bands[0].GainDb - (-3.2)) < 0.01, "Band 1 parsed as Peaking with -3.2 dB gain");
		Assert(preset.Bands[1].FilterType == EchoFilterType.LowShelf && Math.Abs(preset.Bands[1].GainDb - 5.5) < 0.01, "Band 2 parsed as LowShelf with 5.5 dB gain");
		Assert(!preset.Bands[4].Enabled, "Band 5 correctly parsed as OFF (Disabled)");
	}

	static void TestHeadroomProcessor()
	{
		Console.WriteLine("\n--- Testing EchoHeadroomProcessor ---");
		var headroom = new EchoHeadroomProcessor();
		headroom.Prepare(44100.0, 512);
		headroom.HeadroomDb = -6.0;

		float[] buffer = new float[2000];
		for (int i = 0; i < buffer.Length; i++) buffer[i] = 1.0f; // DC 1.0

		headroom.ProcessInterleaved(buffer, 0, buffer.Length, 2);

		// Tail samples should be smoothed to approx 10^(-6/20) = 0.501187
		float tailSample = buffer[buffer.Length - 1];
		Assert(Math.Abs(tailSample - 0.5012f) < 0.01f, $"Headroom smoothly reduced gain from 1.0 to {tailSample:F4} (-6dB)");
	}

	static void TestCompressorProcessor()
	{
		Console.WriteLine("\n--- Testing EchoCompressorProcessor ---");
		var comp = new EchoCompressorProcessor
		{
			Enabled = true,
			ThresholdDb = -12.0f,
			Ratio = 4.0f,
			AttackMs = 1.0f,
			ReleaseMs = 50.0f,
			KneeDb = 4.0f,
			MakeupDb = 0.0f,
			Mix = 1.0f
		};
		comp.Prepare(44100.0);

		// Signal at 0dB (peak 1.0) exceeds -12dB threshold by 12dB.
		// Compression ratio 4:1 reduces the 12dB over by (1 - 1/4)*12 = 9dB.
		// Output level should settle to ~ -9dB (0.355).
		float[] loudSignal = new float[4410];
		for (int i = 0; i < loudSignal.Length; i++) loudSignal[i] = 1.0f;

		comp.ProcessInterleaved(loudSignal, 0, loudSignal.Length, 1);

		float compressedOutput = Math.Abs(loudSignal[loudSignal.Length - 1]);
		Assert(compressedOutput < 0.45f && compressedOutput > 0.30f, $"Compressor compressed 0dBFS signal to {compressedOutput:F3} (~-9dB)");
		Assert(comp.GainReductionDb > 8.0f, $"Gain reduction meter reads {comp.GainReductionDb:F1} dB");
	}

	static void TestSpatialCrossfeedAndStereoField()
	{
		Console.WriteLine("\n--- Testing EchoSpatialProcessor (Crossfeed & Stereo Field) ---");
		var spatial = new EchoSpatialProcessor();
		spatial.Prepare(44100.0);

		// Crossfeed test: Hard-panned Left signal [1.0, 0.0]
		spatial.CrossfeedEnabled = true;
		spatial.CrossfeedAmount = 0.5f;

		float[] stereoBuf = new float[1000 * 2];
		for (int i = 0; i < 1000; i++)
		{
			stereoBuf[i * 2] = 1.0f; // Left only
			stereoBuf[i * 2 + 1] = 0.0f; // Right silent
		}

		spatial.ProcessCrossfeed(stereoBuf, 0, stereoBuf.Length, 2);

		// With Bauer crossfeed, low-frequency sound feeds into the right channel
		float rightBleed = Math.Abs(stereoBuf[stereoBuf.Length - 1]);
		Assert(rightBleed > 0.05f, $"Crossfeed fed sound to opposite ear (Right={rightBleed:F3})");

		// Stereo Field test: Width = 0 should collapse to Mono (L == R)
		spatial.CrossfeedEnabled = false;
		spatial.StereoFieldEnabled = true;
		spatial.StereoWidth = 0.0f;

		float[] stereoBuf2 = new float[100 * 2];
		for (int i = 0; i < 100; i++)
		{
			stereoBuf2[i * 2] = 0.8f;
			stereoBuf2[i * 2 + 1] = 0.2f;
		}

		spatial.ProcessStereoField(stereoBuf2, 0, stereoBuf2.Length, 2);
		float diff = Math.Abs(stereoBuf2[0] - stereoBuf2[1]);
		Assert(diff < 0.001f, $"Stereo Field width=0 collapsed signal to pure Mono (diff={diff})");
	}

	static void TestChannelMatrix()
	{
		Console.WriteLine("\n--- Testing Channel Matrix Routing ---");
		var spatial = new EchoSpatialProcessor();
		spatial.Prepare(44100.0);

		spatial.SetMatrixPreset(ChannelMatrixPreset.SwapLeftRight);
		float[] buf = { 0.7f, 0.3f };
		spatial.ProcessChannelMatrix(buf, 0, 2, 2);

		Assert(Math.Abs(buf[0] - 0.3f) < 0.001f && Math.Abs(buf[1] - 0.7f) < 0.001f, "Matrix swapped Left and Right channels");

		spatial.SetMatrixPreset(ChannelMatrixPreset.InvertPhaseRight);
		float[] buf2 = { 0.5f, 0.5f };
		spatial.ProcessChannelMatrix(buf2, 0, 2, 2);
		Assert(Math.Abs(buf2[0] - 0.5f) < 0.001f && Math.Abs(buf2[1] - (-0.5f)) < 0.001f, "Matrix inverted phase of Right channel");
	}

	static void TestReplayGain()
	{
		Console.WriteLine("\n--- Testing EchoReplayGainProcessor ---");
		var rg = new EchoReplayGainProcessor();
		rg.Prepare(44100.0);
		rg.Mode = ReplayGainMode.Track;
		rg.TrackGainDb = -3.0f; // -3dB
		rg.TrackPeak = 0.8f;
		rg.PreampDb = 0.0f;
		rg.Reset();

		float[] buf = new float[1000];
		for (int i = 0; i < buf.Length; i++) buf[i] = 1.0f;

		rg.ProcessInterleaved(buf, 0, buf.Length, 1);

		float expected = (float)Math.Pow(10.0, -3.0 / 20.0); // ~0.7079
		float actual = buf[buf.Length - 1];
		Assert(Math.Abs(actual - expected) < 0.01f, $"ReplayGain applied -3.0 dB track gain (actual: {actual:F4}, expected: {expected:F4})");
	}

	static void TestConvolutionFir()
	{
		Console.WriteLine("\n--- Testing EchoConvolutionProcessor (FIR / IR) ---");
		var conv = new EchoConvolutionProcessor();
		conv.Prepare(44100.0);

		// Dirac Delta impulse response: [1.0, 0, 0, 0...] -> acts as identity transform
		float[][] deltaIr = new float[1][] { new float[64] };
		deltaIr[0][0] = 1.0f;

		bool loaded = conv.LoadImpulseResponse(deltaIr, 44100.0, "Dirac Delta IR");
		Assert(loaded, "Dirac Delta IR loaded successfully");

		conv.Enabled = true;
		float[] input = new float[128];
		for (int i = 0; i < input.Length; i++) input[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 44100.0);

		float[] testBuf = (float[])input.Clone();
		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 1);

		double maxError = 0.0;
		for (int i = 0; i < testBuf.Length; i++)
		{
			double err = Math.Abs(testBuf[i] - input[i]);
			if (err > maxError) maxError = err;
		}

		Assert(maxError < 0.0001, $"Convolution with Dirac delta produced perfect identity waveform (max error={maxError:E3})");
	}

	static void TestPcmResampler()
	{
		Console.WriteLine("\n--- Testing EchoPcmResampler (Upsampling) ---");
		var resampler = new EchoPcmResampler();
		resampler.Prepare(44100, 192000);
		resampler.Enabled = true;

		float[] pcm44k = new float[441 * 2]; // 10ms stereo at 44.1k
		for (int i = 0; i < 441; i++)
		{
			pcm44k[i * 2] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 44100.0);
			pcm44k[i * 2 + 1] = (float)Math.Cos(2.0 * Math.PI * 440.0 * i / 44100.0);
		}

		float[] resampled = resampler.ResampleInterleaved(pcm44k, 0, pcm44k.Length, 2, out int outCount);
		int expectedFrames = (int)Math.Round(441.0 * 192000.0 / 44100.0);
		int actualFrames = outCount / 2;

		Assert(Math.Abs(actualFrames - expectedFrames) <= 2, $"Resampler upsampled 44.1kHz to 192kHz (output frames={actualFrames}, expected={expectedFrames})");
	}

	static void TestSdmModulator()
	{
		Console.WriteLine("\n--- Testing EchoSdmModulator (PCM to DSD Modulator) ---");
		var sdm = new EchoSdmModulator
		{
			Order = SdmOrder.Order5_HiFi,
			TargetRate = SdmTargetRate.Dsd64
		};

		// Modulate 100 samples
		int ones = 0, minusOnes = 0;
		for (int i = 0; i < 500; i++)
		{
			double pcm = 0.5 * Math.Sin(2.0 * Math.PI * 1000.0 * i / 44100.0);
			int bit = sdm.ModulateSample(pcm, 0);
			if (bit == 1) ones++;
			else if (bit == -1) minusOnes++;
		}

		Assert(ones > 0 && minusOnes > 0 && (ones + minusOnes) == 500, $"SDM 5th-order modulator output valid 1-bit stream ({ones} ones, {minusOnes} minus-ones)");

		// Test DoP bitstream packing
		float[] testPcm = new float[16 * 2];
		for (int i = 0; i < testPcm.Length; i++) testPcm[i] = 0.2f;

		uint[] dop = sdm.EncodeToDop(testPcm, testPcm.Length, 2);
		Assert(dop.Length == 16 * 2, $"DoP encoder output {dop.Length} 24-bit DoP frames");
		byte marker = (byte)(dop[0] >> 16);
		Assert(marker == 0x05 || marker == 0xFA, $"DoP frame marker matches standard DSD marker (0x{marker:X2})");
	}

	static void TestDspRackOrderAndChain()
	{
		Console.WriteLine("\n--- Testing EchoDspChain & DspRackOrder ---");
		var chain = new EchoDspChain(44100.0);

		Assert(chain.RackOrder.Order.Count == 10, "DspRackOrder holds all 10 modular processors");

		// Test bypassing
		chain.RackOrder.SetBypassed(DspRackModuleId.Equalizer, true);
		Assert(chain.RackOrder.IsBypassed(DspRackModuleId.Equalizer), "Bypass state correctly toggled for Equalizer");

		// Test True Peak Safety Limiter
		float[] overloadBuffer = new float[100];
		for (int i = 0; i < overloadBuffer.Length; i++) overloadBuffer[i] = 2.5f; // Extreme +8dB overload

		chain.ProcessBlock(overloadBuffer, 0, overloadBuffer.Length, 2);

		float peak = 0f;
		for (int i = 0; i < overloadBuffer.Length; i++)
		{
			float abs = Math.Abs(overloadBuffer[i]);
			if (abs > peak) peak = abs;
		}

		Assert(peak < 1.0f && peak > 0.88f, $"Safety Limiter tamed extreme +8dB digital overload to safe ceiling: {peak:F4} (< 1.0)");
	}
}
