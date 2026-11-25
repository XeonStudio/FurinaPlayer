using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Core.Services;

public class SettingsService
{
	private readonly object _lock = new object();

	private readonly string _filePath;

	private AppSettings? _cache;

	public string FilePath => _filePath;

	public string BackupDirectory => Path.Combine(
		Path.GetDirectoryName(_filePath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer"),
		"backups");

	public SettingsService(string? filePath = null)
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer");
		_filePath = filePath ?? Path.Combine(path, "settings.json");
	}

	public AppSettings Load()
	{
		lock (_lock)
		{
			if (_cache != null)
			{
				return _cache;
			}
			try
			{
				if (File.Exists(_filePath))
				{
					string json = File.ReadAllText(_filePath);
					_cache = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
				}
				else
				{
					_cache = new AppSettings();
				}
			}
			catch
			{
				_cache = new AppSettings();
			}
			return _cache;
		}
	}

	public void Save(AppSettings settings)
	{
		lock (_lock)
		{
			_cache = settings;
			try
			{
				string directoryName = Path.GetDirectoryName(_filePath);
				if (!string.IsNullOrEmpty(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				string contents = JsonSerializer.Serialize(settings, new JsonSerializerOptions
				{
					WriteIndented = true
				});
				File.WriteAllText(_filePath, contents);
			}
			catch
			{
			}
		}
	}

	public void Update(Action<AppSettings> mutate)
	{
		lock (_lock)
		{
			AppSettings appSettings = Load();
			mutate(appSettings);
			Save(appSettings);
		}
	}

	private sealed class SettingsBackupEnvelope
	{
		public int SchemaVersion { get; set; } = 1;
		public string ExportedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
		public string MachineName { get; set; } = Environment.MachineName;
		public AppSettings? Settings { get; set; }
	}

	public void ExportBackupToFile(string destPath)
	{
		lock (_lock)
		{
			AppSettings current = Load();
			string? dir = Path.GetDirectoryName(destPath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			var envelope = new SettingsBackupEnvelope
			{
				SchemaVersion = 1,
				ExportedAtUtc = DateTime.UtcNow.ToString("O"),
				MachineName = Environment.MachineName,
				Settings = current
			};
			string json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(destPath, json);
		}
	}

	public AppSettings ImportBackupFromFile(string sourcePath, bool sanitizeForNewDevice = true)
	{
		lock (_lock)
		{
			string json = File.ReadAllText(sourcePath);
			AppSettings? imported = null;
			try
			{
				var env = JsonSerializer.Deserialize<SettingsBackupEnvelope>(json);
				if (env?.Settings != null)
				{
					imported = env.Settings;
				}
			}
			catch
			{
			}
			imported ??= JsonSerializer.Deserialize<AppSettings>(json) ?? throw new InvalidDataException("无法解析设置备份文件。");

			if (sanitizeForNewDevice)
			{
				imported.OutputDeviceId = null;
				if (!string.IsNullOrEmpty(imported.LocalWallpaperPath) && !File.Exists(imported.LocalWallpaperPath))
				{
					imported.LocalWallpaperPath = string.Empty;
				}
				if (!string.IsNullOrEmpty(imported.DynamicWallpaperPath) && !File.Exists(imported.DynamicWallpaperPath))
				{
					imported.DynamicWallpaperPath = string.Empty;
					imported.DynamicWallpaperEnabled = false;
				}
				if (!string.IsNullOrEmpty(imported.LyricFontFilePath) && !File.Exists(imported.LyricFontFilePath))
				{
					imported.LyricFontFilePath = string.Empty;
				}
			}

			Save(imported);
			return imported;
		}
	}

	public string? RunAutoBackupIfDue()
	{
		lock (_lock)
		{
			AppSettings s = Load();
			s.AppLaunchCounter = Math.Max(0, s.AppLaunchCounter) + 1;

			if (!s.AutoBackupEnabled)
			{
				Save(s);
				return null;
			}

			bool due = false;
			if (string.Equals(s.AutoBackupMode, "launch_count", StringComparison.OrdinalIgnoreCase))
			{
				int interval = Math.Max(1, s.AutoBackupLaunchInterval);
				due = (s.AppLaunchCounter % interval) == 0;
			}
			else
			{
				int days = Math.Max(1, s.AutoBackupPeriodDays);
				if (string.IsNullOrWhiteSpace(s.LastAutoBackupUtc) ||
					!DateTime.TryParse(s.LastAutoBackupUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastDt))
				{
					due = true;
				}
				else
				{
					due = (DateTime.UtcNow - lastDt).TotalDays >= days;
				}
			}

			if (!due)
			{
				Save(s);
				return null;
			}

			try
			{
				Directory.CreateDirectory(BackupDirectory);
				string fileName = $"FurinaPlayer_AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
				string dest = Path.Combine(BackupDirectory, fileName);
				ExportBackupToFile(dest);
				s.LastAutoBackupUtc = DateTime.UtcNow.ToString("O");
				Save(s);

				// Prune older auto-backups, keep latest 10
				var oldFiles = new DirectoryInfo(BackupDirectory)
					.GetFiles("FurinaPlayer_AutoBackup_*.json")
					.OrderByDescending(f => f.CreationTimeUtc)
					.Skip(10)
					.ToList();
				foreach (var f in oldFiles)
				{
					try { f.Delete(); } catch { }
				}
				return dest;
			}
			catch
			{
				Save(s);
				return null;
			}
		}
	}
}
