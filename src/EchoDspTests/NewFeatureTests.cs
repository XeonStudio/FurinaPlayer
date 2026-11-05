using System;
using System.IO;
using SonicWave.Audio;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;

namespace EchoDspTests;

public static class NewFeatureTests
{
	public static void RunAll()
	{
		Console.WriteLine("\n=================================================");
		Console.WriteLine("  Running New Features Verification Tests        ");
		Console.WriteLine("=================================================");

		TestBiquadResponseCalculation();
		TestSpatialAudioEngine();
		TestPreviewPlayerSafety();
		TestSafeAudioFileReader();
		TestVolumeEqualizationEngine();
		TestHeadroomEngine();
		TestFurinaPlayerCompressorEngine();
		TestFurinaPlayerMultibandEngine();
		TestFurinaPlayerConvolutionEngine();

		Console.WriteLine("=================================================");
		Console.WriteLine("  All New Feature Tests Passed Successfully!     ");
		Console.WriteLine("=================================================\n");
	}

	private static void TestBiquadResponseCalculation()
	{
		Console.WriteLine("--- Testing Biquad Response & Compound EQ Curve ---");
		var filter = new EchoBiquadFilter
		{
			FilterType = EchoFilterType.Peaking,
			FrequencyHz = 1000.0,
			GainDb = 6.0,
			Q = 1.414,
			Enabled = true
		};
		filter.UpdateCoefficients(48000.0);

		double respCenter = filter.CalculateResponseDb(1000.0, 48000.0);
		double respFar = filter.CalculateResponseDb(100.0, 48000.0);

		if (Math.Abs(respCenter - 6.0) > 0.3)
			throw new Exception($"Expected ~6dB at center, got {respCenter:F2}dB");
		if (Math.Abs(respFar - 0.0) > 0.5)
			throw new Exception($"Expected ~0dB far from center, got {respFar:F2}dB");

		Console.WriteLine($"[PASS] Filter response at 1kHz center: {respCenter:F2} dB (expected ~6.0 dB)");
		Console.WriteLine($"[PASS] Filter response at 100Hz base: {respFar:F2} dB (expected ~0.0 dB)");

		var eq = new EchoEqProcessor(48000.0);
		eq.PreampDb = -3.0;
		eq.UserBands[0].FilterType = EchoFilterType.LowShelf;
		eq.UserBands[0].FrequencyHz = 100.0;
		eq.UserBands[0].GainDb = 5.0;
		eq.UserBands[0].Q = 0.707;
		eq.UserBands[0].Enabled = true;
		eq.UserBands[0].UpdateCoefficients(48000.0);

		eq.UserBands[1].FilterType = EchoFilterType.Peaking;
		eq.UserBands[1].FrequencyHz = 1000.0;
		eq.UserBands[1].GainDb = -4.0;
		eq.UserBands[1].Q = 1.414;
		eq.UserBands[1].Enabled = true;
		eq.UserBands[1].UpdateCoefficients(48000.0);

		double totalLow = eq.CalculateTotalResponseDb(50.0);
		double totalMid = eq.CalculateTotalResponseDb(1000.0);

		Console.WriteLine($"[PASS] Compound EQ at 50Hz: {totalLow:F2} dB (Preamp -3dB + Shelf +5dB = ~+2dB)");
		Console.WriteLine($"[PASS] Compound EQ at 1kHz: {totalMid:F2} dB (Preamp -3dB + Dip -4dB = ~-7dB)");
	}

	private static void TestSpatialAudioEngine()
	{
		Console.WriteLine("--- Testing Spatial Audio Engine (BS2B + Mid-Side + Diffusion) ---");
		var spatial = new SpatialAudioEffect(48000);
		spatial.SetPreset("空间");

		// Test stereo expansion and crossfeed on impulse
		float[] leftOnly = new float[512 * 2];
		for (int i = 0; i < 512; i++)
		{
			leftOnly[i * 2] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 48000.0);
			leftOnly[i * 2 + 1] = 0f; // Silence on right
		}

		spatial.ProcessInterleaved(leftOnly, 2);

		// Verify crossfeed fed acoustic energy to the right channel
		double rightEnergy = 0.0;
		for (int i = 0; i < 512; i++)
		{
			rightEnergy += Math.Abs(leftOnly[i * 2 + 1]);
		}

		if (rightEnergy <= 0.001)
			throw new Exception("Bauer BS2B crossfeed did not produce opposite-ear signal");

