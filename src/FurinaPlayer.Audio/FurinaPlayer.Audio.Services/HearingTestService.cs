using System;
using System.Collections.Generic;
using System.Linq;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Services;
using NAudio.Wave;

namespace FurinaPlayer.Audio.Services;

public enum HearingTestEar
{
	Both = 0,
	Left = 1,
	Right = 2
}

public sealed record AudiogramResult(
	double[] FrequenciesHz,
	double[] LeftThresholdsDbHl,
	double[] RightThresholdsDbHl,
	double LeftPtaDb,
	double RightPtaDb,
	int GoldenEarScore,
	string GradeLabel,
	string SummaryText,
	double[] RecommendedEqGains10,
	double RecommendedPreampDb);

/// <summary>
/// Clinical pure-tone audiometry &amp; "Golden Ear" compensation service:
/// generates calibrated sine test tones with anti-click fade envelopes,
/// analyzes per-frequency hearing thresholds into an Audiogram, and computes
/// a safe 10-band EQ compensation curve linked directly to FurinaPlayer's DSP chain.
/// </summary>
public sealed class HearingTestService : IDisposable
{
	public static readonly double[] TestFrequenciesHz =
	{
		250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 12000.0, 16000.0
	};

	public static readonly double[] EqBandFrequenciesHz =
	{
		31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0
	};

	private readonly object _lock = new();
	private WaveOutEvent? _waveOut;
	private PureToneProvider? _provider;

	public bool IsPlaying { get; private set; }

	public void StartTone(double frequencyHz, double levelDbHl, HearingTestEar ear)
	{
		StopTone();
		var provider = new PureToneProvider(48000)
		{
			FrequencyHz = Math.Clamp(frequencyHz, 125.0, 18000.0),
			AmplitudeLinear = DbHlToSafeAmplitude(levelDbHl),
			Ear = ear
		};
		var waveOut = new WaveOutEvent { DesiredLatency = 60 };
		waveOut.Init(provider);
		waveOut.Play();

		lock (_lock)
		{
			_provider = provider;
			_waveOut = waveOut;
			IsPlaying = true;
		}
	}

	public void UpdateTone(double frequencyHz, double levelDbHl, HearingTestEar ear)
	{
		lock (_lock)
		{
			if (_provider != null)
			{
				_provider.FrequencyHz = Math.Clamp(frequencyHz, 125.0, 18000.0);
				_provider.AmplitudeLinear = DbHlToSafeAmplitude(levelDbHl);
				_provider.Ear = ear;
			}
		}
	}

	public void StopTone()
	{
		lock (_lock)
		{
			IsPlaying = false;
			if (_provider != null)
			{
				_provider.RequestFadeOut();
			}
			try
			{
				_waveOut?.Stop();
				_waveOut?.Dispose();
			}
			catch
			{
			}
			_waveOut = null;
			_provider = null;
		}
	}

	/// <summary>
	/// Maps clinical dB HL (0..85 dB HL) to safe digital linear amplitude,
	/// strictly capped at -10 dBFS (0.316f) to protect user hearing.
	/// </summary>
	public static float DbHlToSafeAmplitude(double dbHl)
	{
		double clampedHl = Math.Clamp(dbHl, 0.0, 85.0);
		// Map 0 dB HL -> -72 dBFS, 85 dB HL -> -10 dBFS
		double dbfs = -72.0 + (clampedHl / 85.0) * 62.0;
		dbfs = Math.Min(dbfs, -10.0);
		return (float)Math.Pow(10.0, dbfs / 20.0);
	}

	public static PureToneProvider CreateOfflineProvider(double frequencyHz, double levelDbHl, HearingTestEar ear, int sampleRate = 48000)
	{
		return new PureToneProvider(sampleRate)
		{
			FrequencyHz = Math.Clamp(frequencyHz, 125.0, 18000.0),
			AmplitudeLinear = DbHlToSafeAmplitude(levelDbHl),
			Ear = ear
		};
	}

