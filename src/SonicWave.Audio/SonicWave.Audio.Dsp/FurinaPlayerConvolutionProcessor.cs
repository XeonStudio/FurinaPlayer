using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using NAudio.Wave;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Information about an individual convolution impulse response filter.
/// </summary>
public sealed class ConvolutionFilterInfo
{
	public string Name { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public int SampleRate { get; set; } = 44100;
	public int Channels { get; set; } = 2;
	public int InputChannels { get; set; } = 2;
	public int Frames { get; set; } = 0;
	public int RoutingPaths { get; set; } = 2;
	public double DurationMs => SampleRate > 0 ? (Frames * 1000.0 / SampleRate) : 0.0;
}

/// <summary>
/// Filter bank containing multiple IRs (e.g. for multiple sample rates or Convolver cfg bundles).
/// </summary>
public sealed class ConvolutionFilterBank
{
	public string IrId { get; set; } = string.Empty;
	public string IrName { get; set; } = string.Empty;
	public string SourcePath { get; set; } = string.Empty;
	public int FilterCount => Filters.Count;
	public List<int> SampleRates { get; } = new();
	public List<int> ChannelCounts { get; } = new();
	public List<ConvolutionFilterInfo> Filters { get; } = new();
	public ConvolutionFilterInfo? SelectedFilter { get; set; }
}

/// <summary>
/// High-performance zero-latency FIR convolution engine for FurinaPlayer.
/// Supports WAV, FLAC, AIFF, ALAC, ZIP banks, and Convolver .cfg configurations.
/// </summary>
public class FurinaPlayerConvolutionProcessor
{
	public const int MaxTaps = 131072;
	public const int MaxCoefficients = 262144;
	private const int RingSize = MaxTaps;
	private const int BufferCapacity = RingSize * 2;

	protected double _sampleRate = 44100.0;
	protected bool _enabled = false;
	protected float _trimDb = 0.0f;
	protected float _trimGain = 1.0f;
	protected string _irName = string.Empty;
	protected string _irPath = string.Empty;
	protected string _channelMode = "stereo";
	protected string _lastError = string.Empty;

	// Impulse response taps per channel: [channel][tap]
	protected float[][] _taps = Array.Empty<float[]>();
	// Pre-reversed taps for forward SIMD dot-product: [channel][tap]
	protected float[][] _reversedTaps = Array.Empty<float[]>();
	protected int _tapCount = 0;
	protected int _filterSampleRate = 44100;
	protected int _filterChannels = 2;

	// Circular double-buffered history ring buffers per channel: [channel][BufferCapacity]
	protected float[][] _history = new float[2][];
	protected int _historyWriteIndex = 0;
	protected bool _clippingRisk = false;

	// Filter bank metadata
	protected ConvolutionFilterBank? _filterBank;

	public bool Enabled
	{
		get => _enabled && _tapCount > 0;
		set => _enabled = value;
	}

	public float TrimDb
	{
		get => _trimDb;
		set
		{
			_trimDb = Math.Clamp(value, -24.0f, 24.0f);
			_trimGain = (float)Math.Pow(10.0, _trimDb / 20.0);
		}
	}

	public float TrimGain => _trimGain;
	public string IrName => _irName;
	public string IrPath => _irPath;
	public int TapCount => _tapCount;
	public int FilterSampleRate => _filterSampleRate;
	public int FilterChannels => _filterChannels;
	public double SampleRate => _sampleRate;
	public string ChannelMode => _channelMode;
	public bool ClippingRisk => _clippingRisk;
	public string LastError => _lastError;
	public ConvolutionFilterBank? FilterBank => _filterBank;
	public ConvolutionFilterInfo? SelectedFilter => _filterBank?.SelectedFilter;

	public FurinaPlayerConvolutionProcessor()
	{
		InitHistory(2);
	}