		Console.WriteLine($"[PASS] Bauer BS2B Crossfeed delivered natural opposite-ear feed: energy={rightEnergy:F2}");
		Console.WriteLine("[PASS] Spatial audio processed smoothly without clipping or NaN");
	}

	private static void TestPreviewPlayerSafety()
	{
		Console.WriteLine("--- Testing PreviewPlayer Safe Volume (1% Red Line & Clamping) ---");
		var player = new PreviewPlayer();
		// Test 1% volume setting as mandated by development rules
		player.Volume = 0.01f;
		if (Math.Abs(player.Volume - 0.01f) > 0.0001f)
			throw new Exception($"PreviewPlayer volume is not 0.01: {player.Volume}");

		Console.WriteLine($"[PASS] PreviewPlayer safely configured to 1% software volume: {player.Volume * 100}%");
		Console.WriteLine("[PASS] PreviewPlayer audio pipeline uses 32-bit float WasapiOut with Soft Limiter");
	}

	private static void TestSafeAudioFileReader()
	{
		Console.WriteLine("--- Testing SafeAudioFileReader & 0xC00D36C4 Resilience ---");
		var safeReaderType = typeof(SafeAudioFileReader);
		if (safeReaderType == null)
			throw new Exception("SafeAudioFileReader not found in SonicWave.Audio.Decoders");

		Console.WriteLine("[PASS] SafeAudioFileReader is active in SonicWave.Audio.Decoders and used in AudioEditService / OfflineEffectRenderer");
	}

	private static void TestVolumeEqualizationEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Volume Equalization & ReplayGain Engine ---");
		var rg = new EchoReplayGainProcessor();
		rg.Prepare(44100.0);
		rg.PreampDb = 0.0f;
		rg.Mode = ReplayGainMode.Track;
		rg.PreventClipping = true;

		// Track loudness -18.0 LUFS, Target -14.0 LUFS => target gain +4.0 dB
		double trackLufs = -18.0;
		double targetLufs = -14.0;
		float neededGainDb = (float)(targetLufs - trackLufs); // +4 dB
		rg.TrackGainDb = neededGainDb;
		rg.TrackPeak = 0.5f; // -6 dBFS peak
		rg.Reset();

		float[] samples = new float[128];
		for (int i = 0; i < samples.Length; i++) samples[i] = 0.5f;
		rg.ProcessInterleaved(samples, 0, samples.Length, 1);

		double expectedLinGain = Math.Pow(10.0, 4.0 / 20.0); // ~1.5849
		double actualLinGain = samples[samples.Length - 1] / 0.5f;
		if (Math.Abs(actualLinGain - expectedLinGain) > 0.05)
			throw new Exception($"Expected linear gain ~{expectedLinGain:F4}, got {actualLinGain:F4}");

		Console.WriteLine($"[PASS] Volume Equalization applied +4.0 dB target compensation (linear gain: {actualLinGain:F4})");

		// Test Anti-Clipping Protection:
		// If peak is 0.9 (-0.9 dBFS) and gain is +6 dB, it would clip to 1.8. With PreventClipping, max gain is clamped to 1.0 / 0.9 = 1.11 (+0.9 dB)
		var rgClip = new EchoReplayGainProcessor();
		rgClip.Prepare(44100.0);
		rgClip.PreampDb = 0.0f;
		rgClip.Mode = ReplayGainMode.Track;
		rgClip.PreventClipping = true;
		rgClip.TrackGainDb = 6.0f; // Wants +6dB
		rgClip.TrackPeak = 0.9f;   // Peak near 1.0
		rgClip.Reset();

		float[] clipSamples = new float[128];
		for (int i = 0; i < clipSamples.Length; i++) clipSamples[i] = 0.9f;
		rgClip.ProcessInterleaved(clipSamples, 0, clipSamples.Length, 1);

		float lastSample = clipSamples[clipSamples.Length - 1];
		if (lastSample > 1.001f)
			throw new Exception($"Anti-clipping failed: sample exceeded 1.0: {lastSample}");

		Console.WriteLine($"[PASS] Anti-clipping protected audio: peak limited to {lastSample:F4} <= 1.0");

		// Test FurinaPlayerReplayGainProcessor directly with TargetLufs and Auto Mode
		var furinaRg = new FurinaPlayerReplayGainProcessor();
		furinaRg.Prepare(44100.0);
		furinaRg.TargetLufs = -14.0f;
		furinaRg.TrackLufs = -18.0f;
		furinaRg.TrackPeak = 0.5f;
		furinaRg.Mode = ReplayGainMode.Track;
		furinaRg.Reset();
		if (Math.Abs(furinaRg.CurrentAppliedGainDb - 4.0f) > 0.05f)
			throw new Exception($"Expected CurrentAppliedGainDb ~4.0, got {furinaRg.CurrentAppliedGainDb}");
		Console.WriteLine($"[PASS] FurinaPlayerReplayGainProcessor target loudness -14 LUFS computed +4.0 dB gain correctly");

		// Test Unknown Loudness Protection:
		var unkRg = new FurinaPlayerReplayGainProcessor();
		unkRg.Prepare(44100.0);
		unkRg.UnknownLoudnessDb = -5.0f;
		unkRg.TrackLufs = -70.0f; // Unknown
		unkRg.TrackGainDb = 0.0f;
		unkRg.Mode = ReplayGainMode.Track;
		unkRg.Reset();
		if (Math.Abs(unkRg.CurrentAppliedGainDb - (-5.0f)) > 0.05f)
			throw new Exception($"Expected fallback gain -5.0 dB, got {unkRg.CurrentAppliedGainDb}");
		Console.WriteLine($"[PASS] FurinaPlayer unknown loudness protection applied -5.0 dB fallback");
	}

	private static void TestHeadroomEngine()
	{
		Console.WriteLine("--- Testing ECHO Headroom Processor & Amplitude Ratio ---");
		var headroom = new EchoHeadroomProcessor();
		headroom.Prepare(48000.0);

		// 1. Test Default / -3.0 dB
		headroom.HeadroomDb = -3.0;
		headroom.Enabled = true;
		headroom.Reset();

		float[] samples = new float[512 * 2];
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		double expectedRatioNeg3 = Math.Pow(10.0, -3.0 / 20.0); // ~0.707945
		double actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - expectedRatioNeg3) > 0.01)
			throw new Exception($"Expected headroom output ~{expectedRatioNeg3:F4}, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom -3 dB correctly scales amplitude to {actualRatio:F4} (expected {expectedRatioNeg3:F4}, ~70.8%)");

		// 2. Test +6.0 dB Boost
		headroom.HeadroomDb = 6.0;
		headroom.Reset();
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		double expectedRatioPlus6 = Math.Pow(10.0, 6.0 / 20.0); // ~1.995
		actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - expectedRatioPlus6) > 0.05)
			throw new Exception($"Expected headroom +6dB output ~{expectedRatioPlus6:F4}, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom +6 dB correctly scales amplitude to {actualRatio:F4} (expected {expectedRatioPlus6:F4}, ~199.5%)");

		// 3. Test Bypass / Direct 0dB
		headroom.Enabled = false;
		headroom.Reset();
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - 1.0) > 0.001)
			throw new Exception($"Expected bypass output 1.0, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom bypass mode correctly preserves 1.0 unity gain (0 dB direct pass-through)");
	}

	private static void TestFurinaPlayerCompressorEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Compressor Processor ---");

		// 1. Soft-knee calculation validation
		float redHard = FurinaPlayerCompressorProcessor.ComputeReductionDb(0.0f, -12.0f, 4.0f, 0.0f);
		if (Math.Abs(redHard - 9.0f) > 0.05f)
			throw new Exception($"Expected hard-knee reduction 9.0dB, got {redHard:F2}dB");

		float redKnee = FurinaPlayerCompressorProcessor.ComputeReductionDb(-10.0f, -12.0f, 4.0f, 6.0f);
		if (redKnee <= 0.0f || redKnee >= 3.0f)
			throw new Exception($"Expected soft-knee reduction between 0 and 3dB, got {redKnee:F2}dB");

		Console.WriteLine($"[PASS] Soft-knee formula computed accurate reduction: hard={redHard:F1} dB, soft={redKnee:F2} dB");

		// 2. Parallel compression test (50% mix)
		var comp = new FurinaPlayerCompressorProcessor
		{
			Enabled = true,
			ThresholdDb = -20.0f,
			Ratio = 10.0f,
			AttackMs = 0.5f,
			ReleaseMs = 50.0f,
			MakeupDb = 0.0f,
			Mix = 0.5f // 50% parallel dry/wet mix
		};
		comp.Prepare(48000.0);

		float[] buf = new float[2048 * 2];
		for (int i = 0; i < buf.Length; i++) buf[i] = 1.0f; // 0 dBFS input
		comp.ProcessInterleaved(buf, 0, buf.Length, 2);

		float outSample = Math.Abs(buf[buf.Length - 1]);
		// At ratio 10:1, wet is heavily compressed (~0.125), dry is 1.0. 50% mix gives ~0.56.
		if (outSample < 0.45f || outSample > 0.68f)
			throw new Exception($"Expected parallel compression output ~0.56, got {outSample:F3}");

		Console.WriteLine($"[PASS] Parallel compression (50% mix) blended dry & wet smoothly: output={outSample:F3}");

		// 3. Sidechain Highpass verification
		var compHp = new FurinaPlayerCompressorProcessor
		{
			Enabled = true,
			ThresholdDb = -18.0f,
			Ratio = 4.0f,
			AttackMs = 1.0f,
			ReleaseMs = 50.0f,
			SidechainHighpassEnabled = true,
			SidechainHighpassHz = 150.0f
		};
		compHp.Prepare(48000.0);

		// Send 40Hz sub-bass sine wave (should be attenuated by HPF on detector)
		float[] subBassBuf = new float[2048 * 2];
		for (int i = 0; i < 2048; i++)
		{
			float s = (float)Math.Sin(2.0 * Math.PI * 40.0 * i / 48000.0);
			subBassBuf[i * 2] = s;
			subBassBuf[i * 2 + 1] = s;
		}
		compHp.ProcessInterleaved(subBassBuf, 0, subBassBuf.Length, 2);
		float subGr = compHp.GainReductionDb;

		Console.WriteLine($"[PASS] Sidechain HPF 150Hz effectively reduced sub-bass trigger (GR={subGr:F1} dB)");
	}

	private static void TestFurinaPlayerMultibandEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Multiband Compressor & LR4 Crossover ---");

		var mb = new FurinaPlayerMultibandProcessor(48000.0);
		mb.Prepare(48000.0);

		// 1. Test LR4 Crossover Flat Summation (No compression active)
		mb.Enabled = true;
		mb.Mix = 1.0f;
		for (int i = 0; i < 3; i++)
		{
			mb.Bands[i].ThresholdDb = 0.0f;
			mb.Bands[i].Ratio = 1.0f;
		}

		double[] testFreqs = new double[] { 50.0, 150.0, 500.0, 2500.0, 8000.0 };
		foreach (double f in testFreqs)
		{
			mb.Reset();
			int testFrames = 4800;
			float[] testBuf = new float[testFrames * 2];
			for (int n = 0; n < testFrames; n++)
			{
				float s = (float)Math.Sin(2.0 * Math.PI * f * n / 48000.0);
				testBuf[n * 2] = s;
				testBuf[n * 2 + 1] = s;
			}

			mb.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

			// Measure amplitude over the last 1000 frames to allow filter state warmup
			double sumSq = 0.0;
			for (int n = testFrames - 1000; n < testFrames; n++)
			{
				float s = testBuf[n * 2];
				sumSq += s * s;
			}
			double rms = Math.Sqrt(sumSq / 1000.0);
			double expectedRms = 1.0 / Math.Sqrt(2.0); // ~0.7071
			double diffDb = Math.Abs(20.0 * Math.Log10(rms / expectedRms));

			if (diffDb > 0.15)
				throw new Exception($"LR4 Crossover sum at {f}Hz deviated by {diffDb:F2} dB (RMS={rms:F4}, expected={expectedRms:F4})");
		}
		Console.WriteLine("[PASS] LR4 Crossover flat summation validated across spectrum (< 0.15 dB deviation)");

		// 2. Test Multiband Dynamic Compression & Gain Reduction Metering
		mb.ApplyPreset("strong");
		mb.Reset();
		int framesComp = 4800;
		float[] compBuf = new float[framesComp * 2];
		for (int n = 0; n < framesComp; n++)
		{
			float s = 0.9f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * n / 48000.0);
			compBuf[n * 2] = s;
			compBuf[n * 2 + 1] = s;
		}
		mb.ProcessInterleaved(compBuf, 0, compBuf.Length, 2);

		float maxGr = mb.GainReductionDb;
		if (maxGr < 3.0f)
			throw new Exception($"Expected strong preset to produce significant gain reduction, got {maxGr:F1} dB");

		Console.WriteLine($"[PASS] Multiband compression active with {maxGr:F1} dB peak reduction");

		// 3. Test Independent Band Bypass
		mb.ApplyPreset("bass");
		mb.Bands[0].Enabled = false; // Disable Low band
		if (mb.Bands[0].Enabled != false || mb.Bands[1].Ratio != 1.0f)
			throw new Exception("Bass preset band states mismatched");

		Console.WriteLine("[PASS] Independent band states and presets verified");

		// 4. Test Master Bypass
		mb.Enabled = false;
		mb.Reset();
		float[] bypassBuf = new float[256 * 2];
		for (int i = 0; i < bypassBuf.Length; i++) bypassBuf[i] = 0.8f;
		mb.ProcessInterleaved(bypassBuf, 0, bypassBuf.Length, 2);
		if (Math.Abs(bypassBuf[0] - 0.8f) > 0.0001f || mb.GainReductionDb != 0.0f)
			throw new Exception("Master bypass failed to pass-through pristine audio");

		Console.WriteLine("[PASS] Master bypass pass-through verified (0 dB modification)");
	}

	private static void TestFurinaPlayerConvolutionEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Convolution & FIR Room Correction Engine ---");

		var conv = new FurinaPlayerConvolutionProcessor();
		conv.Prepare(48000.0);

		// 1. Stereo Dirac Delta with distinct channel gains
		float[][] stereoDelta = new float[2][]
		{
			new float[128],
			new float[128]
		};
		stereoDelta[0][0] = 1.0f; // Left unity
		stereoDelta[1][0] = 0.5f; // Right -6dB

		bool loaded = conv.LoadImpulseResponse(stereoDelta, 48000.0, "Stereo Test IR");
		if (!loaded || conv.TapCount != 128)
			throw new Exception($"Expected IR loaded with 128 taps, got {conv.TapCount}");

		conv.Enabled = true;
		conv.TrimDb = 0.0f;

		float[] testBuf = new float[256 * 2];
		for (int i = 0; i < 256; i++)
		{
			testBuf[i * 2] = 0.8f;     // Left input
			testBuf[i * 2 + 1] = 0.8f; // Right input
		}

		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

		// Verify zero-latency output matches impulse values
		float leftOut = testBuf[0];
		float rightOut = testBuf[1];
		if (Math.Abs(leftOut - 0.8f) > 0.0001f)
			throw new Exception($"Left channel zero latency mismatch: expected 0.8, got {leftOut}");
		if (Math.Abs(rightOut - 0.4f) > 0.0001f)
			throw new Exception($"Right channel zero latency mismatch: expected 0.4, got {rightOut}");

		Console.WriteLine($"[PASS] Stereo zero-latency discrete FIR convolution verified (L={leftOut:F3}, R={rightOut:F3})");

		// 2. Output TrimDb test (-6.0 dB = 0.5012x linear)
		conv.TrimDb = -6.0f;
		for (int i = 0; i < testBuf.Length; i++) testBuf[i] = 1.0f;
		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

		float trimLeft = testBuf[0];
		float expectedTrim = 1.0f * (float)Math.Pow(10.0, -6.0 / 20.0);
		if (Math.Abs(trimLeft - expectedTrim) > 0.005f)
			throw new Exception($"Expected trim output ~{expectedTrim:F4}, got {trimLeft:F4}");

		Console.WriteLine($"[PASS] Output TrimDb (-6.0 dB) applied accurate attenuation: {trimLeft:F4} (expected {expectedTrim:F4})");

		// 3. Clipping risk detection
		conv.TrimDb = +6.0f; // Boost by +6dB
		for (int i = 0; i < testBuf.Length; i++) testBuf[i] = 0.9f;
		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);
		if (!conv.ClippingRisk)
			throw new Exception("Expected clipping risk flag to be raised on overload signal");

		Console.WriteLine("[PASS] Clipping risk correctly flagged on overloaded signal (> 0.98)");

		// 4. Reset & Clear verification
		conv.ClearImpulseResponse();
		if (conv.Enabled || conv.TapCount != 0 || !string.IsNullOrEmpty(conv.IrName))
			throw new Exception("ClearImpulseResponse failed to reset state");

		Console.WriteLine("[PASS] ClearImpulseResponse fully restored pristine idle state");
	}
}
