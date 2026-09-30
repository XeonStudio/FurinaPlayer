using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public static class OfflineEffectRenderer
{
	private sealed class DspChain
	{
		private readonly Equalizer _main;

		private readonly Equalizer _timbre;

		private readonly StereoWidener _widener;

		private readonly SpatialAudioEffect _spatial;

		private readonly IReadOnlyList<Vst3PluginState> _vst3 = Array.Empty<Vst3PluginState>();

		public DspChain(int sampleRate, RenderSettings s)
		{
			_main = new Equalizer(sampleRate);
			_main.Gains = s.EqGains ?? new double[10];
			TimbreSettings timbreSettings = s.Timbre ?? new TimbreSettings();
			_timbre = new Equalizer(sampleRate);
			_timbre.Gains = timbreSettings.BuildEqGains();
			_widener = new StereoWidener
			{
				Amount = timbreSettings.WidenerAmount
			};
			_spatial = new SpatialAudioEffect(sampleRate);
			_vst3 = s.Vst3 ?? Array.Empty<Vst3PluginState>();
			if (s.ReverbEnabled)
			{
				_spatial.SetPreset(string.IsNullOrEmpty(s.ReverbPreset) ? "大厅" : s.ReverbPreset);
				_spatial.WetMix = s.ReverbMix;
				SpatialParameters? spatial = s.Spatial;
				if (spatial.HasValue)
				{
					SpatialParameters valueOrDefault = spatial.GetValueOrDefault();
					_spatial.Width = valueOrDefault.Width;
					_spatial.Modulation = valueOrDefault.Modulation;
					_spatial.PredelayMs = valueOrDefault.PredelayMs;
				}
			}
			else
			{
				_spatial.WetMix = 0.0;
			}
		}

		public void Process(float[] buffer, int channels)
		{
			_main.ProcessInterleaved(buffer, channels);
			_timbre.ProcessInterleaved(buffer, channels);
			_widener.ProcessInterleaved(buffer, channels);
			_spatial.ProcessInterleaved(buffer, channels);
			ApplyVst3(buffer, channels);
		}

		private void ApplyVst3(float[] buffer, int channels)
		{
			if (_vst3.Count == 0)
			{
				return;
			}
			foreach (Vst3PluginState item in _vst3)
			{
				if (item != null && item.IsActive)
				{
					Vst3SimulatedProcessor.Process(buffer, channels, new Vst3Parameters(item.Drive, item.Tone, item.Mix));
				}
			}
		}
	}

	private const int ProgressThrottleMs = 150;

	public static async Task RenderFileToWavAsync(string sourcePath, string wavPath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		using AudioFileReader audioFileReader = new AudioFileReader(sourcePath);
		int sampleRate = audioFileReader.WaveFormat.SampleRate;
		int channels = audioFileReader.WaveFormat.Channels;
		long num = Math.Max(1L, (long)Math.Ceiling(audioFileReader.TotalTime.TotalSeconds * (double)sampleRate));
		using WaveFileWriter waveFileWriter = new WaveFileWriter(wavPath, new WaveFormat(sampleRate, 16, channels));
		DspChain dspChain = new DspChain(sampleRate, settings);
		float[] array = new float[Math.Max(1024, sampleRate / 10) * channels];
		long num2 = 0L;
		DateTime utcNow = DateTime.UtcNow;
		DateTime lastReport = DateTime.UtcNow;
		while (num2 < num)
		{
			ct.ThrowIfCancellationRequested();
			int num3 = audioFileReader.Read(array, 0, array.Length) / channels;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = num3 * channels;
			float[] array2 = new float[num4];
			Array.Copy(array, array2, num4);
			dspChain.Process(array2, channels);
			waveFileWriter.WriteSamples(array2, 0, num4);
			num2 += num3;
			ReportIfDue(progress, ref lastReport, num2, num, utcNow);
		}
		progress?.Report(new RenderProgress(1.0, TimeSpan.Zero));
	}

	public static async Task<RenderResult> RenderSamplesAsync(float[] samples, int sampleRate, int channels, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		if (samples == null || samples.Length == 0)
		{
			return new RenderResult(Array.Empty<float>(), sampleRate, channels, TimeSpan.Zero);
		}
		int num = ((channels <= 0) ? 1 : channels);
		int num2 = ((sampleRate > 0) ? sampleRate : 44100);
		float[] array = new float[samples.Length];
		long num3 = samples.Length / num;
		DspChain dspChain = new DspChain(num2, settings);
		int num4 = Math.Max(1024, num2 / 10);
		long num5 = 0L;
		DateTime utcNow = DateTime.UtcNow;
		DateTime lastReport = DateTime.UtcNow;
		while (num5 < num3)
		{
			ct.ThrowIfCancellationRequested();
			int num6 = (int)Math.Min(num4, num3 - num5);
			int num7 = num6 * num;
			Array.Copy(samples, num5 * num, array, num5 * num, num7);
			float[] array2 = new float[num7];
			Array.Copy(array, num5 * num, array2, 0L, num7);
			dspChain.Process(array2, num);
			Array.Copy(array2, 0L, array, num5 * num, num7);
			num5 += num6;
			ReportIfDue(progress, ref lastReport, num5, num3, utcNow);
		}
		progress?.Report(new RenderProgress(1.0, TimeSpan.Zero));
		return new RenderResult(array, num2, num, TimeSpan.FromSeconds((double)array.Length / (double)num / (double)num2));
	}

	public static async Task<RenderResult> RenderFileToMemoryAsync(string sourcePath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		using AudioFileReader audioFileReader = new AudioFileReader(sourcePath);
		int sampleRate = audioFileReader.WaveFormat.SampleRate;
		int channels = audioFileReader.WaveFormat.Channels;
		long num = Math.Max(1L, (long)Math.Ceiling(audioFileReader.TotalTime.TotalSeconds * (double)sampleRate));
		float[] array = new float[num * channels];
		DspChain dspChain = new DspChain(sampleRate, settings);
		float[] array2 = new float[Math.Max(1024, sampleRate / 10) * channels];
		long num2 = 0L;
		DateTime utcNow = DateTime.UtcNow;
		DateTime lastReport = DateTime.UtcNow;
		while (num2 < num)
		{
			ct.ThrowIfCancellationRequested();
			int num3 = audioFileReader.Read(array2, 0, array2.Length) / channels;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = num3 * channels;
			float[] array3 = new float[num4];
			Array.Copy(array2, array3, num4);
			dspChain.Process(array3, channels);
			Array.Copy(array3, 0L, array, num2 * channels, num4);
			num2 += num3;
			ReportIfDue(progress, ref lastReport, num2, num, utcNow);
		}
		progress?.Report(new RenderProgress(1.0, TimeSpan.Zero));
		return new RenderResult(array, sampleRate, channels, TimeSpan.FromSeconds((double)array.Length / (double)channels / (double)sampleRate));
	}

	private static void ReportIfDue(IProgress<RenderProgress>? progress, ref DateTime lastReport, long done, long total, DateTime started)
	{
		if (progress != null)
		{
			DateTime utcNow = DateTime.UtcNow;
			if (!((utcNow - lastReport).TotalMilliseconds < 150.0) || done >= total)
			{
				lastReport = utcNow;
				double num = Math.Clamp((double)done / (double)total, 0.0, 1.0);
				TimeSpan timeSpan = utcNow - started;
				TimeSpan remaining = ((num > 0.005) ? TimeSpan.FromSeconds(timeSpan.TotalSeconds / num - timeSpan.TotalSeconds) : TimeSpan.Zero);
				progress.Report(new RenderProgress(num, remaining));
			}
		}
	}
}
