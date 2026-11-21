using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FurinaPlayer.Core.Services;

/// <summary>
/// 解密后的音频载荷与内嵌元信息
/// </summary>
public sealed class DecodedAudioResult
{
	public required byte[] AudioBytes { get; init; }
	public required string Container { get; init; } // "mp3", "flac", "ogg", "m4a", "wav", "aac", "bin"
	public required string SourceFormat { get; init; } // "ncm", "qmc-v1", "qmc-v2", "kgm-v2", "kgm-v3", "kwm"
	public string? Title { get; init; }
	public string? Artist { get; init; }
	public string? Album { get; init; }
	public byte[]? CoverBytes { get; init; }
}

public sealed class AudioDecodeOptions
{
	public string? EKey { get; set; }
	public string? EKeyDbPath { get; set; }
}

/// <summary>
/// 纯托管开源音频解密引擎：支持网易云 .ncm、QQ 音乐 .mgg/.mflac/.qmc*、酷狗 .kgm/.vpr、酷我 .kwm
/// </summary>
public static class AudioDecoders
{
	private static readonly HashSet<string> SupportedExts = new(StringComparer.OrdinalIgnoreCase)
	{
		".ncm",
		".mgg", ".mgg0", ".mgg1", ".mggl", ".mflac", ".mflac0", ".mmp4",
		".qmcflac", ".qmcogg", ".qmc0", ".qmc2", ".qmc3", ".qmc4", ".qmc6", ".qmc8",
		".tkm", ".bkcmp3", ".bkcm4a", ".bkcflac", ".bkcwav", ".bkcape", ".bkcogg", ".bkcwma",
		".kgm", ".kgma", ".vpr",
		".kwm"
	};

	public static bool IsSupportedExtension(string pathOrExt)
	{
		string ext = pathOrExt.StartsWith('.') ? pathOrExt : Path.GetExtension(pathOrExt);
		return SupportedExts.Contains(ext);
	}

	public static DecodedAudioResult DecodeFile(string filePath, AudioDecodeOptions? options = null)
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException("找不到待转换的音频文件", filePath);

		byte[] data = File.ReadAllBytes(filePath);
		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		options ??= new AudioDecodeOptions();

