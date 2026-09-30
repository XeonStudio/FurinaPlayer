using System;
using System.IO;
using System.Text.Json;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public class SettingsService
{
	private readonly object _lock = new object();

	private readonly string _filePath;

	private AppSettings? _cache;

	public string FilePath => _filePath;

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
}
