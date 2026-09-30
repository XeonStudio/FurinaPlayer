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

	private NowPlayingViewModel? ViewModel => DataContext as NowPlayingViewModel;

	public MiniPlayer()
	{
		InitializeComponent();
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
		ViewModel?.BeginSeek();
	}

	private void OnSeekPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		NowPlayingViewModel viewModel = ViewModel;
		if (viewModel != null && sender is Slider slider)
		{
			viewModel.SeekToPercent(slider.Value);
		}
	}

	private void OnSeekPointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
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
			slider.PointerPressed += OnSeekPointerPressed;
			slider.PointerReleased += OnSeekPointerReleased;
			slider.PointerCaptureLost += OnSeekPointerCaptureLost;
			slider.PointerMoved += OnSeekPointerMoved;
			slider.PointerExited += OnSeekPointerExited;
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
