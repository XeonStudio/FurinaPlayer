using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FurinaPlayer.Core.Helpers;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Core.Services;

public class LibraryService : IDisposable
{
	private readonly LibraryDbContext _db;

	private readonly MetadataService _metadata = new MetadataService();

	private readonly string _coverDir;

	private readonly SemaphoreSlim _dbGate = new SemaphoreSlim(1, 1);

	public string CoverDirectory => _coverDir;

	public LibraryDbContext Db => _db;

	public LibraryService(string dbPath)
	{
		_db = new LibraryDbContext(dbPath);
		_coverDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? ".", "covers");
	}

	public async Task InitializeAsync()
	{
		try
		{
			SQLitePCL.Batteries_V2.Init();
		}
		catch
		{
		}
		await _db.Database.EnsureCreatedAsync();
		await DeduplicateExistingTracksAsync();
		await RepairCoverPathsAsync();
	}

	private async Task RepairCoverPathsAsync()
	{
		try
		{
			List<Track> list = await _db.Tracks.ToListAsync();
			bool flag = false;
			foreach (Track item in list)
			{
				if (!string.IsNullOrEmpty(item.CoverPath) && File.Exists(item.CoverPath))
				{
					continue;
				}

				if (!string.IsNullOrEmpty(item.CoverPath))
				{
					string fileName = Path.GetFileName(item.CoverPath);
					if (!string.IsNullOrWhiteSpace(fileName))
					{
						string text = Path.Combine(_coverDir, fileName);
						if (File.Exists(text))
						{
							item.CoverPath = text;
							flag = true;
							continue;
						}
					}
				}

				// If CoverPath is missing or broken, try extracting from file / same folder
				if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
				{
					string? extracted = _metadata.ExtractCoverToFile(item.FilePath, _coverDir);
					if (!string.IsNullOrEmpty(extracted) && File.Exists(extracted))
					{
						item.CoverPath = extracted;
						flag = true;
						continue;
					}
				}

				if (item.CoverPath != null)
				{
					item.CoverPath = null;
					flag = true;
				}
			}
			if (flag)
			{
				await _db.SaveChangesAsync();
			}
		}
		catch
		{
		}
	}
	public static string NormalizePath(string path)
	{
		try
		{
			return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return path.Trim();
		}
	}

	private static string BuildFileSignature(string filePath, long fileSize)
	{
		return $"{Path.GetFileName(filePath).ToLowerInvariant()}|{fileSize}";
	}

	private static string? BuildContentSignature(string? title, string? artist, double durationMs)
	{
		if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist)) return null;
		string t = title.Trim().ToLowerInvariant();
		string a = artist.Trim().ToLowerInvariant();
		if (t == "未知曲目" || a == "未知艺术家") return null;
		long sec = (long)Math.Round(durationMs / 1000.0);
		return $"{t}|{a}|{sec}";
	}

	private async Task DeduplicateExistingTracksAsync()
	{
		try
		{
			List<Track> all = await _db.Tracks.OrderBy(t => t.Id).ToListAsync();
			if (all.Count <= 1) return;
			var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var seenContent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var toRemove = new List<Track>();
			foreach (Track t in all)
			{
				string norm = NormalizePath(t.FilePath);
				if (!seenPaths.Add(norm))
				{
					toRemove.Add(t);
					continue;
				}
				string? sig = BuildContentSignature(t.Title, t.Artist, t.DurationMilliseconds);
				if (sig != null && !seenContent.Add(sig))
				{
					// Same song imported multiple times from different paths
					if (!File.Exists(t.FilePath) || seenPaths.Count > 0)
					{
						toRemove.Add(t);
					}
				}
			}
			if (toRemove.Count > 0)
			{
				_db.Tracks.RemoveRange(toRemove);
				await _db.SaveChangesAsync();
			}
		}
		catch { }
	}

	public async Task<(int Added, int Updated, int Failed)> ScanFolderAsync(string folder, IProgress<(string file, int total)>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(folder))
		{
			return (Added: 0, Updated: 0, Failed: 0);
		}
		List<string> files = (from f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
			where AudioFormatDetector.IsSupportedExtension(f)
			select f).ToList();
		var res = await ImportFilesBatchInternalAsync(files, progress, ct);
		return (Added: res.Added, Updated: res.Skipped, Failed: res.Failed);
	}

	public async Task<(int Added, int Skipped, int Failed)> ImportFilesBatchAsync(IEnumerable<string> filePaths, IProgress<(string file, int total)>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		List<string> files = filePaths
			.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f) && AudioFormatDetector.IsSupportedExtension(f))
			.ToList();
		return await ImportFilesBatchInternalAsync(files, progress, ct);
	}

	private async Task<(int Added, int Skipped, int Failed)> ImportFilesBatchInternalAsync(List<string> files, IProgress<(string file, int total)>? progress, CancellationToken ct)
	{
		if (files.Count == 0) return (0, 0, 0);

		int added = 0;
		int skipped = 0;
		int failed = 0;

		var existingTracks = await _db.Tracks
			.Select(t => new { t.FilePath, t.FileSize, t.Title, t.Artist, t.DurationMilliseconds })
			.ToListAsync(ct);

		var existingPaths = new HashSet<string>(existingTracks.Select(t => NormalizePath(t.FilePath)), StringComparer.OrdinalIgnoreCase);
		var existingFileSigs = new HashSet<string>(existingTracks.Where(t => t.FileSize > 0).Select(t => BuildFileSignature(t.FilePath, t.FileSize)), StringComparer.OrdinalIgnoreCase);
		var existingContentSigs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var t in existingTracks)
		{
			string? sig = BuildContentSignature(t.Title, t.Artist, t.DurationMilliseconds);
			if (sig != null) existingContentSigs.Add(sig);
		}

		SemaphoreSlim semaphore = new SemaphoreSlim(4);
		await Task.WhenAll(files.Select(async (string rawFile) =>
		{
			await semaphore.WaitAsync(ct);
			try
			{
				string file = NormalizePath(rawFile);
				long fileSize = 0;
				try { fileSize = new FileInfo(file).Length; } catch { }
				string fileSig = BuildFileSignature(file, fileSize);

				lock (existingPaths)
				{
					if (existingPaths.Contains(file) || (fileSize > 0 && existingFileSigs.Contains(fileSig)))
					{
						Interlocked.Increment(ref skipped);
						return;
					}
				}

				Track track = _metadata.ReadTrack(file);
				track.FilePath = file;
				string? contentSig = BuildContentSignature(track.Title, track.Artist, track.DurationMilliseconds);

				lock (existingPaths)
				{
					if (!existingPaths.Add(file) ||
						(fileSize > 0 && !existingFileSigs.Add(fileSig)) ||
						(contentSig != null && !existingContentSigs.Add(contentSig)))
					{
						Interlocked.Increment(ref skipped);
						return;
					}
				}

				track.CoverPath = _metadata.ExtractCoverToFile(file, _coverDir);
				lock (existingPaths)
				{
					_db.Tracks.Add(track);
					Interlocked.Increment(ref added);
				}
			}
			catch
			{
				Interlocked.Increment(ref failed);
			}
			finally
			{
				semaphore.Release();
			}
			progress?.Report((rawFile, files.Count));
		}));

		if (added > 0)
		{
			await _dbGate.WaitAsync(ct);
			try
			{
				await _db.SaveChangesAsync(ct);
			}
			finally
			{
				_dbGate.Release();
			}
		}
		return (Added: added, Skipped: skipped, Failed: failed);
	}

	public async Task<List<Track>> GetAllTracksAsync()
	{
		return await (from t in _db.Tracks
			orderby t.Album, t.DiscNumber, t.TrackNumber
			select t).ToListAsync();
	}

	public async Task UpdateTrackIntegrityAsync(long trackId, string? title, string? artist, string? album, string? embeddedLyrics, string? coverPath)
	{
		await _dbGate.WaitAsync();
		try
		{
			Track? existing = await _db.Tracks.FirstOrDefaultAsync(t => t.Id == trackId);
			if (existing == null) return;
			if (!string.IsNullOrWhiteSpace(title)) existing.Title = title;
			if (!string.IsNullOrWhiteSpace(artist)) existing.Artist = artist;
			if (!string.IsNullOrWhiteSpace(album)) existing.Album = album;
			if (!string.IsNullOrWhiteSpace(embeddedLyrics)) existing.EmbeddedLyrics = embeddedLyrics;
			if (!string.IsNullOrWhiteSpace(coverPath)) existing.CoverPath = coverPath;
			await _db.SaveChangesAsync();
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task<Track?> GetTrackByIdAsync(long id)
	{
		return await _db.Tracks.FirstOrDefaultAsync((Track t) => t.Id == id);
	}

	public async Task<List<Track>> GetTracksAsync(string? search = null)
	{
		List<Track> all = await (from t in _db.Tracks
			orderby t.Album, t.DiscNumber, t.TrackNumber
			select t).ToListAsync();

		if (string.IsNullOrWhiteSpace(search))
		{
			return all;
		}

		string trimmed = search.Trim();
		string[] tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return all
			.Select(t => (Track: t, Score: ScoreTrackMatch(t, trimmed, tokens)))
			.Where(x => x.Score > 0)
			.OrderByDescending(x => x.Score)
			.ThenBy(x => x.Track.Album)
			.ThenBy(x => x.Track.TrackNumber)
			.Select(x => x.Track)
			.ToList();
	}

	public static int ScoreTrackMatch(Track t, string fullQuery, string[] tokens)
	{
		string fileName = string.Empty;
		try { fileName = Path.GetFileNameWithoutExtension(t.FilePath); } catch { }
		string[] fields = { t.Title ?? "", t.Artist ?? "", t.Album ?? "", t.AlbumArtist ?? "", t.Genre ?? "", fileName };

		// All whitespace-separated tokens must match at least one field (substring or fuzzy subsequence)
		int totalScore = 0;
		foreach (string token in tokens)
		{
			int bestTokenScore = 0;
			foreach (string f in fields)
			{
				int s = ScoreFieldMatch(f, token);
				if (s > bestTokenScore) bestTokenScore = s;
			}
			if (bestTokenScore == 0) return 0;
			totalScore += bestTokenScore;
		}

		// Bonus if full query matches Title/Artist/Album directly
		totalScore += ScoreFieldMatch(t.Title, fullQuery) * 2;
		totalScore += ScoreFieldMatch(t.Artist, fullQuery);
		totalScore += ScoreFieldMatch(t.Album, fullQuery);
		return totalScore;
	}

	public static int ScoreFieldMatch(string? field, string query)
	{
		if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(query)) return 0;
		string f = field.Trim();
		string q = query.Trim();
		if (f.Equals(q, StringComparison.OrdinalIgnoreCase)) return 100;
		if (f.StartsWith(q, StringComparison.OrdinalIgnoreCase)) return 80;
		if (f.Contains(q, StringComparison.OrdinalIgnoreCase)) return 60;

		// Fuzzy subsequence match (for queries >= 2 chars, e.g. "bmyy" or skipped characters)
		if (q.Length >= 2 && IsSubsequence(f, q)) return 25;
		return 0;
	}

	private static bool IsSubsequence(string text, string pattern)
	{
		int pi = 0;
		for (int ti = 0; ti < text.Length && pi < pattern.Length; ti++)
		{
			if (char.ToUpperInvariant(text[ti]) == char.ToUpperInvariant(pattern[pi]))
			{
				pi++;
			}
		}
		return pi == pattern.Length;
	}

	public async Task<List<Track>> GetRecentAsync(int limit = 50)
	{
		return await (from t in _db.Tracks
			where t.LastPlayedAt != default(DateTime)
			orderby t.LastPlayedAt descending
			select t).Take(limit).ToListAsync();
	}

	public async Task<List<Track>> GetFavoritesAsync()
	{
		return await (from t in _db.Tracks
			where t.IsFavorite
			orderby t.AddedAt descending
			select t).ToListAsync();
	}

	public async Task<List<Album>> GetAlbumsAsync()
	{
		return (from t in await _db.Tracks.ToListAsync()
			group t by (Album: t.Album, AlbumArtist: t.AlbumArtist) into g
			select new Album
			{
				Name = g.Key.Album,
				Artist = g.Key.AlbumArtist,
				Year = g.First().Year,
				CoverPath = g.Select((Track t) => t.CoverPath).FirstOrDefault((string p) => !string.IsNullOrWhiteSpace(p) && File.Exists(p)),
				Tracks = (from t in g
					orderby t.DiscNumber, t.TrackNumber
					select t).ToList()
			} into a
			orderby a.Name
			select a).ToList();
	}

	public async Task UpdateCoverPathAsync(long trackId, string coverPath)
	{
		await _dbGate.WaitAsync();
		try
		{
			Track track = await _db.Tracks.FindAsync(trackId);
			if (track != null)
			{
				track.CoverPath = coverPath;
				await _db.SaveChangesAsync();
			}
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task UpdatePlayStateAsync(long trackId)
	{
		await _dbGate.WaitAsync();
		try
		{
			Track track = await _db.Tracks.FindAsync(trackId);
			if (track != null)
			{
				track.LastPlayedAt = DateTime.Now;
				track.PlayCount++;
				await _db.SaveChangesAsync();
			}
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task ToggleFavoriteAsync(long trackId)
	{
		await _dbGate.WaitAsync();
		try
		{
			Track track = await _db.Tracks.FindAsync(trackId);
			if (track != null)
			{
				track.IsFavorite = !track.IsFavorite;
				await _db.SaveChangesAsync();
			}
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task RemoveTrackAsync(long trackId)
	{
		await _dbGate.WaitAsync();
		try
		{
			Track track = await _db.Tracks.FindAsync(trackId);
			if (track != null)
			{
				_db.Tracks.Remove(track);
				await _db.SaveChangesAsync();
			}
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task<(bool Added, bool Updated)> ImportFileAsync(string path)
	{
		var res = await ImportFilesBatchAsync(new[] { path });
		return (Added: res.Added > 0, Updated: false);
	}

	private static void ApplyMetadata(Track existing, Track track)
	{
		existing.Title = track.Title;
		existing.Artist = track.Artist;
		existing.Album = track.Album;
		existing.AlbumArtist = track.AlbumArtist;
		existing.Genre = track.Genre;
		existing.Year = track.Year;
		existing.TrackNumber = track.TrackNumber;
		existing.DiscNumber = track.DiscNumber;
		existing.DurationMilliseconds = track.DurationMilliseconds;
		existing.SampleRate = track.SampleRate;
		existing.BitDepth = track.BitDepth;
		existing.Bitrate = track.Bitrate;
		existing.FileSize = track.FileSize;
		existing.Format = track.Format;
	}

	public async Task<int> RemoveMissingFilesAsync()
	{
		List<Track> list = await _db.Tracks.ToListAsync();
		int removed = 0;
		foreach (Track item in list)
		{
			if (!File.Exists(item.FilePath))
			{
				_db.Tracks.Remove(item);
				removed++;
			}
		}
		if (removed > 0)
		{
			await _dbGate.WaitAsync();
			try
			{
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbGate.Release();
			}
		}
		return removed;
	}

	public async Task<int> CountAsync()
	{
		return await _db.Tracks.CountAsync();
	}

	public async Task<List<Playlist>> GetPlaylistsAsync()
	{
		return await _db.Playlists.ToListAsync();
	}

	public async Task SavePlaylistAsync(Playlist playlist)
	{
		await _dbGate.WaitAsync();
		try
		{
			Playlist playlist2 = await _db.Playlists.FindAsync(playlist.Id);
			if (playlist2 == null)
			{
				_db.Playlists.Add(playlist);
			}
			else
			{
				playlist2.Name = playlist.Name;
				playlist2.TrackIds = playlist.TrackIds;
			}
			await _db.SaveChangesAsync();
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public async Task DeletePlaylistAsync(long id)
	{
		await _dbGate.WaitAsync();
		try
		{
			Playlist playlist = await _db.Playlists.FindAsync(id);
			if (playlist != null)
			{
				_db.Playlists.Remove(playlist);
				await _db.SaveChangesAsync();
			}
		}
		finally
		{
			_dbGate.Release();
		}
	}

	public void Dispose()
	{
		_db.Dispose();
		SqliteConnection.ClearAllPools();
	}
}
