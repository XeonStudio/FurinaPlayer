using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
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

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.Initialize();
			Lyrics.Attach(ViewModel);
			Lyrics.ApplyLyricFont(ViewModel.LyricFontFamily, ViewModel.LyricFontFilePath);
			Equalizer.Gains = ViewModel.EqGains;
			int num = Array.IndexOf(SpeedOptions, ViewModel.Speed);
			if (num >= 0)
			{
				SpeedCombo.SelectedIndex = num;
			}
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		Lyrics.Detach();
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
