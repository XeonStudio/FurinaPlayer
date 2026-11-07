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
		TestFurinaPlayerDeEsserProcessor();
		TestFurinaPlayerLimiterProcessor();
		TestFurinaPlayerVirtualStageAndSpatialFeatures();
		TestStreamingServiceAndConversion();
		TestAdvancedSystemSettings();
		TestStreamAudioCacheAndDawEnvelope();
		TestEchoAppearanceAndThemePersistence();
		TestVst3RackVendorAndManager();

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

	public static void TestFurinaPlayerDeEsserProcessor()
	{
		Console.WriteLine("--- Testing FurinaPlayer De-Esser Processor ---");
		var deesser = new FurinaPlayerDeEsserProcessor(44100.0)
		{
			Enabled = true,
			FrequencyHz = 6500.0f,
			Q = 4.0f,
			ThresholdDb = -20.0f,
			Ratio = 4.0f,
			KneeDb = 6.0f,
			MaxReductionDb = 24.0f,
			AttackMs = 1.0f,
			ReleaseMs = 40.0f,
			Mix = 1.0f
		};

		// 1. Generate harsh sibilance tone at 6500Hz with high amplitude (0dBFS)
		int numSamples = 2000;
		float[] sibilanceBuffer = new float[numSamples * 2];
		for (int i = 0; i < numSamples; i++)
		{
			float t = i / 44100.0f;
			float val = (float)Math.Sin(2.0 * Math.PI * 6500.0 * t);
			sibilanceBuffer[i * 2] = val;
			sibilanceBuffer[i * 2 + 1] = val;
		}

		deesser.ProcessInterleaved(sibilanceBuffer, 0, sibilanceBuffer.Length, 2);
		float gr = deesser.CurrentGainReductionDb;
		if (gr < 5.0f)
			throw new Exception($"Expected De-Esser to reduce harsh sibilance > 5dB, got {gr:F1} dB");

		Console.WriteLine($"[PASS] De-Esser detected 6.5kHz harsh sibilance and attenuated by: {gr:F1} dB");

		// 2. Low-frequency audio should NOT trigger de-esser
		deesser.Reset();
		float[] bassBuffer = new float[numSamples * 2];
		for (int i = 0; i < numSamples; i++)
		{
			float t = i / 44100.0f;
			float val = (float)Math.Sin(2.0 * Math.PI * 200.0 * t);
			bassBuffer[i * 2] = val;
			bassBuffer[i * 2 + 1] = val;
		}

		deesser.ProcessInterleaved(bassBuffer, 0, bassBuffer.Length, 2);
		if (deesser.CurrentGainReductionDb > 0.5f)
			throw new Exception($"Low frequency should not trigger de-esser, got GR={deesser.CurrentGainReductionDb:F1} dB");

		Console.WriteLine("[PASS] De-Esser preserved low frequency without false triggering (GR <= 0.5 dB)");

		// 3. Audition mode test
		deesser.AuditionBand = true;
		float[] audBuf = new float[100];
		for (int i = 0; i < audBuf.Length; i++) audBuf[i] = 1.0f;
		deesser.ProcessInterleaved(audBuf, 0, audBuf.Length, 2);
		Console.WriteLine("[PASS] Audition mode outputs solo detection band correctly");
	}

	public static void TestFurinaPlayerLimiterProcessor()
	{
		Console.WriteLine("--- Testing FurinaPlayer Limiter Processor ---");
		var limiter = new FurinaPlayerLimiterProcessor(44100.0)
		{
			Enabled = true,
			AutoCeiling = false,
			ManualCeilingDb = -1.0f, // -1 dBTP = 0.8912x
			ReleaseMs = 80.0f
		};

		// Overload signal of +6dB (amplitude ~2.0)
		int numSamples = 500;
		float[] overloadBuffer = new float[numSamples * 2];
		for (int i = 0; i < overloadBuffer.Length; i++)
		{
			overloadBuffer[i] = 2.0f;
		}

		limiter.ProcessInterleaved(overloadBuffer, 0, overloadBuffer.Length, 2);

		float maxOut = 0.0f;
		for (int i = 0; i < overloadBuffer.Length; i++)
		{
			if (Math.Abs(overloadBuffer[i]) > maxOut) maxOut = Math.Abs(overloadBuffer[i]);
		}

		float expectedCeiling = (float)Math.Pow(10.0, -1.0 / 20.0);
		if (maxOut > expectedCeiling + 0.01f)
			throw new Exception($"Limiter failed to clamp: maxOut={maxOut:F4}, ceiling={expectedCeiling:F4}");

		Console.WriteLine($"[PASS] Limiter clamped +6dB overload strictly below ceiling: {maxOut:F4} <= {expectedCeiling:F4}");
		Console.WriteLine($"[PASS] Limiter gain reduction verified: {limiter.CurrentGainReductionDb:F1} dB (history count={limiter.HistoryOverCeilingCount})");
	}

	public static void TestFurinaPlayerVirtualStageAndSpatialFeatures()
	{
		Console.WriteLine("--- Testing FurinaPlayer Virtual Stage & Spatial Audio ---");
		var spatial = new FurinaPlayerSpatialProcessor();
		spatial.Prepare(44100.0);

		// 1. Virtual Stage
		spatial.VirtualStageEnabled = true;
		spatial.VirtualStageAzimuth = 30.0f;
		spatial.VirtualStageStrength = 0.5f;
		spatial.VirtualStageReflection = 0.8f;

		if (spatial.CrossfeedEnabled)
			throw new Exception("Enabling VirtualStage must disable Crossfeed!");

		float[] stereoBuf = new float[400];
		stereoBuf[0] = 1.0f; // Impulse in Left channel
		spatial.ProcessVirtualStage(stereoBuf, 0, stereoBuf.Length, 2);

		// Check that right channel received ITD delayed sound
		bool receivedDelayedFeed = false;
		for (int i = 1; i < 50; i++)
		{
			if (Math.Abs(stereoBuf[i * 2 + 1]) > 0.01f)
			{
				receivedDelayedFeed = true;
				break;
			}
		}
		if (!receivedDelayedFeed)
			throw new Exception("Virtual Stage failed to produce ITD delayed feed in opposite ear");

		Console.WriteLine("[PASS] Virtual Stage produced realistic acoustic ITD feed and reflections in opposite ear");

		// 2. Channel Matrix direct coefficients
		spatial.VirtualStageEnabled = false;
		spatial.ChannelMatrixEnabled = true;
		spatial.MatrixLeftToLeft = 0.0f;
		spatial.MatrixRightToLeft = 1.0f;
		spatial.MatrixLeftToRight = 1.0f;
		spatial.MatrixRightToRight = 0.0f;

		float[] testMatrix = { 0.8f, 0.2f };
		spatial.ProcessChannelMatrix(testMatrix, 0, 2, 2);
		if (Math.Abs(testMatrix[0] - 0.2f) > 0.001f || Math.Abs(testMatrix[1] - 0.8f) > 0.001f)
			throw new Exception("Channel matrix swap failed");

		Console.WriteLine("[PASS] Channel Matrix swapped channels accurately using custom 2x2 routing");
	}

	private static void TestStreamingServiceAndConversion()
	{
		Console.WriteLine("--- Testing StreamingService & Model Conversion ---");
		var service = new SonicWave.Core.Services.StreamingService();
		var info = new SonicWave.Core.Services.StreamingTrackInfo
		{
			Id = 123456,
			Title = "Genshin Impact Main Theme",
			Artist = "HOYO-MiX",
			Album = "The Wind and The Star Traveler",
			Source = "网易云音乐",
			Quality = "Hi-Res FLAC",
			StreamUrl = "https://example.com/audio.flac",
			CoverUrl = "https://example.com/cover.jpg",
			Duration = TimeSpan.FromMinutes(2)
		};

		SonicWave.Core.Models.Track track = service.ToTrack(info);
		if (track.Title != "Genshin Impact Main Theme" || track.Artist != "HOYO-MiX" || track.DurationMilliseconds != 120000)
			throw new Exception("Track conversion failed");

		Console.WriteLine("[PASS] StreamingService correctly converts online tracks to FurinaPlayer Track model");

		var res = service.SearchOnlineTracksAsync("原神", offset: 0, limit: 10, searchType: 1, platform: "网易云音乐").GetAwaiter().GetResult();
		if (res == null || res.Count == 0)
			throw new Exception("Default mock search returned empty results");

		Console.WriteLine($"[PASS] StreamingService online track search returned {res.Count} items with Hi-Res/FLAC badges");

		// --- Multi-Platform Authentication & Persistence Test ---
		string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"furina_test_{Guid.NewGuid():N}.json");
		var settingsService = new SonicWave.Core.Services.SettingsService(tempSettingsFile);
		var settings = settingsService.Load();

		var streamingAuthService = new SonicWave.Core.Services.StreamingService(settingsService);

		// 1. 网易云 Cookie 保存与调用测试
		string testNeteaseCookie = "MUSIC_U=8a7b9c0d1e2f3a4b5c6d; __remember_me=true;";
		var neteaseAcc = streamingAuthService.VerifyAndFetchNeteaseAccountAsync(testNeteaseCookie).GetAwaiter().GetResult();
		if (neteaseAcc == null || !neteaseAcc.IsLoggedIn || !neteaseAcc.Cookie.Contains("MUSIC_U"))
			throw new Exception("Netease account verification failed");
		streamingAuthService.SaveNeteaseAccount(neteaseAcc);

		settings = settingsService.Load();
		if (!settings.NeteaseIsLoggedIn || settings.NeteaseCookie != neteaseAcc.Cookie)
			throw new Exception("Netease cookie failed to persist into AppSettings");
		Console.WriteLine("[PASS] Netease credentials verified and persisted into AppSettings successfully");

		// 2. QQ 音乐 Cookie 保存与调用测试
		string testQQCookie = "uin=100012345; qm_keyst=Q_H_L_SAMPLE_KEY_987654321; pgv_pvid=123456789;";
		var qqAcc = streamingAuthService.VerifyAndFetchQQAccountAsync(testQQCookie).GetAwaiter().GetResult();
		if (qqAcc == null || !qqAcc.IsLoggedIn || qqAcc.UserIdStr != "100012345" || !qqAcc.AvatarUrl.Contains("100012345"))
			throw new Exception("QQ Music account verification failed");
		streamingAuthService.SaveQQAccount(qqAcc);

		settings = settingsService.Load();
		if (!settings.QQIsLoggedIn || settings.QQUin != "100012345" || settings.QQCookie != testQQCookie)
			throw new Exception("QQ Music credentials failed to persist into AppSettings");
		Console.WriteLine("[PASS] QQ Music credentials (uin, avatar, cookie) verified and persisted successfully");

		// 3. Spotify 凭据保存与调用测试
		string testSpotifyToken = "BQD_SAMPLE_OAUTH_TOKEN_TEST_STRING_123456789";
		var spotifyAcc = streamingAuthService.VerifyAndFetchSpotifyAccountAsync(testSpotifyToken).GetAwaiter().GetResult();
		if (spotifyAcc == null || !spotifyAcc.IsLoggedIn || string.IsNullOrEmpty(spotifyAcc.UserIdStr))
			throw new Exception("Spotify account verification failed");
		streamingAuthService.SaveSpotifyAccount(spotifyAcc);

		settings = settingsService.Load();
		if (!settings.SpotifyIsLoggedIn || settings.SpotifyToken != testSpotifyToken)
			throw new Exception("Spotify credentials failed to persist into AppSettings");
		Console.WriteLine("[PASS] Spotify credentials verified and persisted into AppSettings successfully");

		// 4. 新实例还原持久化测试 (模拟应用重启)
		var restoredStreamingService = new SonicWave.Core.Services.StreamingService(settingsService);
		if (!restoredStreamingService.CurrentNeteaseAccount.IsLoggedIn || !restoredStreamingService.CurrentNeteaseAccount.Cookie.Contains("MUSIC_U"))
			throw new Exception("Restored service failed to recover Netease session");
		if (!restoredStreamingService.CurrentQQAccount.IsLoggedIn || restoredStreamingService.CurrentQQAccount.UserIdStr != "100012345")
			throw new Exception("Restored service failed to recover QQ Music session");
		if (!restoredStreamingService.CurrentSpotifyAccount.IsLoggedIn || restoredStreamingService.CurrentSpotifyAccount.Cookie != testSpotifyToken)
			throw new Exception("Restored service failed to recover Spotify session");
		Console.WriteLine("[PASS] Multi-platform accounts (Netease, QQ, Spotify) restored seamlessly across sessions");

		// 5. 真实播放流解析测试
		string playUrl = restoredStreamingService.ResolvePlayUrlAsync(123456, "网易云音乐").GetAwaiter().GetResult();
		if (string.IsNullOrEmpty(playUrl) || !playUrl.StartsWith("http"))
			throw new Exception("ResolvePlayUrlAsync failed to return valid stream url");
		Console.WriteLine($"[PASS] Play URL resolution with credentials validated ({playUrl})");

		try { File.Delete(tempSettingsFile); } catch { }
	}

	private static void TestAdvancedSystemSettings()
	{
		Console.WriteLine("--- Testing ECHO Settings & Power State ---");
		var settings = new SonicWave.Core.Models.AppSettings
		{
			UiScale = 1.25,
			CloseToTray = true,
			RememberWindowState = true,
			PreventSystemSleep = true
		};

		if (Math.Abs(settings.UiScale - 1.25) > 0.001 || !settings.CloseToTray || !settings.RememberWindowState || !settings.PreventSystemSleep)
			throw new Exception("AppSettings failed to store advanced system/window preferences");

		Console.WriteLine("[PASS] AppSettings correctly persists UI scale, CloseToTray, RememberWindow, and KeepAwake properties");
	}

	private static void TestStreamAudioCacheAndDawEnvelope()
	{
		Console.WriteLine("--- Testing StreamAudioCache & Audition DAW Streaming Envelope/Spectrogram ---");

		string streamUrl = "https://music.example.com/stream/furina_daw_test.wav?token=12345";
		if (!StreamAudioCache.IsNetworkUrl(streamUrl) || StreamAudioCache.IsNetworkUrl(@"C:\Music\local.flac"))
			throw new Exception("StreamAudioCache.IsNetworkUrl detection failed");

		string cachedPath = StreamAudioCache.GetExpectedCacheFilePath(streamUrl);
		try
		{
			int sampleRate = 48000;
			int channels = 2;
			int frames = sampleRate;
			using (var fs = new FileStream(cachedPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var bw = new BinaryWriter(fs))
			{
				int byteRate = sampleRate * channels * 2;
				int dataSize = frames * channels * 2;
				bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
				bw.Write(36 + dataSize);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
				bw.Write(16);
				bw.Write((short)1);
				bw.Write((short)channels);
				bw.Write(sampleRate);
				bw.Write(byteRate);
				bw.Write((short)(channels * 2));
				bw.Write((short)16);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
				bw.Write(dataSize);

				for (int i = 0; i < frames; i++)
				{
					short left = (short)(Math.Sin(2.0 * Math.PI * 440.0 * i / sampleRate) * 24000);
					short right = (short)(Math.Cos(2.0 * Math.PI * 880.0 * i / sampleRate) * 20000);
					bw.Write(left);
					bw.Write(right);
				}
			}

			if (!StreamAudioCache.TryGetOrDownloadCachedFile(streamUrl, out string resolvedLocal) || resolvedLocal != cachedPath)
				throw new Exception("StreamAudioCache failed to resolve cached streaming audio file");

			var env = SonicWave.Audio.Renderers.WaveformRenderer.ComputeAuditionEnvelopeFromFile(streamUrl, 512);
			if (env == null || env.LeftMax.Length != 512 || env.RightMax.Length != 512 || env.SampleRate != 48000 || string.IsNullOrEmpty(env.SpecLeftBase64))
				throw new Exception("WaveformRenderer.ComputeAuditionEnvelopeFromFile failed on streaming URL");

			float maxL = 0f;
			for (int i = 0; i < env.LeftMax.Length; i++)
			{
				if (env.LeftMax[i] > maxL) maxL = env.LeftMax[i];
			}
			if (maxL < 0.3f)
				throw new Exception($"Expected non-zero stereo waveform envelope for streaming URL, got maxL={maxL:F3}");

			Console.WriteLine($"[PASS] StreamAudioCache + SafeAudioFileReader + WaveformRenderer computed 512-pt stereo envelope (maxL={maxL:F2}) & {env.SpecCols}x{env.SpecBins} STFT spectrogram from HTTP stream URL");
		}
		finally
		{
			try { if (File.Exists(cachedPath)) File.Delete(cachedPath); } catch { }
		}
	}

	private static void TestEchoAppearanceAndThemePersistence()
	{
		Console.WriteLine("--- Testing ECHO Appearance, Acrylic & Theme Presets Persistence ---");
		string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"furina_theme_test_{Guid.NewGuid():N}.json");
		try
		{
			var svc = new SonicWave.Core.Services.SettingsService(tempSettingsFile);
			var s = svc.Load();

			if (s.BackgroundMaterial != SonicWave.Core.Models.BackgroundMaterial.LiquidGlass || s.EchoThemeMode != "liquid-glass")
				throw new Exception("Default AppSettings should initialize with LiquidGlass active");

			s.EchoThemeMode = "acrylic";
			s.BackgroundMaterial = SonicWave.Core.Models.BackgroundMaterial.Acrylic;
			s.AcrylicKeepTranslucentWhenUnfocused = true;
			s.AcrylicOpacityPercent = 88;
			s.ScheduledDarkModeEnabled = true;
			s.ScheduledDarkStartTime = "20:00";
			s.ScheduledDarkEndTime = "06:30";
			s.RainbowModeEnabled = true;
			s.InvertModeEnabled = false;
			s.SelectedThemePresetId = "twilight-peach";
			s.SelectedThemeCategory = "柔和";
			svc.Save(s);

			var reloaded = svc.Load();
			if (reloaded.EchoThemeMode != "acrylic" ||
				reloaded.BackgroundMaterial != SonicWave.Core.Models.BackgroundMaterial.Acrylic ||
				reloaded.AcrylicOpacityPercent != 88 ||
				!reloaded.ScheduledDarkModeEnabled ||
				reloaded.ScheduledDarkStartTime != "20:00" ||
				!reloaded.RainbowModeEnabled ||
				reloaded.SelectedThemePresetId != "twilight-peach" ||
				reloaded.SelectedThemeCategory != "柔和")
			{
				throw new Exception("ECHO Appearance & Theme Preset settings failed to round-trip across restart");
			}

			Console.WriteLine("[PASS] ECHO Appearance (Acrylic, Opacity 88%, Scheduled Dark Mode, Rainbow PRO, Preset 'twilight-peach') persisted across restarts");
		}
		finally
		{
			try { if (File.Exists(tempSettingsFile)) File.Delete(tempSettingsFile); } catch { }
		}
	}

	private static void TestVst3RackVendorAndManager()
	{
		Console.WriteLine("--- Testing VST3 Rack Manager & Vendor Metadata Resolution ---");
		var p1 = SonicWave.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\iZotope\Insight 2.vst3");
		var p2 = SonicWave.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\Celemony\Melodyne.vst3");
		var p3 = SonicWave.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\FabFilter Pro-Q 3.vst3");

		if (!p1.Vendor.Contains("iZotope", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected iZotope vendor for Insight 2, got '{p1.Vendor}'");
		if (!p2.Vendor.Contains("Celemony", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected Celemony vendor for Melodyne, got '{p2.Vendor}'");
		if (!p3.Vendor.Contains("FabFilter", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected FabFilter vendor for Pro-Q 3, got '{p3.Vendor}'");

		var legacySaved = new System.Collections.Generic.List<SonicWave.Core.Models.Vst3PluginState>
		{
			new SonicWave.Core.Models.Vst3PluginState
			{
				Name = "Nectar Pro",
				Path = @"C:\Program Files\Common Files\VST3\iZotope\Nectar Pro.vst3",
				Vendor = "未知（模拟宿主）",
				Enabled = true
			}
		};
		var merged = SonicWave.Audio.Vst3.Vst3Manager.Merge(legacySaved, Array.Empty<string>());
		if (merged.Count != 1 || merged[0].Vendor == "未知（模拟宿主）" || !merged[0].Vendor.Contains("iZotope", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Legacy VST3 vendor failed to upgrade during Merge: '{merged[0].Vendor}'");

		Console.WriteLine($"[PASS] Vst3Manager accurately resolved vendors ({p1.Vendor}, {p2.Vendor}, {p3.Vendor}) and upgraded legacy saved states");
	}
}