using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SonicWave.Audio.Output;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;

namespace FurinaPlayer.UI.ViewModels;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Data.INotifyPropertyChanged")]
[WinRTExposedType(typeof(SettingsViewModelWinRTTypeDetails))]
public class SettingsViewModel : ObservableObject
{
	private readonly SettingsService _settingsService;

	private readonly DeviceManager _deviceManager = new DeviceManager();

	private readonly AlbumArtService? _albumArt;

	private readonly DispatcherQueue? _uiDispatcher;

	private DispatcherQueueTimer? _cooldownTimer;

	private AppSettings _settings = new AppSettings();

	[ObservableProperty]
	private ObservableCollection<AudioDeviceInfo> wasapiDevices = new ObservableCollection<AudioDeviceInfo>();

	[ObservableProperty]
	private ObservableCollection<AudioDeviceInfo> asioDevices = new ObservableCollection<AudioDeviceInfo>();

	[ObservableProperty]
	private string outputModeText = "软件解码";

	[ObservableProperty]
	private string themeText = "跟随系统";

	[ObservableProperty]
	private string materialText = "Mica";

	[ObservableProperty]
	private string wallpaperText = "Bing 每日壁纸";

	[ObservableProperty]
	private string musicFoldersText = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor("WallpaperLocalVisibility")]
	private string localWallpaperPath = string.Empty;

	[ObservableProperty]
	private string lyricFontFamily = string.Empty;

	[ObservableProperty]
	private string lyricFontFilePath = string.Empty;

	[ObservableProperty]
	private string albumArtStatus = "未执行（手动触发，每分钟最多 2 次）";

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanFetchAlbumArt")]
	private bool isFetchingAlbumArt;

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanFetchAlbumArt")]
	[NotifyPropertyChangedFor("AlbumArtCooldownText")]
	private int albumArtCooldownSeconds;

	[ObservableProperty]
	private string selectedWasapiDeviceId = string.Empty;

	[ObservableProperty]
	private string selectedAsioDriver = string.Empty;

	[ObservableProperty]
	private int bufferMs = 200;

	[ObservableProperty]
	private bool volumeNormalization;

	[ObservableProperty]
	private bool gaplessPlayback = true;

	[ObservableProperty]
	private bool crossfadeEnabled;

	[ObservableProperty]
	private int crossfadeSeconds = 3;

	[ObservableProperty]
	private string eqPresetName = "平坦";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? refreshDevicesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? restoreDefaultsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? saveCommand;

	public Visibility WallpaperLocalVisibility
	{
		get
		{
			if (_settings.WallpaperSource != WallpaperSource.LocalImage)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public bool CanFetchAlbumArt
	{
		get
		{
			if (!IsFetchingAlbumArt)
			{
				return AlbumArtCooldownSeconds <= 0;
			}
			return false;
		}
	}

	public string AlbumArtCooldownText
	{
		get
		{
			if (AlbumArtCooldownSeconds <= 0)
			{
				return string.Empty;
			}
			return $"下次可刷新：{AlbumArtCooldownSeconds} 秒后";
		}
	}

	public AppSettings Settings => _settings;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<AudioDeviceInfo> WasapiDevices
	{
		get
		{
			return wasapiDevices;
		}
		[MemberNotNull("wasapiDevices")]
		set
		{
			if (!EqualityComparer<ObservableCollection<AudioDeviceInfo>>.Default.Equals(wasapiDevices, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WasapiDevices);
				wasapiDevices = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WasapiDevices);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<AudioDeviceInfo> AsioDevices
	{
		get
		{
			return asioDevices;
		}
		[MemberNotNull("asioDevices")]
		set
		{
			if (!EqualityComparer<ObservableCollection<AudioDeviceInfo>>.Default.Equals(asioDevices, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AsioDevices);
				asioDevices = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AsioDevices);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string OutputModeText
	{
		get
		{
			return outputModeText;
		}
		[MemberNotNull("outputModeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(outputModeText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OutputModeText);
				outputModeText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OutputModeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string ThemeText
	{
		get
		{
			return themeText;
		}
		[MemberNotNull("themeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(themeText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ThemeText);
				themeText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ThemeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string MaterialText
	{
		get
		{
			return materialText;
		}
		[MemberNotNull("materialText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(materialText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MaterialText);
				materialText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MaterialText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string WallpaperText
	{
		get
		{
			return wallpaperText;
		}
		[MemberNotNull("wallpaperText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(wallpaperText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WallpaperText);
				wallpaperText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WallpaperText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string MusicFoldersText
	{
		get
		{
			return musicFoldersText;
		}
		[MemberNotNull("musicFoldersText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(musicFoldersText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MusicFoldersText);
				musicFoldersText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MusicFoldersText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string LocalWallpaperPath
	{
		get
		{
			return localWallpaperPath;
		}
		[MemberNotNull("localWallpaperPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(localWallpaperPath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LocalWallpaperPath);
				localWallpaperPath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LocalWallpaperPath);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WallpaperLocalVisibility);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string LyricFontFamily
	{
		get
		{
			return lyricFontFamily;
		}
		[MemberNotNull("lyricFontFamily")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(lyricFontFamily, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LyricFontFamily);
				lyricFontFamily = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LyricFontFamily);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string LyricFontFilePath
	{
		get
		{
			return lyricFontFilePath;
		}
		[MemberNotNull("lyricFontFilePath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(lyricFontFilePath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LyricFontFilePath);
				lyricFontFilePath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LyricFontFilePath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string AlbumArtStatus
	{
		get
		{
			return albumArtStatus;
		}
		[MemberNotNull("albumArtStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(albumArtStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AlbumArtStatus);
				albumArtStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumArtStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsFetchingAlbumArt
	{
		get
		{
			return isFetchingAlbumArt;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isFetchingAlbumArt, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsFetchingAlbumArt);
				isFetchingAlbumArt = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsFetchingAlbumArt);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanFetchAlbumArt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public int AlbumArtCooldownSeconds
	{
		get
		{
			return albumArtCooldownSeconds;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(albumArtCooldownSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AlbumArtCooldownSeconds);
				albumArtCooldownSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumArtCooldownSeconds);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanFetchAlbumArt);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumArtCooldownText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedWasapiDeviceId
	{
		get
		{
			return selectedWasapiDeviceId;
		}
		[MemberNotNull("selectedWasapiDeviceId")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(selectedWasapiDeviceId, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedWasapiDeviceId);
				selectedWasapiDeviceId = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedWasapiDeviceId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedAsioDriver
	{
		get
		{
			return selectedAsioDriver;
		}
		[MemberNotNull("selectedAsioDriver")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(selectedAsioDriver, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedAsioDriver);
				selectedAsioDriver = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedAsioDriver);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public int BufferMs
	{
		get
		{
			return bufferMs;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(bufferMs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BufferMs);
				bufferMs = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BufferMs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool VolumeNormalization
	{
		get
		{
			return volumeNormalization;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(volumeNormalization, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VolumeNormalization);
				volumeNormalization = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VolumeNormalization);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool GaplessPlayback
	{
		get
		{
			return gaplessPlayback;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(gaplessPlayback, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GaplessPlayback);
				gaplessPlayback = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GaplessPlayback);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CrossfadeEnabled
	{
		get
		{
			return crossfadeEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(crossfadeEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CrossfadeEnabled);
				crossfadeEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CrossfadeEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public int CrossfadeSeconds
	{
		get
		{
			return crossfadeSeconds;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(crossfadeSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CrossfadeSeconds);
				crossfadeSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CrossfadeSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string EqPresetName
	{
		get
		{
			return eqPresetName;
		}
		[MemberNotNull("eqPresetName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(eqPresetName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqPresetName);
				eqPresetName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqPresetName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RefreshDevicesCommand => refreshDevicesCommand ?? (refreshDevicesCommand = new RelayCommand(RefreshDevices));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RestoreDefaultsCommand => restoreDefaultsCommand ?? (restoreDefaultsCommand = new RelayCommand(RestoreDefaults));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SaveCommand => saveCommand ?? (saveCommand = new RelayCommand(Save));

	public event Action<ThemeMode>? ThemeChanged;

	public event Action? DefaultsRestored;

	public event Action<BackgroundMaterial>? MaterialChanged;

	public event Action? AlbumCoverUpdated;

	public SettingsViewModel(SettingsService settingsService, AlbumArtService? albumArt = null)
	{
		try
		{
			_uiDispatcher = DispatcherQueue.GetForCurrentThread();
		}
		catch
		{
		}
		_settingsService = settingsService;
		_albumArt = albumArt;
		Load();
	}

	public async Task FetchMissingAlbumCoverAsync()
	{
		if (IsFetchingAlbumArt || _albumArt == null)
		{
			return;
		}
		IsFetchingAlbumArt = true;
		try
		{
			Track track = await _albumArt.FindMissingCoverTrackAsync();
			if (track == null)
			{
				AlbumArtStatus = "曲库中没有缺少封面的歌曲";
				return;
			}
			AlbumArtStatus = "正在为《" + track.DisplayTitle + "》搜索封面…";
			if (!(await AlbumArtService.WaitForSlotAsync(TimeSpan.FromSeconds(3.0))))
			{
				AlbumArtStatus = "已达限流上限，请稍后再试：" + AlbumArtService.RateStatusText();
				return;
			}
			(bool, string, Track) tuple = await _albumArt.FetchAndApplyCoverAsync(track);
			bool item = tuple.Item1;
			string item2 = tuple.Item2;
			AlbumArtStatus = (item ? ("封面已添加：《" + track.DisplayTitle + "》") : item2);
			if (item)
			{
				AlbumCoverUpdated?.Invoke();
			}
		}
		catch (Exception ex)
		{
			AlbumArtStatus = "操作异常：" + ex.Message;
		}
		finally
		{
			IsFetchingAlbumArt = false;
			StartAlbumArtCooldown();
		}
	}

	private void StartAlbumArtCooldown()
	{
		try
		{
			_cooldownTimer?.Stop();
			TimeSpan timeSpan = AlbumArtRateLimit.NextWait(DateTime.UtcNow);
			if (timeSpan <= TimeSpan.Zero)
			{
				AlbumArtCooldownSeconds = 0;
				return;
			}
			AlbumArtCooldownSeconds = Math.Max(1, (int)Math.Ceiling(timeSpan.TotalSeconds));
			if (_uiDispatcher == null)
			{
				return;
			}
			if (_cooldownTimer == null)
			{
				_cooldownTimer = _uiDispatcher.CreateTimer();
				_cooldownTimer.Interval = TimeSpan.FromSeconds(1.0);
				_cooldownTimer.Tick += (DispatcherQueueTimer _, object _) =>
				{
					AlbumArtCooldownSeconds = Math.Max(0, AlbumArtCooldownSeconds - 1);
					if (AlbumArtCooldownSeconds <= 0)
					{
						_cooldownTimer.Stop();
						AlbumArtStatus = AlbumArtService.RateStatusText();
					}
				};
			}
			_cooldownTimer.Start();
		}
		catch
		{
			AlbumArtCooldownSeconds = 0;
		}
	}

	public void Load()
	{
		_settings = _settingsService.Load();
		BackgroundMaterial backgroundMaterial = _settings.BackgroundMaterial;
		if ((uint)(backgroundMaterial - 1) <= 1u)
		{
			_settings.BackgroundMaterial = BackgroundMaterial.Mica;
			_settingsService.Save(_settings);
		}
		BufferMs = _settings.BufferMilliseconds;
		VolumeNormalization = _settings.VolumeNormalization;
		GaplessPlayback = _settings.GaplessPlayback;
		CrossfadeEnabled = _settings.CrossfadeEnabled;
		CrossfadeSeconds = _settings.CrossfadeSeconds;
		MusicFoldersText = string.Join("\n", _settings.MusicFolders);
		OutputModeText = _settings.OutputMode switch
		{
			OutputMode.WasapiShared => "WASAPI 共享", 
			OutputMode.WasapiExclusive => "WASAPI 独占", 
			OutputMode.Asio => "ASIO", 
			_ => "软件解码", 
		};
		ThemeText = _settings.Theme switch
		{
			ThemeMode.Dark => "深色", 
			ThemeMode.Light => "浅色", 
			_ => "跟随系统", 
		};
		MaterialText = _settings.BackgroundMaterial switch
		{
			BackgroundMaterial.MicaAlt => "Mica Alt", 
			BackgroundMaterial.Acrylic => "亚克力", 
			BackgroundMaterial.LiquidGlass => "液态玻璃", 
			BackgroundMaterial.LiquidGlassDwm => "液态玻璃（新版·重启生效）", 
			_ => "Mica", 
		};
		WallpaperText = _settings.WallpaperSource switch
		{
			WallpaperSource.LocalImage => "本地图片", 
			WallpaperSource.SolidColor => "纯色", 
			_ => "Bing 每日壁纸", 
		};
		LocalWallpaperPath = _settings.LocalWallpaperPath ?? string.Empty;
		LyricFontFamily = _settings.LyricFontFamily ?? string.Empty;
		LyricFontFilePath = _settings.LyricFontFilePath ?? string.Empty;
		SelectedWasapiDeviceId = _settings.OutputDeviceId ?? string.Empty;
		SelectedAsioDriver = _settings.OutputDeviceId ?? string.Empty;
		if (_settings.EqGains.All((double g) => g == 0.0))
		{
			EqPresetName = "平坦";
		}
		else
		{
			EqPresetName = "自定义";
		}
	}

	[RelayCommand]
	private void RefreshDevices()
	{
		WasapiDevices = new ObservableCollection<AudioDeviceInfo>(_deviceManager.EnumerateWasapiDevices());
		AsioDevices = new ObservableCollection<AudioDeviceInfo>(_deviceManager.EnumerateAsioDevices());
		if (string.IsNullOrEmpty(SelectedWasapiDeviceId) && WasapiDevices.Count > 0)
		{
			SelectedWasapiDeviceId = WasapiDevices.FirstOrDefault((AudioDeviceInfo d) => d.IsDefault)?.Id ?? WasapiDevices[0].Id;
		}
		if (string.IsNullOrEmpty(SelectedAsioDriver) && AsioDevices.Count > 0)
		{
			SelectedAsioDriver = AsioDevices[0].Id;
		}
	}

	public void SetOutputMode(OutputMode mode)
	{
		_settings.OutputMode = mode;
	}

	public void SetTheme(ThemeMode theme)
	{
		_settings.Theme = theme;
		ThemeChanged?.Invoke(theme);
	}

	public void SetMaterial(BackgroundMaterial material)
	{
		if ((uint)(material - 1) <= 1u)
		{
			material = BackgroundMaterial.Mica;
		}
		_settings.BackgroundMaterial = material;
		MaterialText = material switch
		{
			BackgroundMaterial.MicaAlt => "Mica Alt", 
			BackgroundMaterial.Acrylic => "亚克力", 
			BackgroundMaterial.LiquidGlass => "液态玻璃", 
			BackgroundMaterial.LiquidGlassDwm => "液态玻璃（新版·重启生效）", 
			_ => "Mica", 
		};
		_settingsService.Save(_settings);
		if (material != BackgroundMaterial.LiquidGlassDwm)
		{
			MaterialChanged?.Invoke(material);
		}
	}

	public void SetLyricFont(string? familyName, string? filePath)
	{
		_settings.LyricFontFamily = familyName ?? string.Empty;
		_settings.LyricFontFilePath = filePath;
		LyricFontFamily = _settings.LyricFontFamily;
		LyricFontFilePath = _settings.LyricFontFilePath ?? string.Empty;
		_settingsService.Save(_settings);
	}

	public void ResetLyricFont()
	{
		_settings.LyricFontFamily = string.Empty;
		_settings.LyricFontFilePath = null;
		LyricFontFamily = string.Empty;
		LyricFontFilePath = string.Empty;
		_settingsService.Save(_settings);
	}

	public void SetWallpaperSource(WallpaperSource source)
	{
		_settings.WallpaperSource = source;
		OnPropertyChanged("WallpaperLocalVisibility");
	}

	public void SetLocalWallpaperPath(string path)
	{
		_settings.LocalWallpaperPath = path;
		LocalWallpaperPath = path;
	}

	public void AddMusicFolder(string folder)
	{
		if (!_settings.MusicFolders.Contains(folder))
		{
			_settings.MusicFolders.Add(folder);
		}
		MusicFoldersText = string.Join("\n", _settings.MusicFolders);
	}

	public void RemoveMusicFolder(string folder)
	{
		_settings.MusicFolders.Remove(folder);
		MusicFoldersText = string.Join("\n", _settings.MusicFolders);
	}

	[RelayCommand]
	private void RestoreDefaults()
	{
		AppSettings appSettings = new AppSettings();
		appSettings.MusicFolders = _settings.MusicFolders ?? new List<string>();
		_settingsService.Save(appSettings);
		Load();
		DefaultsRestored?.Invoke();
	}

	[RelayCommand]
	private void Save()
	{
		_settings.BufferMilliseconds = BufferMs;
		_settings.VolumeNormalization = VolumeNormalization;
		_settings.GaplessPlayback = GaplessPlayback;
		_settings.CrossfadeEnabled = CrossfadeEnabled;
		_settings.CrossfadeSeconds = CrossfadeSeconds;
		_settings.OutputDeviceId = SelectedWasapiDeviceId;
		_settingsService.Save(_settings);
	}

	public void UpdateSettings(Action<AppSettings> mutate)
	{
		try
		{
			mutate(_settings);
			_settingsService.Save(_settings);
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		_deviceManager.Dispose();
	}
}
