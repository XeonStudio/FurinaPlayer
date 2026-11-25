using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FurinaPlayer.UI;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using FurinaPlayer.App.FurinaPlayer_App_XamlTypeInfo;
using FurinaPlayer.Audio;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;
using WinRT;

namespace FurinaPlayer.App;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IApplicationOverrides")]
[WinRTExposedType(typeof(AppWinRTTypeDetails))]
public class App : Application, IXamlMetadataProvider
{
	private MainWindow? _window;

	private AudioEngine? _audioEngine;

	private LibraryService? _library;

	private SettingsService? _settingsService;

	private LyricService? _lyricService;

	private WallpaperService? _wallpaperService;

	private AppSettings? _settings;

	private AlbumArtService? _albumArtService;

	private long _lastSessionSaveMs;

	private NowPlayingViewModel? _nowPlayingVm;

	private DspViewModel? _dspVm;

	private SettingsViewModel? _settingsVm;

	private EditorViewModel? _editorVm;

	private StreamingViewModel? _streamingVm;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private XamlMetaDataProvider __appProvider;

	public HomeViewModel? HomeViewModel { get; private set; }

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	private XamlMetaDataProvider _AppProvider
	{
		get
		{
			if (__appProvider == null)
			{
				__appProvider = new XamlMetaDataProvider();
			}
			return __appProvider;
		}
	}

	public App()
	{
		CrashReporter.Initialize();
		Log("App ctor start");
		InitializeComponent();
		Log("App ctor init done");
		UnhandledException += OnUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += (object _, System.UnhandledExceptionEventArgs e) =>
		{
			Log("AppDomain UnhandledException: " + e.ExceptionObject);
			CrashReporter.WriteManaged(e.ExceptionObject as Exception, "AppDomain");
		};
		TaskScheduler.UnobservedTaskException += (object? _, UnobservedTaskExceptionEventArgs e) =>
		{
			CrashReporter.WriteManaged(e.Exception, "UnobservedTask");
			e.SetObserved();
		};
		Log("App ctor done");
	}

	private static void ClearGlobalGlassHighlightBrush(ResourceDictionary? dict)
	{
		if (dict == null) return;
		try
		{
			var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			dict["GlassHighlightBrush"] = transparent;
			foreach (var merged in dict.MergedDictionaries)
			{
				ClearGlobalGlassHighlightBrush(merged);
			}
			foreach (var themeDict in dict.ThemeDictionaries.Values.OfType<ResourceDictionary>())
			{
				ClearGlobalGlassHighlightBrush(themeDict);
			}
		}
		catch
		{
		}
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		LaunchCoreAsync();
	}