	private void InitHistory(int channelCount)
	{
		int safeCount = Math.Clamp(channelCount, 1, 8);
		_history = new float[safeCount][];
		for (int i = 0; i < safeCount; i++)
		{
			_history[i] = new float[BufferCapacity];
		}
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Reset();
	}

	public void Reset()
	{
		_historyWriteIndex = 0;
		_clippingRisk = false;
		for (int i = 0; i < _history.Length; i++)
		{
			Array.Clear(_history[i], 0, _history[i].Length);
		}
	}

	public void ClearImpulseResponse()
	{
		_taps = Array.Empty<float[]>();
		_reversedTaps = Array.Empty<float[]>();
		_tapCount = 0;
		_irName = string.Empty;
		_irPath = string.Empty;
		_filterBank = null;
		_lastError = string.Empty;
		_enabled = false;
		Reset();
	}

	public bool LoadImpulseResponse(float[][] taps, double sourceSampleRate, string name = "Custom IR")
	{
		if (taps == null || taps.Length == 0 || taps[0].Length == 0)
		{
			ClearImpulseResponse();
			return false;
		}

		_irName = name;
		int channels = Math.Clamp(taps.Length, 1, 8);
		if (_history.Length < channels)
		{
			InitHistory(channels);
		}

		_filterSampleRate = (int)(sourceSampleRate > 0 ? sourceSampleRate : _sampleRate);
		_filterChannels = channels;
		_channelMode = channels == 1 ? "mono" : (channels == 2 ? "stereo" : "multichannel");

		double safeSourceRate = sourceSampleRate > 0 ? sourceSampleRate : _sampleRate;
		double safeTargetRate = _sampleRate > 0 ? _sampleRate : safeSourceRate;

		int rawSamples = taps[0].Length;
		int targetSamples = (int)Math.Ceiling(rawSamples * safeTargetRate / safeSourceRate);
		targetSamples = Math.Clamp(targetSamples, 1, MaxTaps);
		double ratio = safeSourceRate / safeTargetRate;

		_taps = new float[channels][];
		_reversedTaps = new float[channels][];

		for (int ch = 0; ch < channels; ch++)
		{
			_taps[ch] = new float[targetSamples];
			_reversedTaps[ch] = new float[targetSamples];
			float[] srcCh = taps[ch];

			for (int s = 0; s < targetSamples; s++)
			{
				double pos = s * ratio;
				int idx0 = (int)Math.Floor(pos);
				int idx1 = Math.Min(idx0 + 1, rawSamples - 1);
				float frac = (float)(pos - idx0);

				float v0 = idx0 < rawSamples ? srcCh[idx0] : 0f;
				float v1 = idx1 < rawSamples ? srcCh[idx1] : 0f;
				float interp = v0 + (v1 - v0) * frac;
				float val = float.IsFinite(interp) ? interp : 0f;
				_taps[ch][s] = val;
				_reversedTaps[ch][targetSamples - 1 - s] = val;
			}
		}

		_tapCount = targetSamples;
		_lastError = string.Empty;
		Reset();
		return true;
	}

