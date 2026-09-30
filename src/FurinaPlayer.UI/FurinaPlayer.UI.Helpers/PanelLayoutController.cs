using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace FurinaPlayer.UI.Helpers;

public sealed class PanelLayoutController
{
	private readonly Canvas _canvas;

	private readonly double _minWidth;

	private readonly double _minHeight;

	private readonly double _maxWidth;

	private readonly double _maxHeight;

	private readonly Dictionary<UIElement, Border> _panelByHeader = new Dictionary<UIElement, Border>();

	private readonly Dictionary<UIElement, Border> _panelByHandle = new Dictionary<UIElement, Border>();

	private (UIElement Header, double OffsetX, double OffsetY)? _dragState;

	private (UIElement Handle, double LastX, double LastY)? _resizeState;

	private bool _dragged;

	private bool _resized;

	public Action? LayoutChanged;

	public PanelLayoutController(Canvas canvas, double minWidth = 220.0, double minHeight = 120.0, double maxWidth = 1600.0, double maxHeight = 900.0)
	{
		_canvas = canvas;
		_minWidth = minWidth;
		_minHeight = minHeight;
		_maxWidth = maxWidth;
		_maxHeight = maxHeight;
	}

	public void Register(Border panel, UIElement header)
	{
		_panelByHeader[header] = panel;
		header.PointerPressed += OnHeaderPointerPressed;
		header.PointerMoved += OnHeaderPointerMoved;
		header.PointerReleased += OnHeaderPointerReleased;
		header.PointerCanceled += OnHeaderPointerReleased;
		header.PointerCaptureLost += OnHeaderPointerReleased;
	}

	public void RegisterResizeHandle(UIElement handle, Border panel)
	{
		_panelByHandle[handle] = panel;
		handle.PointerPressed += OnResizePointerPressed;
		handle.PointerMoved += OnResizePointerMoved;
		handle.PointerReleased += OnResizePointerReleased;
		handle.PointerCanceled += OnResizePointerReleased;
		handle.PointerCaptureLost += OnResizePointerReleased;
	}

	public async Task<bool> ShowResizeDialogAsync(XamlRoot root, Border panel, string title)
	{
		Slider widthSlider = new Slider
		{
			Minimum = _minWidth,
			Maximum = _maxWidth,
			StepFrequency = 10.0,
			Value = panel.Width,
			Header = "宽度"
		};
		Slider heightSlider = new Slider
		{
			Minimum = _minHeight,
			Maximum = _maxHeight,
			StepFrequency = 10.0,
			Value = panel.Height,
			Header = "高度"
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 8.0
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = "设置面板的宽度与高度（像素），拖动右下角或在此输入数值调整大小",
			TextWrapping = TextWrapping.Wrap,
			Opacity = 0.75,
			FontSize = 12.0
		});
		stackPanel.Children.Add(widthSlider);
		stackPanel.Children.Add(heightSlider);
		if (await new ContentDialog
		{
			Title = title,
			Content = stackPanel,
			PrimaryButtonText = "确定",
			CloseButtonText = "取消",
			XamlRoot = root
		}.ShowAsync() == ContentDialogResult.Primary)
		{
			panel.Width = widthSlider.Value;
			panel.Height = heightSlider.Value;
			return true;
		}
		return false;
	}

	private static bool IsInsideButton(object originalSource)
	{
		DependencyObject dependencyObject = originalSource as DependencyObject;
		while (dependencyObject != null)
		{
			if (dependencyObject is ButtonBase)
			{
				return true;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return false;
	}

	private void OnHeaderPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!IsInsideButton(e.OriginalSource))
		{
			Border relativeTo = _panelByHeader[(UIElement)sender];
			Point position = e.GetCurrentPoint(relativeTo).Position;
			_dragState = ((UIElement)sender, position.X, position.Y);
			_dragged = false;
			((UIElement)sender).CapturePointer(e.Pointer);
			e.Handled = true;
		}
	}

	private void OnHeaderPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_dragState.HasValue && _dragState.Value.Header == sender)
		{
			Border border = _panelByHeader[(UIElement)sender];
			Point position = e.GetCurrentPoint(_canvas).Position;
			double max = Math.Max(0.0, _canvas.Width - border.Width);
			double max2 = Math.Max(0.0, _canvas.Height - border.Height);
			Canvas.SetLeft(border, Math.Clamp(position.X - _dragState.Value.OffsetX, 0.0, max));
			Canvas.SetTop(border, Math.Clamp(position.Y - _dragState.Value.OffsetY, 0.0, max2));
			_dragged = true;
			e.Handled = true;
		}
	}

	private void OnHeaderPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_dragState = null;
		if (_dragged)
		{
			_dragged = false;
			LayoutChanged?.Invoke();
		}
	}

	private void OnResizePointerPressed(object sender, PointerRoutedEventArgs e)
	{
		Point position = e.GetCurrentPoint(_canvas).Position;
		_resizeState = ((UIElement)sender, position.X, position.Y);
		_resized = false;
		((UIElement)sender).CapturePointer(e.Pointer);
		e.Handled = true;
	}

	private void OnResizePointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_resizeState.HasValue && _resizeState.Value.Handle == sender)
		{
			Border border = _panelByHandle[(UIElement)sender];
			Point position = e.GetCurrentPoint(_canvas).Position;
			border.Width = Math.Clamp(border.Width + (position.X - _resizeState.Value.LastX), _minWidth, _maxWidth);
			border.Height = Math.Clamp(border.Height + (position.Y - _resizeState.Value.LastY), _minHeight, _maxHeight);
			_resizeState = ((UIElement)sender, position.X, position.Y);
			_resized = true;
			e.Handled = true;
		}
	}

	private void OnResizePointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_resizeState = null;
		if (_resized)
		{
			_resized = false;
			LayoutChanged?.Invoke();
		}
	}
}