		return ext switch
		{
			".ncm" => NcmDecoder.Decode(data, Path.GetFileName(filePath)),
			".kgm" or ".kgma" or ".vpr" => KgmDecoder.Decode(data, Path.GetFileName(filePath)),
			".kwm" => KwmDecoder.Decode(data, Path.GetFileName(filePath)),
			_ when IsSupportedExtension(ext) => QmcDecoder.Decode(data, Path.GetFileName(filePath), ext, options),
			_ => throw new NotSupportedException($"不支持的私有音频格式: {ext}")
		};
	}

	public static string SniffContainer(ReadOnlySpan<byte> data)
	{
		int offset = 0;
		while (offset < data.Length && data[offset] == 0)
			offset++;

		var s = data[offset..];
		if (s.Length >= 4)
		{
			if (s[0] == 'f' && s[1] == 'L' && s[2] == 'a' && s[3] == 'C') return "flac";
			if (s[0] == 'O' && s[1] == 'g' && s[2] == 'g' && s[3] == 'S') return "ogg";
			if (s[0] == 'R' && s[1] == 'I' && s[2] == 'F' && s[3] == 'F') return "wav";
			if (s[0] == 'A' && s[1] == 'D' && s[2] == 'I' && s[3] == 'F') return "aac";
			if (s[0] == 'I' && s[1] == 'D' && s[2] == '3') return "mp3";
		}
		if (s.Length >= 8 && s[4] == 'f' && s[5] == 't' && s[6] == 'y' && s[7] == 'p')
			return "m4a";
		if (s.Length >= 2 && s[0] == 0xFF && (s[1] & 0xE0) == 0xE0)
		{
			// Distinguish ADTS AAC vs MPEG Audio layer
			if ((s[1] & 0x06) == 0x00) return "aac";
			return "mp3";
		}
		return "bin";
	}

	#region 网易云音乐 NCM 解密器
	public static class NcmDecoder
	{
		private static readonly byte[] Magic = "CTENFDAM"u8.ToArray();
		private static readonly byte[] CoreKey = "hzHRAmso5kInbaxW"u8.ToArray();
		private static readonly byte[] MetaKey = "#14ljk_!\\]&0U<'("u8.ToArray();
		private static readonly byte[] MetaPrefix = "163 key(Don't modify):"u8.ToArray();

		public static DecodedAudioResult Decode(byte[] data, string fileName)
		{
			if (data.Length < 0x2C || !data.AsSpan(0, 8).SequenceEqual(Magic))
				throw new InvalidDataException($"{fileName}: 不是合法的 NCM 文件");

			int pos = 10; // magic(8) + gap(2)
			int keyLen = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
			pos += 4;
			if (keyLen <= 0 || pos + keyLen > data.Length)
				throw new InvalidDataException($"{fileName}: NCM 密钥区长度非法");

			byte[] keyBlob = new byte[keyLen];
			for (int i = 0; i < keyLen; i++)
				keyBlob[i] = (byte)(data[pos + i] ^ 0x64);
			pos += keyLen;

			byte[] keyPlain = DecryptAesEcb(keyBlob, CoreKey);
			if (keyPlain.Length <= 17)
				throw new InvalidDataException($"{fileName}: NCM RC4 密钥无效");
			byte[] rc4Key = keyPlain[17..];

			int metaLen = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
			pos += 4;
			string? title = null, artist = null, album = null, format = null;
			if (metaLen > 0)
			{
				if (pos + metaLen > data.Length)
					throw new InvalidDataException($"{fileName}: NCM 元数据区长度非法");
				byte[] metaXor = new byte[metaLen];
				for (int i = 0; i < metaLen; i++)
					metaXor[i] = (byte)(data[pos + i] ^ 0x63);
				pos += metaLen;

				if (metaXor.AsSpan().StartsWith(MetaPrefix))
				{
					try
					{
						string b64 = Encoding.ASCII.GetString(metaXor, MetaPrefix.Length, metaXor.Length - MetaPrefix.Length);
						byte[] metaPlain = DecryptAesEcb(Convert.FromBase64String(b64), MetaKey);
						string json = Encoding.UTF8.GetString(metaPlain, 6, metaPlain.Length - 6); // skip "music:"
						using var doc = JsonDocument.Parse(json);
						var root = doc.RootElement;
						if (root.TryGetProperty("musicName", out var tProp)) title = tProp.GetString();
						if (root.TryGetProperty("album", out var aProp)) album = aProp.GetString();
						if (root.TryGetProperty("format", out var fProp)) format = fProp.GetString();
						if (root.TryGetProperty("artist", out var artProp) && artProp.ValueKind == JsonValueKind.Array)
						{
							var artists = new List<string>();
							foreach (var item in artProp.EnumerateArray())
							{
								if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
								{
									string? name = item[0].GetString();
									if (!string.IsNullOrWhiteSpace(name)) artists.Add(name);
								}
							}
							if (artists.Count > 0) artist = string.Join(" / ", artists);
						}
					}
					catch { }
				}
			}

			// 5 bytes gap (CRC32 + 5)
			pos += 5;
			byte[]? cover = null;
			if (pos + 8 <= data.Length)
			{
				int imageSpace = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
				pos += 4;
				int imageSize = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
				pos += 4;
				if (imageSize > 0 && pos + imageSize <= data.Length)
				{
					cover = data.AsSpan(pos, imageSize).ToArray();
				}
				pos = pos + imageSpace <= data.Length ? pos + imageSpace : data.Length;
			}

			byte[] box = BuildNcmStreamBox(rc4Key);
			int audioLen = data.Length - pos;
			byte[] audio = new byte[audioLen];
			for (int i = 0; i < audioLen; i++)
			{
				audio[i] = (byte)(data[pos + i] ^ box[(i + 1) & 0xFF]);
			}

			string sniffed = SniffContainer(audio.AsSpan(0, Math.Min(128, audio.Length)));
			string container = !string.IsNullOrWhiteSpace(format) ? format.ToLowerInvariant() : (sniffed != "bin" ? sniffed : "mp3");
			if (sniffed != "bin") container = sniffed;

			return new DecodedAudioResult
			{
				AudioBytes = audio,
				Container = container,
				SourceFormat = "ncm",
				Title = title,
				Artist = artist,
				Album = album,
				CoverBytes = cover
			};
		}

		private static byte[] DecryptAesEcb(byte[] cipher, byte[] key)
		{
			using var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.PKCS7;
			using var dec = aes.CreateDecryptor();
			return dec.TransformFinalBlock(cipher, 0, cipher.Length);
		}

		private static byte[] BuildNcmStreamBox(byte[] key)
		{
			byte[] sbox = new byte[256];
			for (int i = 0; i < 256; i++) sbox[i] = (byte)i;
			int j = 0;
			int klen = key.Length;
			for (int i = 0; i < 256; i++)
			{
				j = (j + sbox[i] + key[i % klen]) & 0xFF;
				(sbox[i], sbox[j]) = (sbox[j], sbox[i]);
			}
			byte[] box = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				box[i] = sbox[(sbox[i] + sbox[(i + sbox[i]) & 0xFF]) & 0xFF];
			}
			return box;
		}
	}
	#endregion

	#region QQ 音乐 QMC / MGG 解密器
	public static class QmcDecoder
	{
		private const int Boundary = 0x7FFF;
		private const int Key128 = 128;
		private const int MaxEKeyLen = 0x500;
		private const int MusicExBlock = 0xC0;

		private static readonly byte[] V1StaticKey =
		{
			0xC3, 0x4A, 0xD6, 0xCA, 0x90, 0x67, 0xF7, 0x52, 0xD8, 0xA1, 0x66, 0x62, 0x9F, 0x5B, 0x09, 0x00,
			0xC3, 0x5E, 0x95, 0x23, 0x9F, 0x13, 0x11, 0x7E, 0xD8, 0x92, 0x3F, 0xBC, 0x90, 0xBB, 0x74, 0x0E,
			0xC3, 0x47, 0x74, 0x3D, 0x90, 0xAA, 0x3F, 0x51, 0xD8, 0xF4, 0x11, 0x84, 0x9F, 0xDE, 0x95, 0x1D,
			0xC3, 0xC6, 0x09, 0xD5, 0x9F, 0xFA, 0x66, 0xF9, 0xD8, 0xF0, 0xF7, 0xA0, 0x90, 0xA1, 0xD6, 0xF3,
			0xC3, 0xF3, 0xD6, 0xA1, 0x90, 0xA0, 0xF7, 0xF0, 0xD8, 0xF9, 0x66, 0xFA, 0x9F, 0xD5, 0x09, 0xC6,
			0xC3, 0x1D, 0x95, 0xDE, 0x9F, 0x84, 0x11, 0xF4, 0xD8, 0x51, 0x3F, 0xAA, 0x90, 0x3D, 0x74, 0x47,
			0xC3, 0x0E, 0x74, 0xBB, 0x90, 0xBC, 0x3F, 0x92, 0xD8, 0x7E, 0x11, 0x13, 0x9F, 0x23, 0x95, 0x5E,
			0xC3, 0x00, 0x09, 0x5B, 0x9F, 0x62, 0x66, 0xA1, 0xD8, 0x52, 0xF7, 0x67, 0x90, 0xCA, 0xD6, 0x4A
		};

		private static readonly HashSet<string> V1Exts = new(StringComparer.OrdinalIgnoreCase)
		{
			".tkm", ".bkcmp3", ".bkcm4a", ".bkcflac", ".bkcwav", ".bkcape", ".bkcogg", ".bkcwma"
		};

		private static readonly byte[] EKeyV2Prefix = Encoding.ASCII.GetBytes(Convert.ToBase64String("QQMusic EncV2,Key:"u8));
		private static readonly byte[] EKeyV2Key1 = { 0x33, 0x38, 0x36, 0x5A, 0x4A, 0x59, 0x21, 0x40, 0x23, 0x2A, 0x24, 0x25, 0x5E, 0x26, 0x29, 0x28 };
		private static readonly byte[] EKeyV2Key2 = { 0x2A, 0x2A, 0x23, 0x21, 0x28, 0x23, 0x24, 0x25, 0x26, 0x5E, 0x61, 0x31, 0x63, 0x5A, 0x2C, 0x54 };
		private static readonly byte[] SimpleKey8 = BuildSimpleKey8();

		private sealed record FooterInfo(int Size, string? EKey, string Kind, long? ResourceId = null, string? Mid = null, string? MediaFilename = null);

		public static DecodedAudioResult Decode(byte[] data, string fileName, string ext, AudioDecodeOptions options)
		{
			if (V1Exts.Contains(ext))
			{
				byte[] v1Out = Qmc1Transform(data, V1StaticKey);
				return new DecodedAudioResult
				{
					AudioBytes = v1Out,
					Container = SniffContainer(v1Out.AsSpan(0, Math.Min(128, v1Out.Length))),
					SourceFormat = "qmc-v1"
				};
			}

			int tailLen = Math.Min(1024, data.Length);
			FooterInfo? footer = null;
			try
			{
				footer = ParseFooter(data.AsSpan(data.Length - tailLen, tailLen));
			}
			catch
			{
				footer = null;
			}

			if (footer == null)
			{
				// 尝试 v1 静态密钥兜底（部分 .qmc0/.qmc3/.qmcflac 属于 v1 格式）
				byte[] head = Qmc1Transform(data.AsSpan(0, Math.Min(128, data.Length)).ToArray(), V1StaticKey);
				if (SniffContainer(head) != "bin")
				{
					byte[] v1Out = Qmc1Transform(data, V1StaticKey);
					return new DecodedAudioResult
					{
						AudioBytes = v1Out,
						Container = SniffContainer(v1Out.AsSpan(0, Math.Min(128, v1Out.Length))),
						SourceFormat = "qmc-v1"
					};
				}
				throw new InvalidDataException($"{fileName}: 未找到 QMC v2 尾包或 QMC v1 静态密钥特征");
			}

			string? ekey = footer.EKey;
			if (string.IsNullOrEmpty(ekey)) ekey = options.EKey;
			if (string.IsNullOrEmpty(ekey) && !string.IsNullOrEmpty(options.EKeyDbPath))
			{
				ekey = LookupEKeyFromDb(options.EKeyDbPath, fileName, footer.Mid, footer.MediaFilename, footer.ResourceId?.ToString());
			}
			if (string.IsNullOrEmpty(ekey))
			{
				throw new InvalidOperationException($"{fileName}: {footer.Kind} 类型未内嵌 EKey（新版 QQ 音乐客户端已将密钥剥离至本地密钥库），请在设置中指定 player_process_db 或提供 EKey。");
			}

			byte[] masterKey = DeriveMasterKey(Encoding.Latin1.GetBytes(ekey));
			int payloadLen = data.Length - footer.Size;
			if (payloadLen <= 0)
				throw new InvalidDataException($"{fileName}: QMC 音频载荷长度无效");

			byte[] encrypted = data.AsSpan(0, payloadLen).ToArray();
			byte[] decrypted = masterKey.Length <= 300
				? DecryptMapCipher(encrypted, masterKey)
				: DecryptRc4Cipher(encrypted, masterKey);

			return new DecodedAudioResult
			{
				AudioBytes = decrypted,
				Container = SniffContainer(decrypted.AsSpan(0, Math.Min(128, decrypted.Length))),
				SourceFormat = $"qmc-v2:{footer.Kind}"
			};
		}

		private static FooterInfo? ParseFooter(ReadOnlySpan<byte> tail)
		{
			if (tail.Length < 8) return null;

			if (tail.EndsWith("STag"u8))
			{
				var body = tail[..^4];
				int payloadLen = BinaryPrimitives.ReadInt32BigEndian(body[^4..]);
				if (body.Length - 4 < payloadLen) throw new InvalidDataException("STag 长度不一致");
				string csv = Encoding.UTF8.GetString(body.Slice(body.Length - 4 - payloadLen, payloadLen));
				string[] parts = csv.Split(',');
				if (parts.Length != 3 || parts[1] != "2" || !long.TryParse(parts[0], out long resId))
					throw new InvalidDataException("STag 内容非法");
				return new FooterInfo(payloadLen + 8, null, "STag", ResourceId: resId, Mid: parts[2]);
			}

			if (tail.EndsWith("QTag"u8))
			{
				var body = tail[..^4];
				int payloadLen = BinaryPrimitives.ReadInt32BigEndian(body[^4..]);
				if (body.Length - 4 < payloadLen) throw new InvalidDataException("QTag 长度不一致");
				string csv = Encoding.UTF8.GetString(body.Slice(body.Length - 4 - payloadLen, payloadLen));
				string[] parts = csv.Split(',');
				if (parts.Length != 3 || parts[2] != "2" || !long.TryParse(parts[1], out long resId))
					throw new InvalidDataException("QTag 内容非法");
				return new FooterInfo(payloadLen + 8, parts[0], "QTag", ResourceId: resId);
			}

			if (tail.EndsWith("musicex\0"u8))
			{
				var payload = tail[..^8];
				if (payload.Length < 8) throw new InvalidDataException("MusicEx 过短");
				int version = BinaryPrimitives.ReadInt32LittleEndian(payload[^4..]);
				if (version != 1) throw new NotSupportedException($"MusicEx 版本 {version} 不支持");
				int payloadLen = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(payload.Length - 8, 4));
				if (payloadLen != MusicExBlock) throw new InvalidDataException($"MusicEx 长度非法: 0x{payloadLen:X}");
				var inner = payload.Slice(payload.Length - 8 - (payloadLen - 0x10), payloadLen - 0x10);
				string mid = ReadAsciiUtf16Le(inner.Slice(12, 60));
				string mediaFile = ReadAsciiUtf16Le(inner.Slice(72, 100));
				return new FooterInfo(MusicExBlock + 12, null, "MusicEx", Mid: mid, MediaFilename: mediaFile);
			}

			int legacyLen = BinaryPrimitives.ReadInt32LittleEndian(tail[^4..]);
			if (legacyLen <= 0 || legacyLen > MaxEKeyLen || tail.Length - 4 < legacyLen)
				return null;

			var ekeySpan = tail.Slice(tail.Length - 4 - legacyLen, legacyLen);
			int zero = ekeySpan.IndexOf((byte)0);
			if (zero != -1) ekeySpan = ekeySpan[..zero];
			if (!IsBase64Ascii(ekeySpan)) return null;

			return new FooterInfo(legacyLen + 4, Encoding.Latin1.GetString(ekeySpan), "PcV1Legacy");
		}

		private static string ReadAsciiUtf16Le(ReadOnlySpan<byte> data)
		{
			var sb = new StringBuilder();
			for (int i = 0; i + 1 < data.Length; i += 2)
			{
				byte lo = data[i], hi = data[i + 1];
				if (lo == 0 && hi == 0) break;
				if (hi == 0 && lo > 0 && lo < 128) sb.Append((char)lo);
				else break;
			}
			return sb.ToString();
		}

		private static bool IsBase64Ascii(ReadOnlySpan<byte> span)
		{
			if (span.IsEmpty) return false;
			foreach (byte c in span)
			{
				bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=';
				if (!ok) return false;
			}
			return true;
		}

		private static string? LookupEKeyFromDb(string dbPath, params string?[] names)
		{
			if (!File.Exists(dbPath)) return null;
			var candidates = new List<string>();
			foreach (var n in names)
			{
				if (string.IsNullOrWhiteSpace(n)) continue;
				candidates.Add(Path.GetFileName(n));
				candidates.Add(n);
			}

			try
			{
				using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
				conn.Open();
				var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
					using var r = cmd.ExecuteReader();
					while (r.Read()) tables.Add(r.GetString(0));
				}

				var knownTables = new (string Table, string PathCol, string KeyCol)[]
				{
					("audio_file_ekey_table", "file_path", "ekey"),
					("EKeyFileInfo", "filePath", "eKey"),
					("p2p_cache_info_table", "file_id", "ekey")
				};

				foreach (var (table, pathCol, keyCol) in knownTables)
				{
					if (!tables.Contains(table)) continue;
					using var cmd = conn.CreateCommand();
					cmd.CommandText = $"SELECT {pathCol}, {keyCol} FROM {table}";
					using var r = cmd.ExecuteReader();
					while (r.Read())
					{
						if (r.IsDBNull(0) || r.IsDBNull(1)) continue;
						string p = r.GetString(0);
						string k = r.GetString(1).Trim();
						string baseName = Path.GetFileName(p);
						foreach (var cand in candidates)
						{
							if (baseName.Equals(cand, StringComparison.OrdinalIgnoreCase) || (cand.Length >= 8 && p.Contains(cand, StringComparison.OrdinalIgnoreCase)))
								return k;
						}
					}
				}
			}
			catch { }
			return null;
		}

		private static byte[] BuildSimpleKey8()
		{
			byte[] res = new byte[8];
			for (int i = 0; i < 8; i++)
			{
				float arg = 106.0f + i * 0.1f;
				float val = MathF.Abs(MathF.Tan(arg)) * 100.0f;
				res[i] = (byte)Math.Clamp((int)val, 0, 255);
			}
			return res;
		}

		public static byte[] DeriveMasterKey(byte[] ekey)
		{
			if (ekey.AsSpan().StartsWith(EKeyV2Prefix))
			{
				string b64 = Encoding.ASCII.GetString(ekey, EKeyV2Prefix.Length, ekey.Length - EKeyV2Prefix.Length);
				byte[] payload = Convert.FromBase64String(b64);
				payload = TeaCbcDecrypt(payload, EKeyV2Key1);
				payload = TeaCbcDecrypt(payload, EKeyV2Key2);
				int zero = Array.IndexOf(payload, (byte)0);
				if (zero != -1) payload = payload[..zero];
				return DeriveEKeyV1(payload);
			}
			return DeriveEKeyV1(ekey);
		}

		private static byte[] DeriveEKeyV1(byte[] ekey)
		{
			byte[] decoded = Convert.FromBase64String(Encoding.ASCII.GetString(ekey));
			if (decoded.Length < 8) throw new InvalidDataException("EKey v1 解码后不足 8 字节");
			byte[] teaKey = new byte[16];
			for (int i = 0; i < 8; i++)
			{
				teaKey[i * 2] = SimpleKey8[i];
				teaKey[i * 2 + 1] = decoded[i];
			}
			byte[] decrypted = TeaCbcDecrypt(decoded[8..], teaKey);
			byte[] master = new byte[8 + decrypted.Length];
			Buffer.BlockCopy(decoded, 0, master, 0, 8);
			Buffer.BlockCopy(decrypted, 0, master, 8, decrypted.Length);
			return master;
		}

		private static byte[] TeaCbcDecrypt(byte[] cipher, byte[] key16)
		{
			if (cipher.Length % 8 != 0 || cipher.Length < 10)
				throw new InvalidDataException($"TEA 密文长度非法: {cipher.Length}");

			uint k0 = BinaryPrimitives.ReadUInt32BigEndian(key16.AsSpan(0, 4));
			uint k1 = BinaryPrimitives.ReadUInt32BigEndian(key16.AsSpan(4, 4));
			uint k2 = BinaryPrimitives.ReadUInt32BigEndian(key16.AsSpan(8, 4));
			uint k3 = BinaryPrimitives.ReadUInt32BigEndian(key16.AsSpan(12, 4));

			ulong ivPrev = 0, ivCur = 0;
			byte[] plain = new byte[cipher.Length];

			for (int i = 0; i < cipher.Length; i += 8)
			{
				ulong block = BinaryPrimitives.ReadUInt64BigEndian(cipher.AsSpan(i, 8));
				ulong mixed = block ^ ivCur;
				ulong nextIv = TeaDecryptBlock(mixed, k0, k1, k2, k3);
				ulong chunk = nextIv ^ ivPrev;
				BinaryPrimitives.WriteUInt64BigEndian(plain.AsSpan(i, 8), chunk);
				ivPrev = block;
				ivCur = nextIv;
			}

			int pad = plain[0] & 0x07;
			int bodyStart = 1 + pad + 2;
			int bodyEnd = cipher.Length - 7;
			for (int i = bodyEnd; i < plain.Length; i++)
			{
				if (plain[i] != 0) throw new InvalidDataException("TEA 尾部校验失败");
			}
			return plain[bodyStart..bodyEnd];
		}

		private static ulong TeaDecryptBlock(ulong block, uint k0, uint k1, uint k2, uint k3)
		{
			const uint delta = 0x9E3779B9;
			uint hi = (uint)(block >> 32);
			uint lo = (uint)(block & 0xFFFFFFFF);
			uint sum = unchecked(delta * 16);
			for (int r = 0; r < 16; r++)
			{
				lo = unchecked(lo - (((hi << 4) + k2) ^ (sum + hi) ^ ((hi >> 5) + k3)));
				hi = unchecked(hi - (((lo << 4) + k0) ^ (sum + lo) ^ ((lo >> 5) + k1)));
				sum = unchecked(sum - delta);
			}
			return ((ulong)hi << 32) | lo;
		}

		public static byte[] Qmc1Transform(byte[] data, byte[] key128, int offsetStart = 0)
		{
			byte[] outBuf = (byte[])data.Clone();
			int pos = offsetStart;
			for (int i = 0; i < outBuf.Length; i++, pos++)
			{
				int phase = pos <= Boundary ? (pos % Key128) : ((pos % Boundary) % Key128);
				outBuf[i] ^= key128[phase];
			}
			return outBuf;
		}

		private static byte[] DecryptMapCipher(byte[] data, byte[] masterKey)
		{
			int n = masterKey.Length;
			byte[] compressed = new byte[128];
			for (int i = 0; i < 128; i++)
			{
				int idx = (i * i + 71214) % n;
				int shift = (idx + 4) % 8;
				compressed[i] = (byte)(((masterKey[idx] << shift) | (masterKey[idx] >> shift)) & 0xFF);
			}
			return Qmc1Transform(data, compressed);
		}

		private static byte[] DecryptRc4Cipher(byte[] data, byte[] masterKey)
		{
			int n = masterKey.Length;
			byte[] state = new byte[n];
			for (int i = 0; i < n; i++) state[i] = (byte)(i & 0xFF);
			int j = 0;
			for (int i = 0; i < n; i++)
			{
				j = (j + state[i] + masterKey[i % n]) % n;
				(state[i], state[j]) = (state[j], state[i]);
			}

			const int firstSeg = 0x80;
			const int segSize = 0x1400;
			const int cacheLen = segSize + 512;
			byte[] keyStream = new byte[cacheLen];
			int ri = 0, rj = 0;
			for (int k = 0; k < cacheLen; k++)
			{
				ri = (ri + 1) % n;
				rj = (rj + state[ri]) % n;
				(state[ri], state[rj]) = (state[rj], state[ri]);
				keyStream[k] = state[(state[ri] + state[rj]) % n];
			}

			uint h = 1;
			foreach (byte v in masterKey)
			{
				if (v == 0) continue;
				uint nxt = unchecked(h * v);
				if (nxt == 0 || nxt <= h) break;
				h = nxt;
			}
			double hashVal = h;

			static int SegmentKey(int segId, int seed, double hv)
			{
				if (seed == 0) return 0;
				ulong denom = unchecked((ulong)(segId + 1) * (ulong)seed);
				return (int)(hv / denom * 100.0);
			}

			byte[] outBuf = (byte[])data.Clone();
			int total = outBuf.Length;
			int pos = 0, start = 0;

			if (pos < firstSeg)
			{
				int take = Math.Min(firstSeg - pos, total);
				for (int k = 0; k < take; k++)
				{
					int p = pos + k;
					outBuf[k] ^= masterKey[SegmentKey(p, masterKey[p % n], hashVal) % n];
				}
				start += take;
				pos += take;
			}

			while (start < total)
			{
				int segId = pos / segSize;
				int blockOff = pos % segSize;
				int seed = masterKey[segId % n];
				int skip = SegmentKey(segId, seed, hashVal) & 0x1FF;
				int take = Math.Min(segSize - blockOff, total - start);
				for (int k = 0; k < take; k++)
				{
					outBuf[start + k] ^= keyStream[skip + blockOff + k];
				}
				start += take;
				pos += take;
			}

			return outBuf;
		}
	}
	#endregion

	#region 酷狗音乐 KGM / VPR 解密器 (v2 / v3)
	public static class KgmDecoder
	{
		private static readonly byte[] KgmMagic = { 0x7C, 0xD5, 0x32, 0xEB, 0x86, 0x02, 0x7F, 0x4B, 0xA8, 0xAF, 0xA6, 0x8E, 0x0F, 0xFF, 0x99, 0x14 };
		private static readonly byte[] VprMagic = { 0x05, 0x28, 0xBC, 0x96, 0xE9, 0xE4, 0x5A, 0x43, 0x91, 0xAA, 0xBD, 0xD0, 0x7A, 0xF5, 0x36, 0x31 };
		private static readonly byte[] SlotKey = "l,/'"u8.ToArray();
		private static readonly byte[] KgmSignature = { 0x38, 0x85, 0xED, 0x92, 0x79, 0x5F, 0xF8, 0x4C, 0xB3, 0x03, 0x61, 0x41, 0x16, 0xA0, 0x1D, 0x47 };
		private static readonly byte[] VprSignature = { 0x1D, 0x5A, 0x05, 0x34, 0x0C, 0x41, 0x8D, 0x42, 0x9C, 0x83, 0x92, 0x6C, 0xAE, 0x16, 0xFE, 0x56 };

		public static DecodedAudioResult Decode(byte[] data, string fileName)
		{
			if (data.Length < 0x40)
				throw new InvalidDataException($"{fileName}: 文件过短");

			bool isKgm = data.AsSpan(0, 16).SequenceEqual(KgmMagic);
			bool isVpr = data.AsSpan(0, 16).SequenceEqual(VprMagic);
			if (!isKgm && !isVpr)
				throw new InvalidDataException($"{fileName}: 不是合法的 KGM/VPR 文件");

			int audioOffset = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x10, 4));
			int cryptoVersion = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x14, 4));
			int slot = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x18, 4));
			if (slot != 1)
				throw new NotSupportedException($"{fileName}: 不支持的 KGM 密钥槽位 ({slot})");
			if (audioOffset <= 0x3C || audioOffset >= data.Length)
				throw new InvalidDataException($"{fileName}: KGM 音频偏移量非法");

			byte[] signature = data.AsSpan(0x1C, 16).ToArray();
			byte[] fileKey = data.AsSpan(0x2C, 16).ToArray();

			byte[] outAudio = data.AsSpan(audioOffset).ToArray();

			if (cryptoVersion == 2)
			{
				DecryptT2(signature, 0, SlotKey);
				VerifySig(signature, isVpr, fileName);
				DecryptT2(outAudio, 0, SlotKey);
			}
			else if (cryptoVersion == 3)
			{
				var (slotDigest, fileDigest) = BuildT3Keys(SlotKey, fileKey);
				DecryptT3(signature, 0, slotDigest, fileDigest);
				VerifySig(signature, isVpr, fileName);
				DecryptT3(outAudio, 0, slotDigest, fileDigest);
			}
			else
			{
				throw new NotSupportedException($"{fileName}: KGM/KGG v{cryptoVersion} 需客户端密钥库，暂不支持离线解密");
			}

			return new DecodedAudioResult
			{
				AudioBytes = outAudio,
				Container = SniffContainer(outAudio.AsSpan(0, Math.Min(128, outAudio.Length))),
				SourceFormat = $"kgm-v{cryptoVersion}"
			};
		}

		private static void VerifySig(byte[] sig, bool isVpr, string fileName)
		{
			byte[] expected = isVpr ? VprSignature : KgmSignature;
			if (!sig.AsSpan().SequenceEqual(expected))
				throw new InvalidDataException($"{fileName}: KGM/VPR 密钥校验失败");
		}

		private static void DecryptT2(Span<byte> data, long offset, byte[] slotKey)
		{
			int slen = slotKey.Length;
			for (int i = 0; i < data.Length; i++)
			{
				byte v = data[i];
				byte s = slotKey[(int)((offset + i) % slen)];
				data[i] = (byte)(v ^ (v << 4) ^ s);
			}
		}

		private static (byte[] slotDigest, byte[] fileDigest) BuildT3Keys(byte[] slotKey, byte[] fileKey)
		{
			byte[] slotDigest = HashSwap(slotKey);
			byte[] fileRaw = HashSwap(fileKey);
			byte[] fileDigest = new byte[17];
			Buffer.BlockCopy(fileRaw, 0, fileDigest, 0, 16);
			fileDigest[16] = 0x6B;
			return (slotDigest, fileDigest);
		}

		private static byte[] HashSwap(byte[] input)
		{
			byte[] digest = MD5.HashData(input);
			for (int i = 0; i < 8; i += 2)
			{
				(digest[i], digest[14 - i]) = (digest[14 - i], digest[i]);
				(digest[i + 1], digest[15 - i]) = (digest[15 - i], digest[i + 1]);
			}
			return digest;
		}

		private static void DecryptT3(Span<byte> data, long offset, byte[] slotDigest, byte[] fileDigest)
		{
			int sLen = slotDigest.Length;
			int fLen = fileDigest.Length;
			for (int i = 0; i < data.Length; i++)
			{
				long j = offset + i;
				byte m = (byte)(j ^ (j >> 8) ^ (j >> 16) ^ (j >> 24));
				byte f = fileDigest[(int)(j % fLen)];
				byte s = slotDigest[(int)(j % sLen)];
				byte x = (byte)(data[i] ^ f);
				data[i] = (byte)(x ^ (x << 4) ^ s ^ m);
			}
		}
	}
	#endregion

	#region 酷我音乐 KWM 解密器
	public static class KwmDecoder
	{
		private const int HeaderLen = 1024;
		private const int KeyLen = 32;
		private static readonly byte[] Magic = "yeelion-kuwo"u8.ToArray();
		private static readonly byte[] RootKey = "MoOtOiTvINGwd2E6n0E1i7L5t2IoOoNk"u8.ToArray();

		public static DecodedAudioResult Decode(byte[] data, string fileName)
		{
			if (data.Length < HeaderLen + KeyLen * 2)
				throw new InvalidDataException($"{fileName}: KWM 文件过短");

			byte[]? chosenKey = null;

			// 策略 1: 官方 yeelion-kuwo 头部密钥派生
			if (data.AsSpan(0, Magic.Length).SequenceEqual(Magic))
			{
				uint seed = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x18, 4));
				string seedStr = seed.ToString();
				byte[] seedBytes = Encoding.ASCII.GetBytes(seedStr);
				byte[] mask = new byte[KeyLen];
				for (int i = 0; i < KeyLen; i++)
				{
					mask[i] = (byte)(seedBytes[i % seedBytes.Length] ^ RootKey[i]);
				}
				if (ProbeKey(data, mask) != "bin")
				{
					chosenKey = mask;
				}
			}

			// 策略 2: 零填充块重复密钥恢复（兼容老版本 KWM）
			if (chosenKey == null)
			{
				int bodyLen = data.Length - HeaderLen;
				int maxChunks = Math.Min(2048, bodyLen / KeyLen);
				var prev = data.AsSpan(HeaderLen, KeyLen);
				for (int i = 1; i < maxChunks; i++)
				{
					var chunk = data.AsSpan(HeaderLen + i * KeyLen, KeyLen);
					if (chunk.SequenceEqual(prev))
					{
						byte[] cand = chunk.ToArray();
						if (ProbeKey(data, cand) != "bin")
						{
							chosenKey = cand;
							break;
						}
					}
					prev = chunk;
				}
			}

			// 策略 3: 前 64 块暴力嗅探兜底
			if (chosenKey == null)
			{
				int bodyLen = data.Length - HeaderLen;
				int limit = Math.Min(64, bodyLen / KeyLen);
				for (int i = 0; i < limit; i++)
				{
					byte[] cand = data.AsSpan(HeaderLen + i * KeyLen, KeyLen).ToArray();
					if (ProbeKey(data, cand) != "bin")
					{
						chosenKey = cand;
						break;
					}
				}
			}

			if (chosenKey == null)
				throw new InvalidDataException($"{fileName}: 未能识别出有效的 KWM 解密密钥");

			int outLen = data.Length - HeaderLen;
			byte[] outAudio = new byte[outLen];
			for (int i = 0; i < outLen; i++)
			{
				outAudio[i] = (byte)(data[HeaderLen + i] ^ chosenKey[i & (KeyLen - 1)]);
			}

			return new DecodedAudioResult
			{
				AudioBytes = outAudio,
				Container = SniffContainer(outAudio.AsSpan(0, Math.Min(256, outAudio.Length))),
				SourceFormat = "kwm"
			};
		}

		private static string ProbeKey(byte[] data, byte[] key)
		{
			int len = Math.Min(4096, data.Length - HeaderLen);
			Span<byte> probe = stackalloc byte[len];
			for (int i = 0; i < len; i++)
				probe[i] = (byte)(data[HeaderLen + i] ^ key[i & (KeyLen - 1)]);
			return SniffContainer(probe);
		}
	}
	#endregion
}
