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
	private bool _isDraggingSlider;
	private NowPlayingViewModel? _currentVm;

	private NowPlayingViewModel? ViewModel => DataContext as NowPlayingViewModel;

	public MiniPlayer()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		DataContextChanged += OnDataContextChanged;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		HookViewModel(ViewModel);
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
		}
	}

	private void UpdateSlider()
	{
		if (!_isDraggingSlider && _seekSlider != null && _currentVm != null)
		{
			_seekSlider.Value = _currentVm.PositionPercent;
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
		}
	}

	private void OnSeekPointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = false;
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && sender is Slider slider)
		{
			viewModel.SeekToPercent(slider.Value);
		}
	}

	private void OnSeekPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && !(viewModel.Duration.TotalSeconds <= 0.0) && sender is Slider { ActualWidth: not (<=0.0) } slider)
		{
			TimeSpan t = TimeSpan.FromSeconds(Math.Clamp(e.GetCurrentPoint(slider).Position.X / slider.ActualWidth, 0.0, 1.0) * viewModel.Duration.TotalSeconds);
			ToolTipService.SetToolTip(slider, NowPlayingPage.FormatTime(t) + " / " + NowPlayingPage.FormatTime(viewModel.Duration));
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