	public bool LoadWavFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath)) return false;

			using var stream = File.OpenRead(filePath);
			using var reader = new BinaryReader(stream);

			// RIFF header
			string riff = new string(reader.ReadChars(4));
			if (riff != "RIFF") return false;
			reader.ReadInt32(); // fileSize
			string wave = new string(reader.ReadChars(4));
			if (wave != "WAVE") return false;

			int channels = 1;
			int sampleRate = 44100;
			int bitsPerSample = 16;
			short formatTag = 1; // 1 = PCM, 3 = IEEE Float, 0xFFFE = Extensible
			float[][]? rawTaps = null;

			while (stream.Position < stream.Length - 8)
			{
				string chunkId = new string(reader.ReadChars(4));
				int chunkSize = reader.ReadInt32();

				if (chunkId == "fmt ")
				{
					formatTag = reader.ReadInt16();
					channels = reader.ReadInt16();
					sampleRate = reader.ReadInt32();
					reader.ReadInt32(); // byteRate
					reader.ReadInt16(); // blockAlign
					bitsPerSample = reader.ReadInt16();
					int extra = chunkSize - 16;
					if (extra > 0)
					{
						if (formatTag == unchecked((short)0xFFFE) && extra >= 22)
						{
							reader.ReadInt16(); // cbSize
							reader.ReadInt16(); // validBitsPerSample
							reader.ReadInt32(); // channelMask
							int subGuid = reader.ReadInt32(); // SubFormat GUID first int: 1 = PCM, 3 = IEEE_FLOAT
							if (subGuid == 3) formatTag = 3;
							else if (subGuid == 1) formatTag = 1;
							int remaining = extra - 24;
							if (remaining > 0) reader.ReadBytes(remaining);
						}
						else
						{
							reader.ReadBytes(extra);
						}
					}
				}
				else if (chunkId == "data")
				{
					int bytesPerSample = bitsPerSample / 8;
					if (bytesPerSample <= 0 || channels <= 0) break;
					int sampleCount = chunkSize / (channels * bytesPerSample);
					sampleCount = Math.Min(sampleCount, MaxTaps);

					rawTaps = new float[channels][];
					for (int c = 0; c < channels; c++)
					{
						rawTaps[c] = new float[sampleCount];
					}

					for (int s = 0; s < sampleCount; s++)
					{
						for (int c = 0; c < channels; c++)
						{
							if (formatTag == 3) // IEEE Float
							{
								if (bitsPerSample == 32)
								{
									rawTaps[c][s] = reader.ReadSingle();
								}
								else if (bitsPerSample == 64)
								{
									rawTaps[c][s] = (float)reader.ReadDouble();
								}
								else
								{
									reader.ReadBytes(bytesPerSample);
								}
							}
							else // Integer PCM
							{
								if (bitsPerSample == 16)
								{
									short sample = reader.ReadInt16();
									rawTaps[c][s] = sample / 32768.0f;
								}
								else if (bitsPerSample == 24)
								{
									byte b0 = reader.ReadByte();
									byte b1 = reader.ReadByte();
									byte b2 = reader.ReadByte();
									int val = (b2 << 24) | (b1 << 16) | (b0 << 8);
									rawTaps[c][s] = val / 2147483648.0f;
								}
								else if (bitsPerSample == 32)
								{
									int sample = reader.ReadInt32();
									rawTaps[c][s] = sample / 2147483648.0f;
								}
								else
								{
									reader.ReadBytes(bytesPerSample);
								}
							}
						}
					}
					break;
				}
				else
				{
					if (chunkSize > 0 && stream.Position + chunkSize <= stream.Length)
					{
						reader.ReadBytes(chunkSize);
					}
					else
					{
						break;
					}
				}
			}

			if (rawTaps != null)
			{
				_irPath = filePath;
				return LoadImpulseResponse(rawTaps, sampleRate, Path.GetFileNameWithoutExtension(filePath));
			}
		}
		catch (Exception ex)
		{
			_lastError = ex.Message;
		}

		return false;
	}

	public bool LoadAudioFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath)) return false;

			using var reader = new AudioFileReader(filePath);
			int channels = Math.Clamp(reader.WaveFormat.Channels, 1, 8);
			int sampleRate = reader.WaveFormat.SampleRate;

			int sampleCount = (int)Math.Min((long)MaxTaps, reader.Length / (channels * sizeof(float)));
			float[] interleaved = new float[sampleCount * channels];
			int readCount = reader.Read(interleaved, 0, interleaved.Length);
			int readFrames = readCount / channels;

			float[][] taps = new float[channels][];
			for (int c = 0; c < channels; c++)
			{
				taps[c] = new float[readFrames];
				for (int i = 0; i < readFrames; i++)
				{
					taps[c][i] = interleaved[i * channels + c];
				}
			}

			_irPath = filePath;
			return LoadImpulseResponse(taps, sampleRate, Path.GetFileNameWithoutExtension(filePath));
		}
		catch (Exception ex)
		{
			_lastError = ex.Message;
			return false;
		}
	}

	public bool LoadZipBank(string zipPath)
	{
		try
		{
			if (!File.Exists(zipPath)) return false;

			string tempExtract = Path.Combine(Path.GetTempPath(), "FurinaConvolution_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempExtract);

			try
			{
				ZipFile.ExtractToDirectory(zipPath, tempExtract, true);

				var audioFiles = Directory.GetFiles(tempExtract, "*.*", SearchOption.AllDirectories)
					.Where(f =>
					{
						string ext = Path.GetExtension(f).ToLowerInvariant();
						return ext == ".wav" || ext == ".flac" || ext == ".aiff" || ext == ".aif" || ext == ".m4a" || ext == ".cfg";
					})
					.ToList();

				if (audioFiles.Count == 0)
				{
					_lastError = "convolution_zip_unsupported_entry";
					return false;
				}

				// Check for Convolver config (.cfg)
				var cfgFile = audioFiles.FirstOrDefault(f => Path.GetExtension(f).Equals(".cfg", StringComparison.OrdinalIgnoreCase));
				if (cfgFile != null)
				{
					bool okCfg = LoadConvolverConfigFile(cfgFile);
					if (okCfg)
					{
						_irName = Path.GetFileNameWithoutExtension(zipPath);
						_irPath = zipPath;
						return true;
					}
				}

				// Otherwise collect all candidate IR files
				var bank = new ConvolutionFilterBank
				{
					IrId = Path.GetFileNameWithoutExtension(zipPath),
					IrName = Path.GetFileNameWithoutExtension(zipPath),
					SourcePath = zipPath
				};

				foreach (var file in audioFiles.Where(f => !f.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase)))
				{
					try
					{
						using var reader = new AudioFileReader(file);
						var info = new ConvolutionFilterInfo
						{
							Name = Path.GetFileName(file),
							Path = file,
							SampleRate = reader.WaveFormat.SampleRate,
							Channels = reader.WaveFormat.Channels,
							InputChannels = reader.WaveFormat.Channels,
							Frames = (int)(reader.Length / (reader.WaveFormat.Channels * sizeof(float)))
						};
						bank.Filters.Add(info);
						if (!bank.SampleRates.Contains(info.SampleRate)) bank.SampleRates.Add(info.SampleRate);
						if (!bank.ChannelCounts.Contains(info.Channels)) bank.ChannelCounts.Add(info.Channels);
					}
					catch
					{
						// Skip unreadable files
					}
				}

				if (bank.Filters.Count == 0)
				{
					_lastError = "convolution_no_matching_channels";
					return false;
				}

				bank.SampleRates.Sort();
				bank.ChannelCounts.Sort();

				// Pick best matching filter for current playback sample rate
				var targetRate = (int)_sampleRate;
				var matched = bank.Filters.FirstOrDefault(f => f.SampleRate == targetRate)
							?? bank.Filters.OrderBy(f => Math.Abs(f.SampleRate - targetRate)).FirstOrDefault();

				if (matched != null)
				{
					bank.SelectedFilter = matched;
					_filterBank = bank;
					bool loaded = LoadFile(matched.Path);
					if (loaded)
					{
						_irName = $"{Path.GetFileNameWithoutExtension(zipPath)} ({matched.Name})";
						_irPath = zipPath;
						return true;
					}
				}
			}
			finally
			{
				try { Directory.Delete(tempExtract, true); } catch { }
			}
		}
		catch (Exception ex)
		{
			_lastError = ex.Message;
		}

		return false;
	}

	public bool LoadConvolverConfigFile(string cfgPath)
	{
		try
		{
			string dir = Path.GetDirectoryName(cfgPath) ?? string.Empty;
			string[] lines = File.ReadAllLines(cfgPath)
				.Select(l => l.Trim())
				.Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && !l.StartsWith("/"))
				.ToArray();

			if (lines.Length < 4) return false;

			// Header line: sampleRate inChannels outChannels mask
			string[] parts = lines[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3) return false;

			int sampleRate = int.Parse(parts[0]);
			int inChannels = int.Parse(parts[1]);
			int outChannels = int.Parse(parts[2]);

			// Look for referenced sound files in subsequent lines
			for (int i = 1; i < lines.Length; i++)
			{
				string refName = lines[i].Trim('"', '\'');
				string candidate = Path.Combine(dir, refName);
				if (File.Exists(candidate))
				{
					bool ok = LoadFile(candidate);
					if (ok)
					{
						_filterSampleRate = sampleRate;
						_irName = Path.GetFileNameWithoutExtension(cfgPath);
						_irPath = cfgPath;
						return true;
					}
				}
			}
		}
		catch (Exception ex)
		{
			_lastError = ex.Message;
		}

		return false;
	}

	public bool LoadFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			_lastError = "missing_file";
			return false;
		}

		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		bool result;

		if (ext == ".zip")
		{
			result = LoadZipBank(filePath);
		}
		else if (ext == ".cfg")
		{
			result = LoadConvolverConfigFile(filePath);
		}
		else if (ext == ".wav")
		{
			result = LoadWavFile(filePath);
			if (!result) result = LoadAudioFile(filePath);
		}
		else
		{
			result = LoadAudioFile(filePath);
		}

		if (result)
		{
			_irPath = filePath;
		}

		return result;
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || _tapCount <= 0 || count <= 0 || channels <= 0 || _taps.Length == 0)
		{
			_clippingRisk = false;
			return;
		}

		int frames = count / channels;
		int activeChannels = Math.Min(channels, _history.Length);
		int tapsChannels = _taps.Length;
		int tapsCount = Math.Min(_tapCount, MaxTaps);
		float trim = _trimGain;
		bool risk = false;
		int vecSize = Vector<float>.Count;

		for (int frame = 0; frame < frames; frame++)
		{
			int frameOffset = offset + frame * channels;

			// Write current input into circular double-history
			for (int ch = 0; ch < activeChannels; ch++)
			{
				float input = buffer[frameOffset + ch];
				float safeInput = float.IsFinite(input) ? input : 0f;
				_history[ch][_historyWriteIndex] = safeInput;
				_history[ch][_historyWriteIndex + RingSize] = safeInput;
			}

			// Past tapsCount samples are contiguous at [start .. start + tapsCount - 1]
			int start = _historyWriteIndex + RingSize - tapsCount + 1;

			// Compute SIMD zero-latency discrete convolution
			for (int ch = 0; ch < activeChannels; ch++)
			{
				int irCh = tapsChannels > 1 ? Math.Min(ch, tapsChannels - 1) : 0;
				float[] revTaps = _reversedTaps[irCh];
				float[] chHistory = _history[ch];

				Vector<float> vSum = Vector<float>.Zero;
				int k = 0;
				for (; k <= tapsCount - vecSize; k += vecSize)
				{
					var vTap = new Vector<float>(revTaps, k);
					var vHist = new Vector<float>(chHistory, start + k);
					vSum += vTap * vHist;
				}

				float sum = Vector.Dot(vSum, Vector<float>.One);
				for (; k < tapsCount; k++)
				{
					sum += revTaps[k] * chHistory[start + k];
				}

				float outSample = sum * trim;
				if (Math.Abs(outSample) > 0.98f)
				{
					risk = true;
				}

				buffer[frameOffset + ch] = outSample;
			}

			_historyWriteIndex = (_historyWriteIndex + 1) % RingSize;
		}

		_clippingRisk = risk;
	}
}
