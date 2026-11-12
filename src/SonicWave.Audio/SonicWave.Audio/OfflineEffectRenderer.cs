using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public static class OfflineEffectRenderer
{
	private sealed class DspChain : IDisposable
	{
		private readonly Equalizer _main;

		private readonly EchoEqProcessor? _parametricEq;

		private readonly Equalizer _timbre;

		private readonly StereoWidener _widener;

		private readonly SpatialAudioEffect _spatial;

		private readonly List<(bool isSimulated, Vst3PluginState state, Vst3NativeInstance? instance)> _vst3Chain = new List<(bool, Vst3PluginState, Vst3NativeInstance?)>();

		public DspChain(int sampleRate, int channels, RenderSettings s, IReadOnlyList<EchoBiquadFilter>? parametricBands = null, double parametricPreampDb = 0.0)
		{
			_main = new Equalizer(sampleRate);
			_main.Gains = s.EqGains ?? new double[10];
			if ((parametricBands != null && parametricBands.Count > 0) || Math.Abs(parametricPreampDb) > 0.001)
			{
				int safeRate = sampleRate > 0 ? sampleRate : 44100;
				_parametricEq = new EchoEqProcessor(safeRate)
				{
					Enabled = true,
					CalibrationEnabled = true,
					PreampDb = parametricPreampDb
				};
				_parametricEq.Prepare(safeRate);
				if (parametricBands != null && parametricBands.Count > 0)
				{
					_parametricEq.SetCalibrationBands(CloneParametricBands(parametricBands, safeRate), 0.0);
				}
			}
			TimbreSettings timbreSettings = s.Timbre ?? new TimbreSettings();
			_timbre = new Equalizer(sampleRate);
			_timbre.Gains = timbreSettings.BuildEqGains();
			_widener = new StereoWidener
			{
				Amount = timbreSettings.WidenerAmount
			};
			_spatial = new SpatialAudioEffect(sampleRate);
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

			IReadOnlyList<Vst3PluginState> vst3 = s.Vst3 ?? Array.Empty<Vst3PluginState>();
			int ch = channels > 0 ? channels : 2;
			foreach (Vst3PluginState item in vst3)
			{
				if (item == null || !item.IsActive)
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(item.Path) && Vst3Manager.IsValidVst3Path(item.Path))
				{
					Vst3NativeInstance? inst = null;
					try
					{
						inst = new Vst3NativeInstance(item.Path);
						string? err = inst.Initialize(sampleRate > 0 ? sampleRate : 44100, ch);
						if (err == null)
						{
							byte[]? savedState = Vst3NativeHost.Instance.GetLiveOrCachedState(item.Path);
							if (savedState != null && savedState.Length > 0)
							{
								inst.ImportState(savedState);
							}
							inst.SetTransportPlaying(true);
							_vst3Chain.Add((false, item, inst));
							inst = null;
						}
					}
					catch
					{
					}
					finally
					{
						if (inst != null)
						{
							try { inst.Dispose(); } catch { }
						}
					}
				}
				else
				{
					_vst3Chain.Add((true, item, null));
				}
			}
		}

		public void Process(float[] buffer, int channels)
		{
			_main.ProcessInterleaved(buffer, channels);
			_parametricEq?.ProcessInterleaved(buffer, 0, buffer.Length, channels);
			_timbre.ProcessInterleaved(buffer, channels);
			_widener.ProcessInterleaved(buffer, channels);
			_spatial.ProcessInterleaved(buffer, channels);
			ApplyVst3(buffer, channels);
		}

		private void ApplyVst3(float[] buffer, int channels)
		{
			for (int i = 0; i < _vst3Chain.Count; i++)
			{
				var entry = _vst3Chain[i];
				if (entry.isSimulated)
				{
					Vst3SimulatedProcessor.Process(buffer, channels, new Vst3Parameters(entry.state.Drive, entry.state.Tone, entry.state.Mix));
				}
				else
				{
					entry.instance?.TryProcess(buffer, channels);
				}
			}
		}

		public void Dispose()
		{
			for (int i = 0; i < _vst3Chain.Count; i++)
			{
				Vst3NativeInstance? inst = _vst3Chain[i].instance;
				if (inst != null)
				{
					try { inst.Dispose(); } catch { }
				}
			}
			_vst3Chain.Clear();
		}
	}

	private const int ProgressThrottleMs = 150;

	private static bool HasActiveNativeVst3(RenderSettings settings)
	{
		if (settings.Vst3 == null)
		{
			return false;
		}
		foreach (Vst3PluginState item in settings.Vst3)
		{
			if (item != null && item.IsActive && !string.IsNullOrWhiteSpace(item.Path) && Vst3Manager.IsValidVst3Path(item.Path))
			{
				return true;
			}
		}
		return false;
	}

	private static Task<T> RunOnStaAsync<T>(Func<T> action)
	{
		TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		Thread thread = new Thread(() =>
		{
			try
			{
				tcs.SetResult(action());
			}
			catch (OperationCanceledException oce)
			{
				tcs.TrySetCanceled(oce.CancellationToken);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		})
		{
			IsBackground = true,
			Name = "SonicWave.Vst3OfflineRender"
		};
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		return tcs.Task;
	}

	public static Task RenderFileToWavAsync(string sourcePath, string wavPath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		if (HasActiveNativeVst3(settings))
		{
			return RunOnStaAsync(() =>
			{
				RenderFileToWavCore(sourcePath, wavPath, settings, progress, ct);
				return true;
			});
		}
		RenderFileToWavCore(sourcePath, wavPath, settings, progress, ct);
		return Task.CompletedTask;
	}

	private static void RenderFileToWavCore(string sourcePath, string wavPath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		using SafeAudioFileReader audioFileReader = new SafeAudioFileReader(sourcePath);
		int sampleRate = audioFileReader.WaveFormat.SampleRate;
		int channels = audioFileReader.WaveFormat.Channels;
		long num = Math.Max(1L, (long)Math.Ceiling(audioFileReader.TotalTime.TotalSeconds * (double)sampleRate));
		using WaveFileWriter waveFileWriter = new WaveFileWriter(wavPath, new WaveFormat(sampleRate, 16, channels));
		using DspChain dspChain = new DspChain(sampleRate, channels, settings);
		float[] array = new float[Math.Clamp(sampleRate / 10, 1024, 8192) * channels];
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

	public static Task<RenderResult> RenderSamplesAsync(float[] samples, int sampleRate, int channels, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		return RenderSamplesWithParametricEqAsync(samples, sampleRate, channels, settings, null, 0.0, progress, ct);
	}

	public static Task<RenderResult> RenderSamplesWithParametricEqAsync(
		float[] samples,
		int sampleRate,
		int channels,
		RenderSettings settings,
		IReadOnlyList<EchoBiquadFilter>? parametricBands,
		double preampDb,
		IProgress<RenderProgress>? progress,
		CancellationToken ct)
	{
		IReadOnlyList<EchoBiquadFilter>? bandSnapshot = parametricBands != null && parametricBands.Count > 0
			? CloneParametricBands(parametricBands, sampleRate > 0 ? sampleRate : 44100)
			: null;
		if (HasActiveNativeVst3(settings))
		{
			return RunOnStaAsync(() => RenderSamplesCore(samples, sampleRate, channels, settings, bandSnapshot, preampDb, progress, ct));
		}
		return Task.FromResult(RenderSamplesCore(samples, sampleRate, channels, settings, bandSnapshot, preampDb, progress, ct));
	}

	public static float[] ApplyParametricEq(
		float[] samples,
		int sampleRate,
		int channels,
		IReadOnlyList<EchoBiquadFilter>? parametricBands,
		double preampDb = 0.0)
	{
		if (samples == null || samples.Length == 0)
		{
			return Array.Empty<float>();
		}
		int safeChannels = channels <= 0 ? 1 : channels;
		int safeRate = sampleRate > 0 ? sampleRate : 44100;
		float[] output = (float[])samples.Clone();
		if ((parametricBands == null || parametricBands.Count == 0) && Math.Abs(preampDb) <= 0.001)
		{
			return output;
		}
		var eq = new EchoEqProcessor(safeRate)
		{
			Enabled = true,
			CalibrationEnabled = true,
			PreampDb = preampDb
		};
		eq.Prepare(safeRate);
		if (parametricBands != null && parametricBands.Count > 0)
		{
			eq.SetCalibrationBands(CloneParametricBands(parametricBands, safeRate), 0.0);
		}
		eq.ProcessInterleaved(output, 0, output.Length, safeChannels);
		return output;
	}

	private static EchoBiquadFilter[] CloneParametricBands(IReadOnlyList<EchoBiquadFilter> bands, double sampleRate)
	{
		var cloned = new EchoBiquadFilter[bands.Count];
		for (int i = 0; i < bands.Count; i++)
		{
			EchoBiquadFilter src = bands[i];
			var copy = new EchoBiquadFilter
			{
				FilterType = src.FilterType,
				FrequencyHz = src.FrequencyHz,
				GainDb = src.GainDb,
				Q = src.Q,
				Enabled = src.Enabled
			};
			copy.UpdateCoefficients(sampleRate);
			cloned[i] = copy;
		}
		return cloned;
	}

	private static RenderResult RenderSamplesCore(
		float[] samples,
		int sampleRate,
		int channels,
		RenderSettings settings,
		IReadOnlyList<EchoBiquadFilter>? parametricBands,
		double preampDb,
		IProgress<RenderProgress>? progress,
		CancellationToken ct)
	{
		if (samples == null || samples.Length == 0)
		{
			return new RenderResult(Array.Empty<float>(), sampleRate, channels, TimeSpan.Zero);
		}
		int num = ((channels <= 0) ? 1 : channels);
		int num2 = ((sampleRate > 0) ? sampleRate : 44100);
		float[] array = new float[samples.Length];
		long num3 = samples.Length / num;
		using DspChain dspChain = new DspChain(num2, num, settings, parametricBands, preampDb);
		int num4 = Math.Clamp(num2 / 10, 1024, 8192);
		long num5 = 0L;
		DateTime utcNow = DateTime.UtcNow;
		DateTime lastReport = DateTime.UtcNow;
		while (num5 < num3)
		{
			ct.ThrowIfCancellationRequested();
			int num6 = (int)Math.Min(num4, num3 - num5);
			int num7 = num6 * num;
			float[] array2 = new float[num7];
			Array.Copy(samples, num5 * num, array2, 0L, num7);
			dspChain.Process(array2, num);
			Array.Copy(array2, 0L, array, num5 * num, num7);
			num5 += num6;
			ReportIfDue(progress, ref lastReport, num5, num3, utcNow);
		}
		progress?.Report(new RenderProgress(1.0, TimeSpan.Zero));
		return new RenderResult(array, num2, num, TimeSpan.FromSeconds((double)array.Length / (double)num / (double)num2));
	}

	public static Task<RenderResult> RenderFileToMemoryAsync(string sourcePath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		if (HasActiveNativeVst3(settings))
		{
			return RunOnStaAsync(() => RenderFileToMemoryCore(sourcePath, settings, progress, ct));
		}
		return Task.FromResult(RenderFileToMemoryCore(sourcePath, settings, progress, ct));
	}

	private static RenderResult RenderFileToMemoryCore(string sourcePath, RenderSettings settings, IProgress<RenderProgress>? progress, CancellationToken ct)
	{
		using SafeAudioFileReader audioFileReader = new SafeAudioFileReader(sourcePath);
		int sampleRate = audioFileReader.WaveFormat.SampleRate;
		int channels = audioFileReader.WaveFormat.Channels;
		long num = Math.Max(1L, (long)Math.Ceiling(audioFileReader.TotalTime.TotalSeconds * (double)sampleRate));
		float[] array = new float[num * channels];
		using DspChain dspChain = new DspChain(sampleRate, channels, settings);
		float[] array2 = new float[Math.Clamp(sampleRate / 10, 1024, 8192) * channels];
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
