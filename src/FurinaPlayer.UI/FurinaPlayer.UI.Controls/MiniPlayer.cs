using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using SonicWave.Core.Models;
using WinRT;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(MiniPlayerWinRTTypeDetails))]
public sealed class MiniPlayer : UserControl, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button RecentButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView RecentList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	private Slider? _seekSlider;
	private TextBlock? _positionText;
	private TextBlock? _durationText;
	private UIElement? _trackInfoPanel;
	private UIElement? _coverBorder;
	private bool _isDraggingSlider;
	private NowPlayingViewModel? _currentVm;

	public static event Action? OpenNowPlayingRequested;
	public event Action? NavigateToPlayingRequested;

	private NowPlayingViewModel? ViewModel => DataContext as NowPlayingViewModel;

	public MiniPlayer()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		DataContextChanged += OnDataContextChanged;
	}

	public void RequestOpenNowPlaying()
	{
		OpenNowPlayingRequested?.Invoke();
		NavigateToPlayingRequested?.Invoke();

		try
		{
			var win = WindowManager.CurrentWindow;
			if (win != null)
			{
				var navMethod = win.GetType().GetMethod("NavigateTo", new[] { typeof(string) });
				navMethod?.Invoke(win, new object[] { "playing" });
			}
		}
		catch { }
	}

	private void AttachOpenNowPlayingGesture(UIElement? element)
	{
		if (element == null) return;
		try
		{
			var prop = typeof(UIElement).GetProperty("ProtectedCursor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			prop?.SetValue(element, Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand));
		}
		catch { }

		element.PointerPressed -= OnTrackInfoPointerPressed;
		element.PointerPressed += OnTrackInfoPointerPressed;
		element.Tapped -= OnTrackInfoTapped;
		element.Tapped += OnTrackInfoTapped;
	}

	private void OnTrackInfoPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		RequestOpenNowPlaying();
	}

	private void OnTrackInfoTapped(object sender, TappedRoutedEventArgs e)
	{
		e.Handled = true;
		RequestOpenNowPlaying();
	}

	private static UIElement? FindElementByName(DependencyObject? parent, string name)
	{
		if (parent == null) return null;
		int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
			if (child is FrameworkElement fe && fe.Name == name)
			{
				return fe;
			}
			var result = FindElementByName(child, name);
			if (result != null) return result;
		}
		return null;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		HookViewModel(ViewModel);
		FluentIconFactory.UpgradeButtonsInVisualTree(this);
		UpdateModeButtonFluentIcon();
		UpdateTimes();
		AttachOpenNowPlayingGesture(_trackInfoPanel ?? FindName("TrackInfoPanel") as UIElement ?? FindElementByName(this, "TrackInfoPanel"));
		AttachOpenNowPlayingGesture(_coverBorder ?? FindName("CoverBorder") as UIElement ?? FindElementByName(this, "CoverBorder"));
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		UnhookViewModel(_currentVm);
	}

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		HookViewModel(args.NewValue as NowPlayingViewModel);
	}

	private void HookViewModel(NowPlayingViewModel? vm)
	{
		if (_currentVm != null)
		{
			_currentVm.PropertyChanged -= OnVmPropertyChanged;
		}
		_currentVm = vm;
		if (_currentVm != null)
		{
			_currentVm.PropertyChanged += OnVmPropertyChanged;
			UpdateSlider();
			UpdateTimes();
		}
	}

	private void UnhookViewModel(NowPlayingViewModel? vm)
	{
		if (vm != null)
		{
			vm.PropertyChanged -= OnVmPropertyChanged;
		}
		_currentVm = null;
	}

	private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(NowPlayingViewModel.Position) ||
		    e.PropertyName == nameof(NowPlayingViewModel.PositionPercent) ||
		    e.PropertyName == nameof(NowPlayingViewModel.Duration))
		{
			UpdateSlider();
			if (!_isDraggingSlider)
			{
				UpdateTimes();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.ModeText))
		{
			UpdateModeButtonFluentIcon();
		}
	}

	private void UpdateModeButtonFluentIcon()
	{
		try
		{
			FindAndUpdateModeButton(this, _currentVm?.ModeText ?? "顺序播放");
		}
		catch
		{
		}
	}

	private static void FindAndUpdateModeButton(DependencyObject? root, string modeText)
	{
		if (root == null) return;
		int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			if (child is Button btn && ToolTipService.GetToolTip(btn) is string tt && tt.Contains("播放模式"))
			{
				FluentIconFactory.SetButtonFluentIcon(btn, FluentIconFactory.ResolvePlayModeKind(modeText), modeText, 18);
				return;
			}
			FindAndUpdateModeButton(child, modeText);
		}
	}

	private void UpdateSlider()
	{
		if (!_isDraggingSlider && _seekSlider != null && _currentVm != null)
		{
			_seekSlider.Value = _currentVm.PositionPercent;
		}
	}

	private void UpdateTimes()
	{
		if (_currentVm != null)
		{
			if (_positionText != null)
			{
				_positionText.Text = NowPlayingPage.FormatTime(_currentVm.Position);
			}
			if (_durationText != null)
			{
				_durationText.Text = NowPlayingPage.FormatTime(_currentVm.Duration);
			}
		}
	}

	private async void OnRecentFlyoutOpening(object sender, object e)
	{
		if (ViewModel != null)
		{
			await ViewModel.LoadRecentAsync();
		}
	}

	private void OnSeekPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = true;
		ViewModel?.BeginSeek();
	}

	private void OnSeekPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = false;
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && sender is Slider slider)
		{
			viewModel.SeekToPercent(slider.Value);
			UpdateTimes();
		}
	}

	private void OnSeekPointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = false;
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && sender is Slider slider)
		{
			viewModel.SeekToPercent(slider.Value);
			UpdateTimes();
		}
	}

	private void OnSeekPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && !(viewModel.Duration.TotalSeconds <= 0.0) && sender is Slider { ActualWidth: not (<=0.0) } slider)
		{
			TimeSpan t = TimeSpan.FromSeconds(Math.Clamp(e.GetCurrentPoint(slider).Position.X / slider.ActualWidth, 0.0, 1.0) * viewModel.Duration.TotalSeconds);
			ToolTipService.SetToolTip(slider, NowPlayingPage.FormatTime(t) + " / " + NowPlayingPage.FormatTime(viewModel.Duration));
			if (_isDraggingSlider && _positionText != null)
			{
				_positionText.Text = NowPlayingPage.FormatTime(t);
			}
		}
	}

	private void OnSeekPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Slider element)
		{
			ToolTipService.SetToolTip(element, null);
		}
	}

	private void OnRecentItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Track parameter)
		{
			ViewModel?.PlayRecentCommand?.Execute(parameter);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/MiniPlayer.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 4:
			RecentButton = target.As<Button>();
			break;
		case 5:
			target.As<Flyout>().Opening += OnRecentFlyoutOpening;
			break;
		case 6:
			RecentList = target.As<ListView>();
			RecentList.ItemClick += OnRecentItemClick;
			break;
		case 7:
		{
			Slider slider = target.As<Slider>();
			_seekSlider = slider;
			slider.PointerPressed += OnSeekPointerPressed;
			slider.PointerReleased += OnSeekPointerReleased;
			slider.PointerCaptureLost += OnSeekPointerCaptureLost;
			slider.PointerMoved += OnSeekPointerMoved;
			slider.PointerExited += OnSeekPointerExited;
			UpdateSlider();
			break;
		}
		case 8:
			_positionText = target.As<TextBlock>();
			UpdateTimes();
			break;
		case 9:
			_durationText = target.As<TextBlock>();
			UpdateTimes();
			break;
		case 10:
			_trackInfoPanel = target as UIElement ?? (target is IWinRTObject ? target.As<UIElement>() : null);
			AttachOpenNowPlayingGesture(_trackInfoPanel);
			break;
		case 11:
			_coverBorder = target as UIElement ?? (target is IWinRTObject ? target.As<UIElement>() : null);
			AttachOpenNowPlayingGesture(_coverBorder);
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