	public static AudiogramResult AnalyzeAudiogram(double[] leftThresholdsDbHl, double[] rightThresholdsDbHl)
	{
		int n = TestFrequenciesHz.Length;
		double[] left = new double[n];
		double[] right = new double[n];
		for (int i = 0; i < n; i++)
		{
			left[i] = Math.Clamp(i < leftThresholdsDbHl.Length ? leftThresholdsDbHl[i] : 15.0, 0.0, 90.0);
			right[i] = Math.Clamp(i < rightThresholdsDbHl.Length ? rightThresholdsDbHl[i] : 15.0, 0.0, 90.0);
		}

		// Standard clinical PTA = average of 500, 1000, 2000, 4000 Hz (indices 1, 2, 3, 4)
		double leftPta = (left[1] + left[2] + left[3] + left[4]) / 4.0;
		double rightPta = (right[1] + right[2] + right[3] + right[4]) / 4.0;
		double avgPta = (leftPta + rightPta) / 2.0;
		double highFreqAvg = (left[5] + right[5] + left[6] + right[6]) / 4.0;

		int score = (int)Math.Clamp(Math.Round(100.0 - avgPta * 1.15 - Math.Max(0.0, highFreqAvg - 20.0) * 0.45), 10, 100);

		string grade;
		string summary;
		if (avgPta <= 12.0 && highFreqAvg <= 20.0)
		{
			grade = "黄金听感 · 极佳";
			summary = "全频段听觉灵敏度均衡，高频延伸表现优异，无需强增益补偿，建议保持轻微空气感微调。";
		}
		else if (avgPta <= 25.0 && highFreqAvg <= 35.0)
		{
			grade = "正常听力 · 均衡";
			summary = "核心语音频段（500–4000 Hz）听力正常，可按需对 8–16 kHz 进行温和补偿以提升细节通透度。";
		}
		else if (avgPta <= 25.0 && highFreqAvg > 35.0)
		{
			grade = "高频轻微衰减";
			summary = "中低频听力正常，但 8 kHz 以上高频阈值偏高，建议适度抬升 4–16 kHz 弥补泛音与齿音细节。";
		}
		else if (avgPta <= 40.0)
		{
			grade = "轻度听力衰减";
			summary = "部分频段存在轻度阈值升高，已为您计算温和非失真 EQ 补偿曲线并自动匹配负向 Preamp 保护余量。";
		}
		else
		{
			grade = "中度及以上衰减";
			summary = "检测到明显频响衰减，已限制最大 EQ 补偿在安全范围内（+6 dB 封顶）以避免削波失真；建议结合专业医学评估。";
		}

		// Compute safe 10-band EQ compensation (-3.0 .. +6.0 dB)
		double[] eq10 = new double[10];
		double[] avgAtTest = new double[n];
		for (int i = 0; i < n; i++)
		{
			avgAtTest[i] = (left[i] + right[i]) / 2.0;
		}

		// Map 10 EQ bands (31.25, 62.5, 125, 250, 500, 1k, 2k, 4k, 8k, 16k)
		// Sub-bass (31-125 Hz) conservatively tracks 250 Hz at 50% weight
		double subComp = Math.Clamp((avgAtTest[0] - 15.0) * 0.16, -2.0, 3.0);
		eq10[0] = Math.Round(subComp * 0.6, 1);
		eq10[1] = Math.Round(subComp * 0.8, 1);
		eq10[2] = Math.Round(subComp, 1);

		// 250, 500, 1000, 2000, 4000, 8000 Hz -> test indices 0..5
		for (int i = 0; i < 6; i++)
		{
			double lossAboveBaseline = Math.Max(0.0, avgAtTest[i] - 15.0);
			// Half-gain rule (NAL-R inspired), capped at +6.0 dB
			double gain = Math.Clamp(lossAboveBaseline * 0.28, 0.0, 6.0);
			eq10[3 + i] = Math.Round(gain, 1);
		}
		// 16 kHz -> average of 12k & 16k
		double ultraLoss = Math.Max(0.0, ((avgAtTest[6] + avgAtTest[7]) / 2.0) - 20.0);
		eq10[9] = Math.Round(Math.Clamp(ultraLoss * 0.25, 0.0, 6.0), 1);

		double maxGain = eq10.Max();
		double preampDb = maxGain > 0.5 ? Math.Round(-maxGain - 0.5, 1) : 0.0;

		return new AudiogramResult(
			TestFrequenciesHz,
			left,
			right,
			Math.Round(leftPta, 1),
			Math.Round(rightPta, 1),
			score,
			grade,
			summary,
			eq10,
			preampDb);
	}

	public static void ApplyCompensationToEngine(AudioEngine? engine, SettingsService settingsService, AudiogramResult result)
	{
		var eq = engine?.DspChain?.Eq;
		if (eq != null)
		{
			eq.Enabled = true;
			eq.PreampDb = result.RecommendedPreampDb;
			eq.SetAllBandGains(result.RecommendedEqGains10);
		}

		settingsService.Update(s =>
		{
			s.EqPresetIndex = -1;
			s.EqGains = result.RecommendedEqGains10.ToArray();
		});
	}

	public void Dispose()
	{
		StopTone();
	}

	public sealed class PureToneProvider : ISampleProvider
	{
		private readonly int _sampleRate;
		private double _phase;
		private float _env;
		private volatile bool _fadingOut;
		private readonly float _fadeStep;

		public double FrequencyHz { get; set; } = 1000.0;
		public float AmplitudeLinear { get; set; } = 0.05f;
		public HearingTestEar Ear { get; set; } = HearingTestEar.Both;

		public WaveFormat WaveFormat { get; }

		public PureToneProvider(int sampleRate = 48000)
		{
			_sampleRate = Math.Max(8000, sampleRate);
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, 2);
			_fadeStep = 1.0f / (_sampleRate * 0.025f); // 25 ms anti-click envelope
		}

		public void RequestFadeOut() => _fadingOut = true;

		public int Read(float[] buffer, int offset, int count)
		{
			int frames = count / 2;
			double phaseInc = 2.0 * Math.PI * FrequencyHz / _sampleRate;
			float amp = AmplitudeLinear;
			var ear = Ear;

			for (int i = 0; i < frames; i++)
			{
				if (_fadingOut)
				{
					_env = Math.Max(0f, _env - _fadeStep);
				}
				else
				{
					_env = Math.Min(1f, _env + _fadeStep);
				}

				// Raised cosine smooth envelope
				float smoothEnv = 0.5f - 0.5f * MathF.Cos(_env * MathF.PI);
				float sample = MathF.Sin((float)_phase) * amp * smoothEnv;

				_phase += phaseInc;
				if (_phase >= 2.0 * Math.PI)
				{
					_phase -= 2.0 * Math.PI;
				}

				buffer[offset + i * 2] = ear == HearingTestEar.Right ? 0f : sample;
				buffer[offset + i * 2 + 1] = ear == HearingTestEar.Left ? 0f : sample;
			}
			return frames * 2;
		}
	}
}