	private async Task LaunchCoreAsync()
	{
		try
		{
			Log("OnLaunched start");
			ClearGlobalGlassHighlightBrush(Resources);
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer");
			MigrateLegacyData(text);
			if (!IsDirectoryWritable(text))
			{
				text = Path.Combine(AppContext.BaseDirectory, "Data");
				Directory.CreateDirectory(text);
			}
			_settingsService = new SettingsService(Path.Combine(text, "settings.json"));
			AppSettings settings = _settingsService.Load();
			_settingsService.RunAutoBackupIfDue();
			Loc.SetLanguageSilently(settings.Language);
			_settings = settings;
			_wallpaperService = new WallpaperService(Path.Combine(text, "wallpapers"));
			Log("settings loaded");
			_library = new LibraryService(Path.Combine(text, "library.db"));
			Log("library init begin");
			await _library.InitializeAsync();
			Log("library init done");
			_lyricService = new LyricService();
			_audioEngine = new AudioEngine();
			Log("audio engine created");
			_audioEngine.ConfigureOutput(settings.OutputMode, settings.OutputMode == OutputMode.Asio ? null : settings.OutputDeviceId, settings.OutputMode == OutputMode.Asio ? settings.OutputDeviceId : null, settings.BufferMilliseconds);
			_audioEngine.Mode = settings.PlaybackMode;
			_audioEngine.SetEqGains(settings.EqGains);
			_audioEngine.SetFurinaPlayerHeadroom(settings.FurinaPlayerHeadroomDb);
			_audioEngine.SetFurinaPlayerCompressor(settings.FurinaPlayerCompressorEnabled, (float)settings.FurinaPlayerCompressorThresholdDb, (float)settings.FurinaPlayerCompressorRatio, (float)settings.FurinaPlayerCompressorAttackMs, (float)settings.FurinaPlayerCompressorReleaseMs, (float)settings.FurinaPlayerCompressorKneeDb, (float)settings.FurinaPlayerCompressorMakeupDb, 1.0f);
			_audioEngine.SetFurinaPlayerCrossfeed(settings.FurinaPlayerCrossfeedEnabled, (float)settings.FurinaPlayerCrossfeedAmount, (float)settings.FurinaPlayerCrossfeedCutoffHz);
			_audioEngine.SetFurinaPlayerStereoField(settings.FurinaPlayerStereoFieldEnabled, (float)settings.FurinaPlayerStereoWidth, (float)settings.FurinaPlayerStereoCenterGainDb, (float)settings.FurinaPlayerStereoSideGainDb);
			_audioEngine.SetFurinaPlayerChannelMatrix((FurinaPlayer.Audio.Dsp.ChannelMatrixPreset)settings.FurinaPlayerChannelMatrixPreset);
			_audioEngine.SetFurinaPlayerReplayGain((FurinaPlayer.Audio.Dsp.ReplayGainMode)settings.FurinaPlayerReplayGainMode, (float)settings.FurinaPlayerReplayGainPreampDb, settings.FurinaPlayerReplayGainPreventClipping);
			if (settings.FurinaPlayerConvolutionEnabled && !string.IsNullOrEmpty(settings.FurinaPlayerConvolutionFilePath))
			{
				_audioEngine.LoadFurinaPlayerConvolution(settings.FurinaPlayerConvolutionFilePath, (float)settings.FurinaPlayerConvolutionTrimDb);
			}
			_audioEngine.AutoFallbackExclusiveToShared = settings.AutoFallbackExclusiveToShared;
			_audioEngine.SetSoundEffect(settings.FurinaSoundEffectPreset, settings.FurinaSoundEffectIntensity);
			_audioEngine.Volume = settings.Volume;
			_audioEngine.VolumeChanged += (int v) =>
			{
				try
				{
					_settingsService?.Update((AppSettings s) =>
					{
						s.Volume = v;
					});
				}
				catch
				{
				}
			};
			Log("audio configured");
			_albumArtService = new AlbumArtService(_library, _library.CoverDirectory);
			HomeViewModel homeViewModel = new HomeViewModel(_library, _audioEngine, _settingsService, _albumArtService);
			_nowPlayingVm = new NowPlayingViewModel(_audioEngine, _lyricService, _library, _settingsService);
			EditorViewModel editorVm = (_editorVm = new EditorViewModel(_audioEngine, _settingsService));
			_settingsVm = new SettingsViewModel(_settingsService, _albumArtService);
			_dspVm = new DspViewModel(_audioEngine, _settingsService);
			_streamingVm = new StreamingViewModel(_audioEngine, _library, _albumArtService, _settingsService);
			_settingsVm.ThemeChanged += (ThemeMode t) =>
			{
				_window?.ApplyTheme(t);
			};
			_settingsVm.AutoFallbackExclusiveToSharedChanged += (bool enabled) =>
			{
				if (_audioEngine != null)
				{
					_audioEngine.AutoFallbackExclusiveToShared = enabled;
				}
			};
			_settingsVm.MaterialChanged += (BackgroundMaterial m) =>
			{
				_window?.ApplyMaterial(m);
			};
			_settingsVm.AlbumCoverUpdated += () =>
			{
				try
				{
					HomeViewModel?.RefreshCommand.Execute(null);
				}
				catch
				{
				}
				_nowPlayingVm?.RefreshCurrentCoverAsync();
			};
			_settingsVm.DefaultsRestored += ResetAllSettings;
			_settingsVm.OutputSettingsChanged += (mode, devId, asio, buf) => _audioEngine?.ConfigureOutput(mode, devId, asio, buf);
			HomeViewModel = homeViewModel;
			_audioEngine.TrackChanged += (Track? _) =>
			{
				SaveSession();
			};
			_audioEngine.PositionChanged += (TimeSpan _) =>
			{
				SaveSessionThrottled();
			};
			Log("creating MainWindow");
			_window = new MainWindow(homeViewModel, _nowPlayingVm, editorVm, _settingsVm, _dspVm, _streamingVm, _audioEngine);
			_window.SessionClosing += SaveSession;
			Log("MainWindow created");
			WindowManager.CurrentWindow = _window;
			_window.WallpaperRefreshRequested += () => ApplyWallpaperAsync(_settingsVm?.Settings ?? _settings);
			Log("activating window");
			_window.Activate();
			_window.ApplyFurinaPlayerAppearanceSettings();
			Log("window activated");
			ApplyWallpaperAsync(_settingsVm?.Settings ?? settings);
			Task.Run(() =>
			{
				RestoreSession(settings);
			});
			if (Environment.GetCommandLineArgs().Contains("--capture-pages"))
			{
				_ = CapturePagesAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "screenshots"));
			}
			Log("OnLaunched completed OK");
		}
		catch (Exception ex)
		{
			Log("OnLaunched EXCEPTION: " + ex);
			throw;
		}
	}

	private void ResetAllSettings()
	{
		try
		{
			AppSettings appSettings = new AppSettings();
			_audioEngine?.ConfigureOutput(appSettings.OutputMode, null, null, appSettings.BufferMilliseconds);
			if (_audioEngine != null)
			{
				_audioEngine.Mode = appSettings.PlaybackMode;
			}
			_audioEngine?.SetEqGains(appSettings.EqGains);
			_audioEngine?.SetReverb(enabled: false, appSettings.ReverbMix, "大厅");
			_audioEngine?.SetTimbre(new TimbreSettings());
			_audioEngine.Volume = appSettings.Volume;
			_window?.ApplyFurinaPlayerAppearanceSettings();
			_dspVm?.ReloadDefaults();
			_nowPlayingVm?.ApplyDefaults();
			_editorVm?.ResetDefaults();
			ApplyWallpaperAsync(appSettings);
			Log("settings defaults restored");
		}
		catch (Exception ex)
		{
			Log("restore defaults error: " + ex.Message);
		}
	}

	private void SaveSessionThrottled()
	{
		long tickCount = Environment.TickCount64;
		if (tickCount - _lastSessionSaveMs >= 5000)
		{
			_lastSessionSaveMs = tickCount;
			SaveSession();
		}
	}

	private void SaveSession()
	{
		try
		{
			if (_audioEngine == null || _settingsService == null)
			{
				return;
			}
			if (!_settingsService.Load().RememberPlaybackProgress)
			{
				_settingsService.Update((AppSettings s) =>
				{
					s.LastPlayedTrackPath = null;
					s.LastPlayedPositionMs = 0L;
					s.LastQueuePaths = new List<string>();
				});
				return;
			}
			Track track = _audioEngine.CurrentTrack;
			if (track == null)
			{
				_settingsService.Update((AppSettings s) =>
				{
					s.LastPlayedTrackPath = null;
					s.LastPlayedPositionMs = 0L;
					s.LastQueuePaths = new List<string>();
				});
				return;
			}
			PlaybackState state = _audioEngine.State;
			long posMs = 0L;
			if (state != PlaybackState.Stopped)
			{
				posMs = (long)Math.Max(0.0, _audioEngine.Position.TotalMilliseconds);
				double totalMilliseconds = _audioEngine.Duration.TotalMilliseconds;
				if (totalMilliseconds > 1000.0 && (double)posMs >= totalMilliseconds - 1000.0)
				{
					_settingsService.Update((AppSettings s) =>
					{
						s.LastPlayedTrackPath = null;
						s.LastPlayedPositionMs = 0L;
						s.LastQueuePaths = new List<string>();
					});
					return;
				}
			}
			else
			{
				posMs = (long)Math.Max(0.0, _audioEngine.Position.TotalMilliseconds);
			}
			List<string> paths = _audioEngine.Queue.Select((Track t) => t.FilePath).ToList();
			_settingsService.Update((AppSettings s) =>
			{
				s.LastPlayedTrackPath = track.FilePath;
				s.LastPlayedPositionMs = posMs;
				s.LastQueuePaths = paths;
			});
		}
		catch (Exception ex)
		{
			Log("session save error: " + ex.Message);
		}
	}

	private void RestoreSession(AppSettings settings)
	{
		try
		{
			if (_library == null || _audioEngine == null)
			{
				return;
			}
			List<Track> result = _library.GetAllTracksAsync().GetAwaiter().GetResult();
			if (result.Count == 0)
			{
				return;
			}
			if (!settings.RememberPlaybackProgress || string.IsNullOrWhiteSpace(settings.LastPlayedTrackPath))
			{
				if (settings.AutoPlayOnStartup)
				{
					_audioEngine.SetQueue(result, 0);
					_audioEngine.Play();
				}
				return;
			}
			Track? last = result.FirstOrDefault((Track t) => string.Equals(t.FilePath, settings.LastPlayedTrackPath, StringComparison.OrdinalIgnoreCase));
			if (last == null)
			{
				if (settings.AutoPlayOnStartup)
				{
					_audioEngine.SetQueue(result, 0);
					_audioEngine.Play();
				}
				return;
			}
			List<Track> list = new List<Track>();
			foreach (string p in settings.LastQueuePaths)
			{
				Track? track = result.FirstOrDefault((Track x) => string.Equals(x.FilePath, p, StringComparison.OrdinalIgnoreCase));
				if (track != null && !list.Contains(track))
				{
					list.Add(track);
				}
			}
			if (!list.Contains(last))
			{
				list.Add(last);
			}
			int num = list.FindIndex((Track t) => t == last);
			if (num < 0)
			{
				num = 0;
			}
			_audioEngine.SetQueue(list, num);
			TimeSpan position = TimeSpan.FromMilliseconds(Math.Max(0L, settings.LastPlayedPositionMs));
			_audioEngine.PrepareRestore(position, settings.AutoPlayOnStartup);
			Log("session restored: " + last.FilePath + " @" + position);
		}
		catch (Exception ex)
		{
			Log("session restore error: " + ex.Message);
		}
	}

	private async Task ApplyWallpaperAsync(AppSettings? settings)
	{
		_ = 1;
		try
		{
			if (_window == null)
			{
				return;
			}
			if (settings == null || settings.WallpaperSource == WallpaperSource.SolidColor)
			{
				_window.ApplyWallpaper(null, solid: true);
				return;
			}
			string image = null;
			if (settings.WallpaperSource == WallpaperSource.LocalImage && !string.IsNullOrWhiteSpace(settings.LocalWallpaperPath) && File.Exists(settings.LocalWallpaperPath))
			{
				image = settings.LocalWallpaperPath;
			}
			else if (settings.WallpaperSource == WallpaperSource.BingDaily && _wallpaperService != null)
			{
				List<WallpaperService.BingWallpaper> list = await _wallpaperService.GetBingWallpapersAsync();
				if (list.Count > 0)
				{
					image = await _wallpaperService.DownloadBingWallpaperAsync(list[0]);
				}
			}
			_window.ApplyWallpaper(image, solid: false);
			Log((image != null) ? ("wallpaper applied: " + image) : "wallpaper unavailable (offline/no source), fallback to system backdrop");
		}
		catch (Exception ex)
		{
			Log("wallpaper error: " + ex.Message);
		}
	}

	private static void MigrateLegacyData(string newDir)
	{
		try
		{
			if (Directory.Exists(newDir))
			{
				return;
			}
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer");
			if (!Directory.Exists(path))
			{
				return;
			}
			Directory.CreateDirectory(newDir);
			foreach (string item in Directory.EnumerateFileSystemEntries(path))
			{
				try
				{
					string fileName = Path.GetFileName(item);
					string text = Path.Combine(newDir, fileName);
					if (Directory.Exists(item))
					{
						Directory.Move(item, text);
					}
					else if (File.Exists(item))
					{
						File.Move(item, text);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsDirectoryWritable(string dir)
	{
		try
		{
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, ".write_probe");
			File.WriteAllText(path, "ok");
			File.Delete(path);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		Log("UnhandledException: " + e.Exception);
		e.Handled = true;
	}

	private static void Log(string s)
	{
		try
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + s + Environment.NewLine);
			CrashReporter.LogInfo(s);
		}
		catch
		{
		}
	}

	private async Task CapturePagesAsync(string outDir)
	{
		try
		{
			Directory.CreateDirectory(outDir);
			await Task.Delay(2500);
			string[] pages = new string[] { "home", "streaming", "playing", "editor", "dsp", "settings" };
			foreach (var page in pages)
			{
				_window?.DispatcherQueue.TryEnqueue(() =>
				{
					_window.NavigateTo(page);
					if (page == "dsp" && _window.MainContentFrame?.Content is DspPage dp)
					{
						dp.SetDspViewCategory("vst3");
					}
				});
				if (page == "playing")
				{
					try
					{
						if (_library != null && _audioEngine != null)
						{
							var allTracks = await _library.GetAllTracksAsync();
							var targetTrack = allTracks.FirstOrDefault(t => t.FilePath != null && t.FilePath.Contains("远航星")) ?? allTracks.FirstOrDefault(t => !string.IsNullOrEmpty(t.FilePath) && File.Exists(t.FilePath));
							if (targetTrack != null && _audioEngine.CurrentTrack?.Id != targetTrack.Id)
							{
								_window?.DispatcherQueue.TryEnqueue(() =>
								{
									int idx = Math.Max(0, allTracks.IndexOf(targetTrack));
									_audioEngine.SetQueue(allTracks, idx);
									_audioEngine.PlayTrack(targetTrack);
								});
								await Task.Delay(1500);
							}

							if (_audioEngine.State != FurinaPlayer.Audio.PlaybackState.Playing)
							{
								_window?.DispatcherQueue.TryEnqueue(() =>
								{
									if (_audioEngine.CurrentTrack != null)
									{
										_audioEngine.Play();
									}
									else if (targetTrack != null)
									{
										int idx = Math.Max(0, allTracks.IndexOf(targetTrack));
										_audioEngine.SetQueue(allTracks, idx);
										_audioEngine.PlayTrack(targetTrack);
									}
								});
								await Task.Delay(2000);
							}
						}
					}
					catch { }
					await Task.Delay(2500);
					if (_window?.MainContentFrame?.Content is NowPlayingPage npPage)
					{
						string dawPath = Path.Combine(outDir, "playing_audition.png");
						bool okDaw = await npPage.CaptureDawPreviewAsync(dawPath);
						Log("Captured playing_audition.png: " + okDaw);

						_window?.DispatcherQueue.TryEnqueue(() =>
						{
							npPage.SetLyricsMode(NowPlayingPage.LyricsPageMode.PowerPoint);
						});
						await Task.Delay(3000);
						string pptPath = Path.Combine(outDir, "playing_powerpoint.png");
						bool okPpt = await npPage.CaptureDawPreviewAsync(pptPath);
						Log("Captured playing_powerpoint.png: " + okPpt);

						_window?.DispatcherQueue.TryEnqueue(() =>
						{
							npPage.SetLyricsMode(NowPlayingPage.LyricsPageMode.Classic);
						});
						await Task.Delay(2500);
						_window?.DispatcherQueue.TryEnqueue(async () =>
						{
							try
							{
								if (_window.Content is Microsoft.UI.Xaml.UIElement rootElem)
								{
									var rtbC = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
									await rtbC.RenderAsync(rootElem);
									var pixelBuf = await rtbC.GetPixelsAsync();
									byte[] cBytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixelBuf);
									string cPath = Path.Combine(outDir, "playing_classic.png");
									using var cFs = File.Open(cPath, FileMode.Create);
									var cEnc = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(cFs));
									cEnc.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, (uint)rtbC.PixelWidth, (uint)rtbC.PixelHeight, 96, 96, cBytes);
									await cEnc.FlushAsync();
									Log("Captured playing_classic.png");
								}
							}
							catch (Exception ex)
							{
								Log("Classic capture error: " + ex);
							}
						});
						await Task.Delay(2000);

						// Keep in Classic mode so RenderTargetBitmap can safely render the XAML visual tree
						await Task.Delay(500);
					}
				}
				else
				{
					await Task.Delay(1200);
				}
				_window?.DispatcherQueue.TryEnqueue(async () =>
				{
					try
					{
						if (_window.Content is Microsoft.UI.Xaml.UIElement root)
						{
							var rtb = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
							await rtb.RenderAsync(root);
							var pixelBuffer = await rtb.GetPixelsAsync();
							byte[] bytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixelBuffer);
							string filePath = Path.Combine(outDir, page + ".png");
							using var fileStream = File.Open(filePath, FileMode.Create);
							var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(fileStream));
							encoder.SetPixelData(
								Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
								Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
								(uint)rtb.PixelWidth,
								(uint)rtb.PixelHeight,
								96, 96, bytes);
							await encoder.FlushAsync();
							if (page == "playing")
							{
								string dawCap = Path.Combine(outDir, "playing_audition.png");
								if (File.Exists(dawCap))
								{
									try
									{
										File.Copy(dawCap, filePath, overwrite: true);
										Log("Replaced playing.png with live DAW preview screenshot");
									}
									catch { }
								}
							}
							Log("Rendered page screenshot: " + filePath + " (" + rtb.PixelWidth + "x" + rtb.PixelHeight + ")");
						}
					}
					catch (Exception ex)
					{
						Log("Screenshot render failed for " + page + ": " + ex);
					}
				});
				await Task.Delay(1000);

				if (page == "dsp" && _window?.MainContentFrame?.Content is DspPage dpFree)
				{
					_window?.DispatcherQueue.TryEnqueue(() =>
					{
						dpFree.SetDspViewCategory("free");
					});
					await Task.Delay(1500);
					_window?.DispatcherQueue.TryEnqueue(async () =>
					{
						try
						{
							if (_window.Content is Microsoft.UI.Xaml.UIElement rootFree)
							{
								var rtbF = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
								await rtbF.RenderAsync(rootFree);
								var pixelBuf = await rtbF.GetPixelsAsync();
								byte[] fBytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixelBuf);
								string fPath = Path.Combine(outDir, "dsp_free.png");
								using var fFs = File.Open(fPath, FileMode.Create);
								var fEnc = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(fFs));
								fEnc.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, (uint)rtbF.PixelWidth, (uint)rtbF.PixelHeight, 96, 96, fBytes);
								await fEnc.FlushAsync();
								Log("Captured dsp_free.png (" + rtbF.PixelWidth + "x" + rtbF.PixelHeight + ")");
							}
						}
						catch (Exception ex)
						{
							Log("Captured dsp_free.png error: " + ex);
						}
					});
					await Task.Delay(1500);
				}
			}

			// Copy to brain artifact directory
			string artifactDir = @"C:\Users\Xeon\.gemini\antigravity\brain\f6fd29c5-40af-410c-9230-cce446de9781";
			if (Directory.Exists(artifactDir))
			{
				foreach (var file in Directory.GetFiles(outDir, "*.png"))
				{
					try
					{
						File.Copy(file, Path.Combine(artifactDir, Path.GetFileName(file)), overwrite: true);
					}
					catch { }
				}
			}
			Log("CapturePagesAsync finished all pages successfully!");
			await Task.Delay(1000);
			_window?.DispatcherQueue.TryEnqueue(() =>
			{
				_window.Close();
			});
		}
		catch (Exception ex)
		{
			Log("CapturePagesAsync exception: " + ex);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///App.xaml");
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(Type type)
	{
		return _AppProvider.GetXamlType(type);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(string fullName)
	{
		return _AppProvider.GetXamlType(fullName);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return _AppProvider.GetXmlnsDefinitions();
	}
}
