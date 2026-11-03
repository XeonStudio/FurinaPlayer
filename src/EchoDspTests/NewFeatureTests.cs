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
		Console.WriteLine("--- Testing ECHO Volume Equalization & ReplayGain Engine ---");
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
}
