using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SonicWave.Audio.Decoders;

/// <summary>
/// A robust audio reader that wraps NAudio's AudioFileReader with transparent fallback
/// to StreamMediaFoundationReader via FlacSanitizingStream when dealing with FLAC files containing
/// oversized embedded artwork (which MediaFoundation URL readers fail to decode).
/// </summary>
public sealed class SafeAudioFileReader : WaveStream, ISampleProvider
{
	private readonly WaveStream _readerStream;
	private readonly SampleChannel _sampleChannel;
	private readonly long _length;

	public SafeAudioFileReader(string filePath)
	{
		WaveStream stream;
		if (StreamAudioCache.IsNetworkUrl(filePath))
		{
			if (StreamAudioCache.TryGetOrDownloadCachedFile(filePath, out string cachedPath) && File.Exists(cachedPath))
			{
				filePath = cachedPath;
				stream = OpenLocalWaveStream(filePath);
			}
			else
			{
				stream = new MediaFoundationReader(filePath);
			}
		}
		else
		{
			stream = OpenLocalWaveStream(filePath);
		}

		_readerStream = stream;
		_sampleChannel = new SampleChannel(_readerStream, false);
		_length = _readerStream.Length;
	}

	private static WaveStream OpenLocalWaveStream(string filePath)
	{
		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		if (LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(ext)
			&& LibVlcPcmTranscoder.TryDecodeToWav(filePath, out string predecodedWavPath))
		{
			return new WaveFileReader(predecodedWavPath);
		}

		try
		{
			return new AudioFileReader(filePath);
		}
		catch
		{
			if (ext == ".flac")
			{
				FlacSanitizingStream? sanitizingStream = null;
				try
				{
					sanitizingStream = new FlacSanitizingStream(filePath);
					return new StreamMediaFoundationReader(sanitizingStream);
				}
				catch
				{
					sanitizingStream?.Dispose();
				}
			}

			if (LibVlcPcmTranscoder.TryDecodeToWav(filePath, out string wavPath))
			{
				return new WaveFileReader(wavPath);
			}

			throw;
		}
	}

	public override WaveFormat WaveFormat => _sampleChannel.WaveFormat;

	public override long Length => _length;

	public override long Position
	{
		get
		{
			lock (_readerStream)
			{
				return _readerStream.Position;
			}
		}
		set
		{
			lock (_readerStream)
			{
				_readerStream.Position = value;
			}
		}
	}

	public override TimeSpan CurrentTime
	{
		get
		{
			lock (_readerStream)
			{
				return _readerStream.CurrentTime;
			}
		}
		set
		{
			lock (_readerStream)
			{
				_readerStream.CurrentTime = value;
			}
		}
	}

	public override TimeSpan TotalTime => _readerStream.TotalTime;

	public float Volume
	{
		get => _sampleChannel.Volume;
		set => _sampleChannel.Volume = value;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _readerStream.Read(buffer, offset, count);
	}

	public int Read(float[] buffer, int offset, int count)
	{
		return _sampleChannel.Read(buffer, offset, count);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_readerStream?.Dispose();
		}
		base.Dispose(disposing);
	}
}
