using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using LibVLCSharp.Shared;
using SonicWave.Audio.Dsp;

namespace SonicWave.Audio.Decoders;

public sealed class LibVLCDecoder : IDisposable
{
	private LibVLC? _libVlc;

	private MediaPlayer? _player;

	private Media? _media;

	private double[]? _lastEqGains;

	public bool IsAvailable => _player != null;

	public TimeSpan Position => TimeSpan.FromMilliseconds(_player?.Time ?? 0);

	public TimeSpan Duration => TimeSpan.FromMilliseconds(_player?.Length ?? 0);

	public int Volume
	{
		get
		{
			return _player?.Volume ?? 100;
		}
		set
		{
			if (_player != null)
			{
				_player.Volume = Math.Clamp(value, 0, 200);
			}
		}
	}

	public double Speed
	{
		get
		{
			return ((double?)_player?.Rate) ?? 1.0;
		}
		set
		{
			if (_player != null)
			{
				_player.SetRate((float)Math.Clamp(value, 0.5, 3.0));
			}
		}
	}

	public bool IsPlaying => _player?.IsPlaying ?? false;

	public event Action? EndReached;

	public event Action? EncounteredError;

	public bool Initialize(string? extraArgs = null)
	{
		try
		{
			string text = ResolveLibVlcDirectory();
			if (text != null)
			{
				LibVLCSharp.Shared.Core.Initialize(text);
			}
			else
			{
				LibVLCSharp.Shared.Core.Initialize();
			}
			string[] options = new string[4]
			{
				"--no-video",
				"--quiet",
				"--audio-time-stretch",
				extraArgs ?? string.Empty
			};
			_libVlc = new LibVLC(options);
			_player = new MediaPlayer(_libVlc);
			_player.EndReached += (object? _, EventArgs _) =>
			{
				EndReached?.Invoke();
			};
			_player.EncounteredError += (object? _, EventArgs _) =>
			{
				EncounteredError?.Invoke();
			};
			return true;
		}
		catch
		{
			_player?.Dispose();
			_player = null;
			_libVlc?.Dispose();
			_libVlc = null;
			return false;
		}
	}

	private static string? ResolveLibVlcDirectory()
	{
		string baseDirectory = AppContext.BaseDirectory;
		string[] array = new string[4]
		{
			Path.Combine(baseDirectory, "libvlc", "win-x64"),
			Path.Combine(baseDirectory, "libvlc", "win-x86"),
			Path.Combine(baseDirectory, "libvlc"),
			baseDirectory
		};
		foreach (string text in array)
		{
			try
			{
				if (File.Exists(Path.Combine(text, "libvlc.dll")))
				{
					return text;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public bool Open(string filePath, bool startPaused = false)
	{
		if (_libVlc == null || _player == null)
		{
			return false;
		}
		try
		{
			_media?.Dispose();
			_media = new Media(_libVlc, filePath, FromType.FromPath);
			if (_lastEqGains != null)
			{
				SetEqGains(_lastEqGains);
			}
			_player.Play(_media);
			if (startPaused)
			{
				WaitUntilPlaying();
				_player.Pause();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool WaitUntilPlaying(int timeoutMs = 2000)
	{
		if (_player == null)
		{
			return false;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			try
			{
				if (_player.IsPlaying || _player.Time > 0)
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
			Thread.Sleep(20);
		}
		try
		{
			return _player.Time > 0;
		}
		catch
		{
			return false;
		}
	}

	public void Play()
	{
		_player?.Play();
	}

	public void Pause()
	{
		if (_player != null && _player.IsPlaying)
		{
			_player.Pause();
		}
	}

	public void Stop()
	{
		_player?.Stop();
		_media?.Dispose();
		_media = null;
	}

	public bool Seek(TimeSpan position)
	{
		try
		{
			_player?.SeekTo(TimeSpan.FromMilliseconds(position.TotalMilliseconds));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public void SetEqGains(double[] gains)
	{
		_lastEqGains = gains;
		if (_player == null || _libVlc == null)
		{
			return;
		}
		try
		{
			LibVLCSharp.Shared.Equalizer equalizer = new LibVLCSharp.Shared.Equalizer();
			equalizer.SetPreamp(0f);
			uint bandCount = equalizer.BandCount;
			for (uint num = 0u; num < bandCount; num++)
			{
				double num2 = equalizer.BandFrequency(num);
				double value = 0.0;
				double num3 = double.MaxValue;
				for (int i = 0; i < SonicWave.Audio.Dsp.Equalizer.BandFrequencies.Length; i++)
				{
					double num4 = Math.Abs(SonicWave.Audio.Dsp.Equalizer.BandFrequencies[i] - num2);
					if (num4 < num3)
					{
						num3 = num4;
						value = ((i < gains.Length) ? gains[i] : 0.0);
					}
				}
				equalizer.SetAmp((float)Math.Clamp(value, -12.0, 12.0), num);
			}
			_player.SetEqualizer(equalizer);
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		_player?.Stop();
		_player?.Dispose();
		_player = null;
		_media?.Dispose();
		_media = null;
		_libVlc?.Dispose();
		_libVlc = null;
	}
}
