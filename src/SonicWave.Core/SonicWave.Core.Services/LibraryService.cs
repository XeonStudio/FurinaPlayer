using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SonicWave.Core.Helpers;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

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
		await RepairCoverPathsAsync();
	}

	private async Task RepairCoverPathsAsync()
	{
		_ = 1;
		try
		{
			List<Track> list = await _db.Tracks.Where((Track t) => t.CoverPath != null && t.CoverPath != "").ToListAsync();
			bool flag = false;
			foreach (Track item in list)
			{
				if (File.Exists(item.CoverPath))
				{
					continue;
				}
				string fileName = Path.GetFileName(item.CoverPath);
				if (string.IsNullOrWhiteSpace(fileName))
				{
					item.CoverPath = null;
					flag = true;
					continue;
				}
				string text = Path.Combine(_coverDir, fileName);
				if (File.Exists(text))
				{
					item.CoverPath = text;
					flag = true;
				}
				else
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

	public async Task<(int Added, int Updated, int Failed)> ScanFolderAsync(string folder, IProgress<(string file, int total)>? progress = null, CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(folder))
		{
			return (Added: 0, Updated: 0, Failed: 0);
		}
		List<string> files = (from f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
			where AudioFormatDetector.IsSupportedExtension(f)
			select f).ToList();
		int added = 0;
		int updated = 0;
		int failed = 0;
		HashSet<string> existingPaths = new HashSet<string>(await _db.Tracks.Select((Track t) => t.FilePath).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);
		SemaphoreSlim semaphore = new SemaphoreSlim(4);
		await Task.WhenAll(((IEnumerable<string>)files).Select((Func<string, Task>)(async (string file) =>
		{
			await semaphore.WaitAsync(ct);
			try
			{
				Track track = _metadata.ReadTrack(file);
				track.CoverPath = _metadata.ExtractCoverToFile(file, _coverDir);
				lock (existingPaths)
				{
					if (existingPaths.Add(file))
					{
						_db.Tracks.Add(track);
						Interlocked.Increment(ref added);
					}
					else
					{
						Track track2 = _db.Tracks.Local.FirstOrDefault((Track t) => t.FilePath == file) ?? _db.Tracks.FirstOrDefault((Track t) => t.FilePath == file);
						if (track2 != null)
						{
							ApplyMetadata(track2, track);
							if (track.CoverPath != null)
							{
								track2.CoverPath = track.CoverPath;
							}
							Interlocked.Increment(ref updated);
						}
					}
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
			progress?.Report((file, files.Count));
		})));
		await _dbGate.WaitAsync(ct);
		try
		{
			await _db.SaveChangesAsync(ct);
		}
		finally
		{
			_dbGate.Release();
		}
		return (Added: added, Updated: updated, Failed: failed);
	}

	public async Task<List<Track>> GetAllTracksAsync()
	{
		return await (from t in _db.Tracks
			orderby t.Album, t.DiscNumber, t.TrackNumber
			select t).ToListAsync();
	}

	public async Task<Track?> GetTrackByIdAsync(long id)
	{
		return await _db.Tracks.FirstOrDefaultAsync((Track t) => t.Id == id);
	}

	public async Task<List<Track>> GetTracksAsync(string? search = null)
	{
		IQueryable<Track> source = _db.Tracks.AsQueryable();
		if (!string.IsNullOrWhiteSpace(search))
		{
			string s = search.Trim();
			source = source.Where((Track t) => t.Title.Contains(s) || t.Artist.Contains(s) || t.Album.Contains(s));
		}
		return await (from t in source
			orderby t.Album, t.TrackNumber
			select t).ToListAsync();
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
		if (!File.Exists(path) || !AudioFormatDetector.IsSupportedExtension(path))
		{
			return (Added: false, Updated: false);
		}
		Track track = _metadata.ReadTrack(path);
		track.CoverPath = _metadata.ExtractCoverToFile(path, _coverDir);
		await _dbGate.WaitAsync();
		try
		{
			Track track2 = await _db.Tracks.FirstOrDefaultAsync((Track t) => t.FilePath == path);
			if (track2 == null)
			{
				_db.Tracks.Add(track);
				await _db.SaveChangesAsync();
				return (Added: true, Updated: false);
			}
			ApplyMetadata(track2, track);
			if (track.CoverPath != null)
			{
				track2.CoverPath = track.CoverPath;
			}
			await _db.SaveChangesAsync();
			return (Added: false, Updated: true);
		}
		finally
		{
			_dbGate.Release();
		}
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
