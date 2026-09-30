using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Output;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Decoders;

public sealed class NAudioDecoder : IDisposable
{
	private IWavePlayer? _player;

	private AudioFileReader? _reader;

	private DspSampleProvider? _dsp;

	private EventHandler<StoppedEventArgs>? _stoppedHandler;

	private readonly object _lock = new object();

	public int Generation { get; set; }

	public bool IsPlaying { get; private set; }

	public int SampleRate => _reader?.WaveFormat.SampleRate ?? 0;

	public TimeSpan Position
	{
		get
		{
			lock (_lock)
			{
				return _reader?.CurrentTime ?? TimeSpan.Zero;
			}
		}
	}

	public TimeSpan Duration
	{
		get
		{
			lock (_lock)
			{
				return _reader?.TotalTime ?? TimeSpan.Zero;
			}
		}
	}

	public float Volume
	{
		get
		{
			lock (_lock)
			{
				if (!(_player is WasapiOut { Volume: var volume }))
				{
					return _reader?.Volume ?? 1f;
				}
				return volume;
			}
		}
		set
		{
			lock (_lock)
			{
				float volume = Math.Clamp(value, 0f, 1f);
				if (_player is WasapiOut wasapiOut)
				{
					wasapiOut.Volume = volume;
				}
				if (_reader != null)
				{
					_reader.Volume = volume;
				}
			}
		}
	}

	public event Action<int, bool>? PlaybackStopped;

	public bool Open(string filePath, string? deviceId, string? asioDriver, int bufferMs = 200)
	{
		lock (_lock)
		{
			Close();
			try
			{
				_reader = new AudioFileReader(filePath);
				_dsp = new DspSampleProvider(_reader, _reader.WaveFormat.SampleRate, _reader.WaveFormat.Channels);
				SampleToWaveProvider waveProvider = new SampleToWaveProvider(_dsp);
				if (asioDriver != null)
				{
					_player = new AsioOut(asioDriver);
				}
				else if (deviceId != null)
				{
					using MMDeviceEnumerator mMDeviceEnumerator = new MMDeviceEnumerator();
					using MMDevice device = mMDeviceEnumerator.GetDevice(deviceId);
					_player = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: false, bufferMs);
				}
				else
				{
					_player = new WaveOutEvent
					{
						DesiredLatency = bufferMs
					};
				}
				_player.Init(waveProvider);
				int gen = Generation;
				_stoppedHandler = (object? _, StoppedEventArgs e) =>
				{
					OnPlaybackStopped(gen, e);
				};
				_player.PlaybackStopped += _stoppedHandler;
				return true;
			}
			catch
			{
				Close();
				return false;
			}
		}
	}

	private void OnPlaybackStopped(int gen, StoppedEventArgs e)
	{
		IsPlaying = false;
		PlaybackStopped?.Invoke(gen, e.Exception != null);
	}

	public bool OpenForReading(string filePath)
	{
		lock (_lock)
		{
			Close();
			try
			{
				_reader = new AudioFileReader(filePath);
				_dsp = new DspSampleProvider(_reader, _reader.WaveFormat.SampleRate, _reader.WaveFormat.Channels);
				return true;
			}
			catch
			{
				Close();
				return false;
			}
		}
	}

	public void Play()
	{
		lock (_lock)
		{
			if (_player != null && _reader != null)
			{
				_player.Play();
				IsPlaying = true;
			}
		}
	}

	public void Pause()
	{
		lock (_lock)
		{
			_player?.Pause();
			IsPlaying = false;
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			_player?.Stop();
			IsPlaying = false;
		}
	}

	public bool Seek(TimeSpan position)
	{
		lock (_lock)
		{
			if (_reader == null)
			{
				return false;
			}
			try
			{
				_reader.CurrentTime = position;
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public void SetEqGains(double[] gains)
	{
		_dsp?.SetEqGains(gains);
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		_dsp?.SetSpatialAudio(enabled, mix, preset, width, modulation, predelayMs);
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SetSpatialAudio(enabled, mix, preset, SpatialParameters.Default.Width, SpatialParameters.Default.Modulation, SpatialParameters.Default.PredelayMs);
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_dsp?.SetTimbre(timbre);
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		_dsp?.SetVst3(plugins);
	}

	public int ReadSamples(float[] buffer, int offset, int count)
	{
		lock (_lock)
		{
			return _reader?.Read(buffer, offset, count) ?? 0;
		}
	}

	public void Close()
	{
		lock (_lock)
		{
			if (_player != null)
			{
				if (_stoppedHandler != null)
				{
					_player.PlaybackStopped -= _stoppedHandler;
				}
				_stoppedHandler = null;
				_player.Stop();
				_player.Dispose();
				_player = null;
			}
			_reader?.Dispose();
			_reader = null;
			_dsp = null;
			IsPlaying = false;
		}
	}

	public void Dispose()
	{
		Close();
	}
}
