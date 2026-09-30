using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(NowPlayingPageWinRTTypeDetails))]
public sealed class NowPlayingPage : Page, IComponentConnector
{
	private static readonly double[] SpeedOptions = new double[7] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private LyricsPanel Lyrics;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private EqualizerControl Equalizer;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox EqPresetCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox SpeedCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Slider ProgressSlider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public NowPlayingViewModel ViewModel { get; set; }

	public NowPlayingPage()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public NowPlayingPage(NowPlayingViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is NowPlayingViewModel nowPlayingViewModel)
		{
			ViewModel = nowPlayingViewModel;
			DataContext = nowPlayingViewModel;
		}
	}

	private bool _layoutAdjusted;
	private TextBlock? _positionText;
	private TextBlock? _durationText;

	// DAW Mode Integration
	private static bool s_useDawMode = true;
	private bool _isWrapped;
	private Grid? _rootGrid;
	private Grid? _classicContainer;
	private Grid? _dawContainer;
	private WebView2? _webView;
	private Button? _modeSwitchBtn;
	private DispatcherQueueTimer? _meterTimer;
	private bool _isDawMode;
	private bool _dawReady;

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ApplyResponsiveLayout();
		SetupPageContainers();
		if (ViewModel != null)
		{
			ViewModel.Initialize();
			ViewModel.PropertyChanged += OnViewModelPropertyChanged;
			Lyrics.Attach(ViewModel);
			Lyrics.ApplyLyricFont(ViewModel.LyricFontFamily, ViewModel.LyricFontFilePath);
			Equalizer.Gains = ViewModel.EqGains;
			int num = Array.IndexOf(SpeedOptions, ViewModel.Speed);
			if (num >= 0)
			{
				SpeedCombo.SelectedIndex = num;
			}
			UpdateTimeTexts();
		}
	}

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(NowPlayingViewModel.Position) || e.PropertyName == nameof(NowPlayingViewModel.Duration))
		{
			UpdateTimeTexts();
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.CurrentTrack) ||
		         e.PropertyName == nameof(NowPlayingViewModel.CoverPath) ||
		         e.PropertyName == nameof(NowPlayingViewModel.Title))
		{
			if (_isDawMode)
			{
				SendFullDawState();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.IsPlaying))
		{
			if (_isDawMode)
			{
				SendDawPlaybackState();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.LyricLines))
		{
			if (_isDawMode)
			{
				SendDawLyrics();
			}
		}
	}

	private void UpdateTimeTexts()
	{
		if (ViewModel == null) return;
		if (_positionText != null)
		{
			_positionText.Text = FormatTime(ViewModel.Position);
		}
		if (_durationText != null)
		{
			_durationText.Text = FormatTime(ViewModel.Duration);
		}
	}

	private void ApplyResponsiveLayout()
	{
		if (_layoutAdjusted) return;
		_layoutAdjusted = true;
		try
		{
			if (Content is Grid rootGrid)
			{
				if (rootGrid.ColumnDefinitions.Count >= 2)
				{
					rootGrid.ColumnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
					rootGrid.ColumnDefinitions[0].MinWidth = 300.0;
					rootGrid.ColumnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
					rootGrid.ColumnDefinitions[1].MinWidth = 280.0;
				}

				if (rootGrid.Children.Count > 0 && rootGrid.Children[0] is Grid leftGrid)
				{
					leftGrid.RowSpacing = 8.0;
					if (leftGrid.RowDefinitions.Count > 0)
					{
						leftGrid.RowDefinitions[0].Height = GridLength.Auto;
					}
					if (leftGrid.Children.Count > 0 && leftGrid.Children[0] is StackPanel coverAndTitlePanel)
					{
						coverAndTitlePanel.Spacing = 6.0;
						if (coverAndTitlePanel.Children.Count > 0 && coverAndTitlePanel.Children[0] is Border coverBorder)
						{
							coverBorder.Width = 140.0;
							coverBorder.Height = 140.0;
							coverBorder.MaxWidth = 160.0;
							coverBorder.MaxHeight = 160.0;
						}
						if (coverAndTitlePanel.Children.Count > 1 && coverAndTitlePanel.Children[1] is StackPanel titlePanel)
						{
							if (titlePanel.Children.Count > 0 && titlePanel.Children[0] is TextBlock titleBlock)
							{
								titleBlock.FontSize = 18.0;
								titleBlock.MaxLines = 2;
								titleBlock.TextWrapping = TextWrapping.Wrap;
								titleBlock.TextTrimming = TextTrimming.CharacterEllipsis;
							}
						}
					}

					// Locate Position and Duration TextBlocks in Row 1 to format as m:ss cleanly
					foreach (var child in leftGrid.Children)
					{
						if (child is Grid rowGrid && Grid.GetRow(rowGrid) == 1)
						{
							foreach (var sub in rowGrid.Children)
							{
								if (sub is Grid timeGrid && Grid.GetRow(timeGrid) == 1)
								{
									if (timeGrid.Children.Count >= 2 &&
									    timeGrid.Children[0] is TextBlock posBlock &&
									    timeGrid.Children[1] is TextBlock durBlock)
									{
										_positionText = posBlock;
										_durationText = durBlock;
										_positionText.ClearValue(TextBlock.TextProperty);
										_durationText.ClearValue(TextBlock.TextProperty);
										UpdateTimeTexts();
									}
								}
							}
						}
					}

					rootGrid.Children.RemoveAt(0);
					ScrollViewer scrollViewer = new ScrollViewer
					{
						VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
						HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
						Content = leftGrid
					};
					Grid.SetColumn(scrollViewer, 0);
					rootGrid.Children.Insert(0, scrollViewer);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		StopMeterTimer();
		if (ViewModel != null)
		{
			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}
		Lyrics.Detach();
	}

	private void SetupPageContainers()
	{
		if (_isWrapped) return;
		_isWrapped = true;

		try
		{
			if (Content is Grid originalGrid)
			{
				Content = null;
				_classicContainer = originalGrid;

				_dawContainer = new Grid
				{
					Visibility = Visibility.Collapsed,
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 20, 22)),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};

				_modeSwitchBtn = new Button
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 8, 20, 0),
					Padding = new Thickness(12, 6, 12, 6),
					CornerRadius = new CornerRadius(16),
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(235, 30, 34, 40)),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)),
					BorderThickness = new Thickness(1)
				};
				_modeSwitchBtn.Click += OnModeSwitchClick;

				_rootGrid = new Grid();
				_rootGrid.Children.Add(_classicContainer);
				_rootGrid.Children.Add(_dawContainer);
				_rootGrid.Children.Add(_modeSwitchBtn);

				Content = _rootGrid;

				SetDawMode(s_useDawMode);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error wrapping containers: {ex.Message}");
		}
	}

	private void OnModeSwitchClick(object sender, RoutedEventArgs e)
	{
		SetDawMode(!_isDawMode);
	}

	private void SetDawMode(bool enableDaw)
	{
		_isDawMode = enableDaw;
		s_useDawMode = enableDaw;
		UpdateModeSwitchButtonUI();

		if (_isDawMode)
		{
			if (_classicContainer != null) _classicContainer.Visibility = Visibility.Collapsed;
			if (_dawContainer != null) _dawContainer.Visibility = Visibility.Visible;
			_ = EnsureDawWebViewAsync();
			StartMeterTimer();
		}
		else
		{
			StopMeterTimer();
			if (_dawContainer != null) _dawContainer.Visibility = Visibility.Collapsed;
			if (_classicContainer != null) _classicContainer.Visibility = Visibility.Visible;
		}
	}

	private void UpdateModeSwitchButtonUI()
	{
		if (_modeSwitchBtn == null) return;
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		if (_isDawMode)
		{
			_modeSwitchBtn.Margin = new Thickness(0, 4, 12, 0);
			sp.Children.Add(new FontIcon { Glyph = "\uE8D6", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)) });
			sp.Children.Add(new TextBlock { Text = "经典播放页", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
			ToolTipService.SetToolTip(_modeSwitchBtn, "返回经典播放器视图");
		}
		else
		{
			_modeSwitchBtn.Margin = new Thickness(0, 8, 20, 0);
			sp.Children.Add(new FontIcon { Glyph = "\uE995", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)) });
			sp.Children.Add(new TextBlock { Text = "Audition DAW 模式", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
			ToolTipService.SetToolTip(_modeSwitchBtn, "切换至 Adobe Audition DAW 专业工作台视图");
		}
		_modeSwitchBtn.Content = sp;
	}

	private async Task EnsureDawWebViewAsync()
	{
		if (_webView != null)
		{
			if (_dawReady)
			{
				SendFullDawState();
			}
			return;
		}

		try
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] EnsureDawWebViewAsync started\r\n");
			_webView = new WebView2
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			_dawContainer?.Children.Add(_webView);

			await _webView.EnsureCoreWebView2Async();
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] EnsureCoreWebView2Async done\r\n");

			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string assetsDir = Path.Combine(baseDir, "Assets", "AuditionDaw");
			if (!Directory.Exists(assetsDir))
			{
				assetsDir = @"C:\Users\Xeon\Desktop\FurinaPlayer\Assets\AuditionDaw";
			}
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] assetsDir=" + assetsDir + "\r\n");

			_webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
				"audition.daw",
				assetsDir,
				CoreWebView2HostResourceAccessKind.Allow);

			_webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
			_webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
			_webView.CoreWebView2.Settings.IsScriptEnabled = true;

			_webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
			_webView.NavigationCompleted += (s, e) =>
			{
				_dawReady = true;
				File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] NavigationCompleted: success=" + e.IsSuccess + ", error=" + e.WebErrorStatus + "\r\n");
				SendFullDawState();

				Task.Run(async () =>
				{
					await Task.Delay(2500);
					DispatcherQueue.TryEnqueue(async () =>
					{
						try
						{
							string capDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "screenshots");
							Directory.CreateDirectory(capDir);
							string capPath = Path.Combine(capDir, "daw_preview.png");
							using var fs = File.Create(capPath);
							await _webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(fs));
							File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] Captured preview successfully: " + capPath + "\r\n");
						}
						catch (Exception capEx)
						{
							File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] CapturePreview failed: " + capEx + "\r\n");
						}
					});
				});
			};

			_webView.Source = new Uri("https://audition.daw/index.html");
		}
		catch (Exception ex)
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " [DAW] Exception in EnsureDawWebViewAsync: " + ex + "\r\n");
		}
	}

	private void StartMeterTimer()
	{
		if (_meterTimer == null)
		{
			_meterTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			_meterTimer.Interval = TimeSpan.FromMilliseconds(40);
			_meterTimer.Tick += OnMeterTimerTick;
		}
		if (!_meterTimer.IsRunning)
		{
			_meterTimer.Start();
		}
	}

	private void StopMeterTimer()
	{
		if (_meterTimer != null && _meterTimer.IsRunning)
		{
			_meterTimer.Stop();
		}
	}

	private void OnMeterTimerTick(DispatcherQueueTimer sender, object args)
	{
		if (!_isDawMode || !_dawReady || _webView?.CoreWebView2 == null || ViewModel == null) return;

		var meters = ViewModel.GetLiveAudioMeters();
		bool isPlaying = ViewModel.IsPlaying;

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:clock",
			clock = new
			{
				state = isPlaying ? "playing" : "paused",
				positionSeconds = ViewModel.Position.TotalSeconds,
				durationSeconds = ViewModel.Duration.TotalSeconds,
				currentTrackId = ViewModel.CurrentTrack?.Id ?? 0L
			}
		});

		double energy = isPlaying ? Math.Clamp((meters.RmsDb + 60.0) / 60.0, 0.0, 1.0) : 0.0;
		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:audio",
			levels = new
			{
				peakDb = isPlaying ? meters.PeakDb : -90.0,
				rmsDb = isPlaying ? meters.RmsDb : -90.0
			},
			spectrum = new
			{
				bands = isPlaying ? meters.Bands : Array.Empty<float>(),
				energy
			}
		});
	}

	private void PostDawMessage(object message)
	{
		if (_webView?.CoreWebView2 == null) return;
		try
		{
			string json = JsonSerializer.Serialize(message);
			_webView.CoreWebView2.PostWebMessageAsJson(json);
		}
		catch
		{
		}
	}

	private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
	{
		try
		{
			string json = e.WebMessageAsJson;
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			string type = root.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? "" : "";

			if (type == "echo:workshop-ui:ready")
			{
				_dawReady = true;
				SendFullDawState();
				return;
			}

			if (type == "echo:audition:getQueue")
			{
				SendDawQueue();
				return;
			}

			if (type == "echo:workshop-ui:command")
			{
				string requestId = root.TryGetProperty("requestId", out var rProp) ? rProp.GetString() ?? "" : "";
				string cmd = root.TryGetProperty("command", out var cProp) ? cProp.GetString() ?? "" : "";
				JsonElement payload = root.TryGetProperty("payload", out var pProp) ? pProp : default;

				HandleDawCommand(cmd, requestId, payload);
				return;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error handling web message: {ex.Message}");
		}
	}

	private void HandleDawCommand(string cmd, string requestId, JsonElement payload)
	{
		bool ok = true;
		object? extraValue = null;

		switch (cmd)
		{
			case "playPause":
				ViewModel?.TogglePlayPauseCommand?.Execute(null);
				break;
			case "play":
				ViewModel?.PlayCommand?.Execute(null);
				break;
			case "pause":
				ViewModel?.PauseCommand?.Execute(null);
				break;
			case "previous":
				ViewModel?.PreviousCommand?.Execute(null);
				break;
			case "next":
				ViewModel?.NextCommand?.Execute(null);
				break;
			case "stop":
				ViewModel?.StopCommand?.Execute(null);
				break;
			case "seek":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("positionSeconds", out var posProp))
				{
					double sec = posProp.GetDouble();
					ViewModel?.SeekSeconds(sec);
				}
				break;
			case "setVolume":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("volume", out var volProp))
				{
					double vol = volProp.GetDouble();
					int intVol = vol <= 1.0 ? (int)Math.Round(vol * 100.0) : (int)Math.Round(vol);
					ViewModel?.SetVolumePercent(intVol);
				}
				break;
			case "setRepeat":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("mode", out var modeProp))
				{
					string mode = modeProp.GetString() ?? "off";
					if (ViewModel?.Engine != null)
					{
						ViewModel.Engine.Mode = mode switch
						{
							"one" => PlaybackMode.RepeatOne,
							"all" => PlaybackMode.RepeatAll,
							_ => PlaybackMode.Sequential
						};
					}
				}
				break;
			case "toggleShuffle":
				if (ViewModel?.Engine != null)
				{
					ViewModel.Engine.Mode = ViewModel.Engine.Mode == PlaybackMode.Shuffle
						? PlaybackMode.Sequential
						: PlaybackMode.Shuffle;
				}
				break;
			case "queue:playTrack":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("trackId", out var tidProp))
				{
					long tid = tidProp.GetInt64();
					var trk = ViewModel?.Engine.Queue.FirstOrDefault(t => t.Id == tid);
					if (trk != null) ViewModel?.Engine.PlayTrack(trk);
				}
				break;
			case "lyrics:get":
				SendDawLyrics();
				break;
			case "storage:get":
				extraValue = null;
				break;
			case "storage:set":
				break;
			default:
				break;
		}

		if (!string.IsNullOrEmpty(requestId))
		{
			var resultObj = new Dictionary<string, object?>
			{
				["protocolVersion"] = 1,
				["type"] = "echo:workshop-ui:result",
				["requestId"] = requestId,
				["ok"] = ok
			};
			if (extraValue != null)
			{
				resultObj["value"] = extraValue;
			}
			PostDawMessage(resultObj);
		}
	}

	private void SendFullDawState()
	{
		if (ViewModel == null || !_dawReady || _webView?.CoreWebView2 == null) return;
		var track = ViewModel.CurrentTrack;
		long trackId = track?.Id ?? 0L;
		bool isPlaying = ViewModel.IsPlaying;
		double volume01 = Math.Clamp(ViewModel.Volume / 100.0, 0.0, 1.0);

		string repeatMode = "off";
		bool isShuffle = false;
		if (ViewModel.Engine != null)
		{
			repeatMode = ViewModel.Engine.Mode switch
			{
				PlaybackMode.RepeatOne => "one",
				PlaybackMode.RepeatAll => "all",
				_ => "off"
			};
			isShuffle = ViewModel.Engine.Mode == PlaybackMode.Shuffle;
		}

		string coverUrl = GetCoverDataUrl(ViewModel.CoverPath);

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:init",
			appearance = new
			{
				accent = "#00e5ff",
				accentText = "#000000"
			}
		});

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:state",
			playback = new
			{
				state = isPlaying ? "playing" : "paused",
				currentTrackId = trackId,
				volume = volume01,
				repeatMode,
				shuffle = isShuffle
			},
			currentTrack = new
			{
				id = trackId,
				title = ViewModel.Title,
				artist = ViewModel.Artist,
				album = ViewModel.Album,
				durationSeconds = ViewModel.Duration.TotalSeconds,
				coverUrl
			}
		});

		SendDawAudioFormat();
		SendDawLyrics();
		SendDawQueue();
	}

	private void SendDawPlaybackState()
	{
		if (ViewModel == null || !_dawReady || _webView?.CoreWebView2 == null) return;
		var track = ViewModel.CurrentTrack;
		long trackId = track?.Id ?? 0L;
		bool isPlaying = ViewModel.IsPlaying;
		double volume01 = Math.Clamp(ViewModel.Volume / 100.0, 0.0, 1.0);

		string repeatMode = "off";
		bool isShuffle = false;
		if (ViewModel.Engine != null)
		{
			repeatMode = ViewModel.Engine.Mode switch
			{
				PlaybackMode.RepeatOne => "one",
				PlaybackMode.RepeatAll => "all",
				_ => "off"
			};
			isShuffle = ViewModel.Engine.Mode == PlaybackMode.Shuffle;
		}

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:state",
			playback = new
			{
				state = isPlaying ? "playing" : "paused",
				currentTrackId = trackId,
				volume = volume01,
				repeatMode,
				shuffle = isShuffle
			}
		});
	}

	private void SendDawAudioFormat()
	{
		if (ViewModel == null || !_dawReady || _webView?.CoreWebView2 == null) return;
		var fmt = ViewModel.GetAudioFormatDetails();
		var meters = ViewModel.GetLiveAudioMeters();

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:audio",
			audio = new
			{
				codec = fmt.Codec,
				sampleRate = fmt.SampleRate,
				bitDepth = fmt.BitDepth,
				channels = fmt.Channels,
				outputDevice = fmt.OutputDevice,
				outputBackend = fmt.OutputEngine,
				outputMode = fmt.OutputMode
			},
			levels = new
			{
				peakDb = meters.PeakDb,
				rmsDb = meters.RmsDb
			},
			spectrum = new
			{
				bands = meters.Bands,
				energy = Math.Clamp((meters.RmsDb + 60.0) / 60.0, 0.0, 1.0)
			}
		});
	}

	private void SendDawLyrics()
	{
		if (ViewModel == null || !_dawReady || _webView?.CoreWebView2 == null) return;
		var track = ViewModel.CurrentTrack;
		if (track == null) return;

		var lyricLines = ViewModel.LyricLines;
		var linesList = new List<object>();

		if (lyricLines != null)
		{
			foreach (var l in lyricLines)
			{
				linesList.Add(new
				{
					timeMs = (long)l.Time.TotalMilliseconds,
					text = l.Text
				});
			}
		}

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:lyrics",
			trackId = track.Id,
			lyrics = new
			{
				kind = linesList.Count > 0 ? "synced" : "empty",
				title = track.DisplayTitle,
				artist = track.Artist ?? "",
				album = track.Album ?? "",
				durationSeconds = track.DurationMilliseconds > 0 ? track.DurationMilliseconds / 1000.0 : ViewModel.Duration.TotalSeconds,
				lines = linesList
			}
		});
	}

	private void SendDawQueue()
	{
		if (ViewModel?.Engine == null || !_dawReady || _webView?.CoreWebView2 == null) return;
		var queue = ViewModel.Engine.Queue;
		if (queue == null) return;

		var items = queue.Select((t, i) => new
		{
			queueId = $"q_{t.Id}_{i}",
			id = t.Id,
			title = t.DisplayTitle,
			artist = t.Artist ?? "",
			album = t.Album ?? "",
			durationSeconds = t.DurationMilliseconds > 0 ? t.DurationMilliseconds / 1000.0 : 0.0
		}).ToList();

		PostDawMessage(new
		{
			type = "echo:audition:queueData",
			items
		});
	}

	private static string GetCoverDataUrl(string? coverPath)
	{
		if (string.IsNullOrEmpty(coverPath) || !File.Exists(coverPath)) return string.Empty;
		try
		{
			byte[] bytes = File.ReadAllBytes(coverPath);
			string ext = Path.GetExtension(coverPath).ToLowerInvariant();
			string mime = ext switch
			{
				".png" => "image/png",
				".webp" => "image/webp",
				".bmp" => "image/bmp",
				_ => "image/jpeg"
			};
			return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
		}
		catch
		{
			return string.Empty;
		}
	}

	private void OnPlayClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PlayCommand?.Execute(null);
	}

	private void OnPauseClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PauseCommand?.Execute(null);
	}

	private void OnNextClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.NextCommand?.Execute(null);
	}

	private void OnPreviousClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PreviousCommand?.Execute(null);
	}

	private void OnStopClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.StopCommand?.Execute(null);
	}

	private void OnCycleModeClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.CycleModeCommand?.Execute(null);
	}

	private void OnProgressPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.BeginSeek();
		}
	}

	private void OnProgressPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (ViewModel != null && !(ViewModel.Duration.TotalSeconds <= 0.0) && sender is Slider { ActualWidth: not (<=0.0) } slider)
		{
			TimeSpan t = TimeSpan.FromSeconds(Math.Clamp(e.GetCurrentPoint(slider).Position.X / slider.ActualWidth, 0.0, 1.0) * ViewModel.Duration.TotalSeconds);
			ToolTipService.SetToolTip(slider, FormatTime(t) + " / " + FormatTime(ViewModel.Duration));
		}
	}

	private void OnProgressPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Slider element)
		{
			ToolTipService.SetToolTip(element, null);
		}
	}

	internal static string FormatTime(TimeSpan t)
	{
		if (!(t.TotalHours >= 1.0))
		{
			return t.ToString("m\\:ss");
		}
		return t.ToString("h\\:mm\\:ss");
	}

	private void OnProgressPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.SeekToPercent(ProgressSlider.Value);
		}
	}

	private void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && !(SpeedCombo == null) && SpeedCombo.SelectedIndex >= 0 && SpeedCombo.SelectedIndex < SpeedOptions.Length)
		{
			ViewModel.Speed = SpeedOptions[SpeedCombo.SelectedIndex];
		}
	}

	private void OnEqGainsChanged(double[] gains)
	{
		ViewModel?.ApplyEqualizer();
	}

	private void OnEqPresetChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && EqPresetCombo.SelectedItem is ComboBoxItem { Content: string content })
		{
			ViewModel.ApplyEqPresetCommand?.Execute(content);
			Equalizer.Gains = ViewModel.EqGains;
		}
	}

	private void OnEqResetClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			double[] gains = new double[10];
			Equalizer.Gains = gains;
			ViewModel.ApplyEqualizer();
		}
	}

	private async void OnBatchImportLyricsClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker
			{
				FileTypeFilter = { ".lrc", ".txt" }
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);
			IReadOnlyList<StorageFile> readOnlyList = await fileOpenPicker.PickMultipleFilesAsync();
			if (readOnlyList == null || readOnlyList.Count == 0)
			{
				return;
			}
			string[] array = new string[readOnlyList.Count];
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				array[i] = readOnlyList[i].Path;
			}
			string content = await ViewModel.ImportLyricsBatchAsync(array);
			await new ContentDialog
			{
				Title = "歌词批量导入",
				Content = content,
				CloseButtonText = "好",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "歌词批量导入",
				Content = "导入失败：" + ex.Message,
				CloseButtonText = "好",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/NowPlayingPage.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			Lyrics = target.As<LyricsPanel>();
			break;
		case 3:
			target.As<Button>().Click += OnBatchImportLyricsClick;
			break;
		case 4:
			Equalizer = target.As<EqualizerControl>();
			Equalizer.GainsChanged += OnEqGainsChanged;
			break;
		case 5:
			EqPresetCombo = target.As<ComboBox>();
			EqPresetCombo.SelectionChanged += OnEqPresetChanged;
			break;
		case 6:
			target.As<Button>().Click += OnEqResetClick;
			break;
		case 7:
			SpeedCombo = target.As<ComboBox>();
			SpeedCombo.SelectionChanged += OnSpeedChanged;
			break;
		case 8:
			target.As<Button>().Click += OnCycleModeClick;
			break;
		case 9:
			target.As<Button>().Click += OnPreviousClick;
			break;
		case 10:
			target.As<Button>().Click += OnPlayClick;
			break;
		case 11:
			target.As<Button>().Click += OnPauseClick;
			break;
		case 12:
			target.As<Button>().Click += OnNextClick;
			break;
		case 13:
			target.As<Button>().Click += OnStopClick;
			break;
		case 14:
			ProgressSlider = target.As<Slider>();
			ProgressSlider.PointerPressed += OnProgressPointerPressed;
			ProgressSlider.PointerReleased += OnProgressPointerReleased;
			ProgressSlider.PointerCaptureLost += OnProgressPointerReleased;
			ProgressSlider.PointerMoved += OnProgressPointerMoved;
			ProgressSlider.PointerExited += OnProgressPointerExited;
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
