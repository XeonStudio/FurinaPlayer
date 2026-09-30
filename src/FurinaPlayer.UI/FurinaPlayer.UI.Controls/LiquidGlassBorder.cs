using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;
using Windows.Foundation;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(LiquidGlassBorderWinRTTypeDetails))]
public sealed class LiquidGlassBorder : UserControl, IComponentConnector
{
	public new static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(LiquidGlassBorder), new PropertyMetadata(null, OnContentChanged));

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border Glass;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ContentPresenter ContentHost;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public new object? Content
	{
		get
		{
			return GetValue(ContentProperty);
		}
		set
		{
			SetValue(ContentProperty, value);
		}
	}

	public LiquidGlassBorder()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is LiquidGlassBorder liquidGlassBorder)
		{
			liquidGlassBorder.ContentHost.Content = e.NewValue;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ContentHost.Content = Content;
		Border child = new Border
		{
			Height = 1.0,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8.0, 1.0, 8.0, 0.0),
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0f, 0f),
				EndPoint = new Point(1f, 0f),
				GradientStops = 
				{
					new GradientStop
					{
						Color = Colors.Transparent,
						Offset = 0.0
					},
					new GradientStop
					{
						Color = Colors.White,
						Offset = 0.5
					},
					new GradientStop
					{
						Color = Colors.Transparent,
						Offset = 1.0
					}
				}
			},
			Opacity = 0.25
		};
		Glass.Child = child;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/LiquidGlassBorder.xaml");
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
			Glass = target.As<Border>();
			break;
		case 3:
			ContentHost = target.As<ContentPresenter>();
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
