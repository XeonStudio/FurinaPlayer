using System;
using System.Collections.Generic;
using System.IO;

namespace SonicWave.Audio.Decoders;

/// <summary>
/// A zero-copy virtual stream that transparently bypasses oversized FLAC metadata blocks
/// (such as embedded high-resolution album pictures > 512KB) which cause Windows Media Foundation
/// to fail with MF_E_UNSUPPORTED_BYTESTREAM_TYPE (0xC00D36C4).
/// </summary>
public sealed class FlacSanitizingStream : Stream
{
	private readonly FileStream _file;
	private readonly List<(long virtualStart, long fileStart, long length)> _segments = new();
	private readonly long _totalLength;
	private long _position;
	private readonly object _lock = new();

	public FlacSanitizingStream(string filePath)
	{
		_file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.RandomAccess);

		byte[] magic = new byte[4];
		int magicRead = _file.Read(magic, 0, 4);
		if (magicRead < 4 || magic[0] != 'f' || magic[1] != 'L' || magic[2] != 'a' || magic[3] != 'C')
		{
			// Not a valid FLAC header, pass through the entire stream unmodified
			_segments.Add((0, 0, _file.Length));
			_totalLength = _file.Length;
			_position = 0;
			return;
		}

		long audioStart = 0;
		bool isLast = false;
		var blocks = new List<(int type, bool isLast, long offset, int length)>();

		while (!isLast)
		{
			long blockOffset = _file.Position;
			int b0 = _file.ReadByte();
			if (b0 == -1) break;

			isLast = (b0 & 0x80) != 0;
			int blockType = b0 & 0x7F;

			int b1 = _file.ReadByte();
			int b2 = _file.ReadByte();
			int b3 = _file.ReadByte();
			if (b1 == -1 || b2 == -1 || b3 == -1) break;

			int len = (b1 << 16) | (b2 << 8) | b3;
			blocks.Add((blockType, isLast, blockOffset, len));

			_file.Position += len;
			if (isLast)
			{
				audioStart = _file.Position;
			}
		}

		if (audioStart == 0)
		{
			audioStart = _file.Position;
		}

		// Check if there are oversized PICTURE blocks (type 6) or other blocks > 512KB
		bool hasOversizedPicture = blocks.Exists(b => b.type == 6 && b.length > 512 * 1024);
		if (!hasOversizedPicture)
		{
			// File is within normal probing size; pass through directly
			_segments.Add((0, 0, _file.Length));
			_totalLength = _file.Length;
			_position = 0;
			return;
		}

		// Filter out oversized picture blocks
		var keepBlocks = blocks.FindAll(b => !(b.type == 6 && b.length > 512 * 1024));

		long curVirt = 0;
		// Segment 0: 'fLaC' header (4 bytes)
		_segments.Add((curVirt, 0, 4));
		curVirt += 4;

		// Retained metadata blocks
		for (int i = 0; i < keepBlocks.Count; i++)
		{
			var b = keepBlocks[i];
			_segments.Add((curVirt, b.offset, 4 + b.length));
			curVirt += 4 + b.length;
		}

		// Segment: Audio frames to the end of file
		long audioLen = Math.Max(0L, _file.Length - audioStart);
		if (audioLen > 0)
		{
			_segments.Add((curVirt, audioStart, audioLen));
			curVirt += audioLen;
		}

		_totalLength = curVirt;
		_position = 0;
	}

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => false;
	public override long Length => _totalLength;

	public override long Position
	{
		get
		{
			lock (_lock)
			{
				return _position;
			}
		}
		set
		{
			Seek(value, SeekOrigin.Begin);
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		lock (_lock)
		{
			long target = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => _position + offset,
				SeekOrigin.End => _totalLength + offset,
				_ => throw new ArgumentException("Invalid seek origin", nameof(origin))
			};

			if (target < 0) target = 0;
			if (target > _totalLength) target = _totalLength;
			_position = target;
			return _position;
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (buffer == null) throw new ArgumentNullException(nameof(buffer));
		if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();
		if (count == 0) return 0;

		lock (_lock)
		{
			if (_position >= _totalLength) return 0;

			int totalRead = 0;
			while (count > 0 && _position < _totalLength)
			{
				int segIdx = _segments.FindIndex(s => _position >= s.virtualStart && _position < s.virtualStart + s.length);
				if (segIdx < 0) break;

				var seg = _segments[segIdx];
				long inSegOffset = _position - seg.virtualStart;
				int toRead = (int)Math.Min(count, seg.length - inSegOffset);

				_file.Position = seg.fileStart + inSegOffset;
				int r = _file.Read(buffer, offset + totalRead, toRead);
				if (r <= 0) break;

				totalRead += r;
				_position += r;
				count -= r;
			}

			return totalRead;
		}
	}

	public override void Flush() { }

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_file.Dispose();
		}
		base.Dispose(disposing);
	}
}
