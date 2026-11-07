using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// Apple Liquid Glass 容器控件 (https://developer.apple.com/documentation/technologyoverviews/liquid-glass)
/// 抛弃旧版简陋的单线描边，采用：
/// 1. SkiaSharp 硬件加速曲面边缘透镜折射层 (Edge Lensing &amp; Caustic Dispersion)
/// 2. 动态左上 135° 镜面高光环与右下 315° 次表面焦散内反射 (Specular &amp; Caustic Rim)
/// 3. 实时鼠标指针跟随镜面聚光斑与按压流体反馈 (Interactive Pointer Lensing)
/// </summary>
[ContentProperty(Name = "Content")]
public sealed class LiquidGlassBorder : UserControl
{
	private readonly Grid _rootGrid;
	private readonly Border _outerGlassBorder;
	private readonly SKXamlCanvas _opticalCanvas;
	private readonly ContentPresenter _presenter;

	private bool _isHovered;
	private bool _isPressed;
	private Point _pointerNorm = new Point(0.22, 0.18);

	public static readonly DependencyProperty GlassOpacityProperty = DependencyProperty.Register(
		nameof(GlassOpacity),
		typeof(double),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(0.82, OnVisualPropertyChanged));

	public static readonly DependencyProperty TintColorProperty = DependencyProperty.Register(
		nameof(TintColor),
		typeof(Color),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(Color.FromArgb(255, 245, 249, 255), OnVisualPropertyChanged));

	public static readonly DependencyProperty HighlightOpacityProperty = DependencyProperty.Register(
		nameof(HighlightOpacity),
		typeof(double),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(0.65, OnVisualPropertyChanged));

	public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
		nameof(Variant),
		typeof(LiquidGlassVariant),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(LiquidGlassVariant.Regular, OnVisualPropertyChanged));

	public new static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
		nameof(CornerRadius),
		typeof(CornerRadius),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(new CornerRadius(16.0), OnVisualPropertyChanged));

	public new static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
		nameof(Padding),
		typeof(Thickness),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(new Thickness(16.0), OnVisualPropertyChanged));

	public new static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
		nameof(Content),
		typeof(object),
		typeof(LiquidGlassBorder),
		new PropertyMetadata(null, OnVisualPropertyChanged));

	public double GlassOpacity
	{
		get => (double)GetValue(GlassOpacityProperty);
		set => SetValue(GlassOpacityProperty, value);
	}

	public Color TintColor
	{
		get => (Color)GetValue(TintColorProperty);
		set => SetValue(TintColorProperty, value);
	}

	public double HighlightOpacity
	{
		get => (double)GetValue(HighlightOpacityProperty);
		set => SetValue(HighlightOpacityProperty, value);
	}

	public LiquidGlassVariant Variant
	{
		get => (LiquidGlassVariant)GetValue(VariantProperty);
		set => SetValue(VariantProperty, value);
	}

	public new CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	public new Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	public new object? Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public LiquidGlassBorder()
	{
		_rootGrid = new Grid();

		_opticalCanvas = new SKXamlCanvas
		{
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_opticalCanvas.PaintSurface += OnOpticalCanvasPaintSurface;

		_presenter = new ContentPresenter
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};

		_rootGrid.Children.Add(_opticalCanvas);
		_rootGrid.Children.Add(_presenter);

		_outerGlassBorder = new Border
		{
			BorderThickness = new Thickness(1.2),
			Translation = new Vector3(0f, 0f, 16f),
			Shadow = new ThemeShadow(),
			Child = _rootGrid
		};

		base.Content = _outerGlassBorder;

		PointerEntered += OnPointerEntered;
		PointerMoved += OnPointerMoved;
		PointerExited += OnPointerExited;
		PointerPressed += OnPointerPressed;
		PointerReleased += OnPointerReleased;
		ActualThemeChanged += (s, e) => UpdateVisuals();
		SizeChanged += (s, e) => _opticalCanvas.Invalidate();

		UpdateVisuals();
	}

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		_isHovered = true;
		UpdatePointerNorm(e);
		UpdateVisuals();
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isHovered) _isHovered = true;
		UpdatePointerNorm(e);
		UpdateVisuals();
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		_isHovered = false;
		_isPressed = false;
		_pointerNorm = new Point(0.22, 0.18);
		UpdateVisuals();
	}

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_isPressed = true;
		UpdatePointerNorm(e);
		UpdateVisuals();
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_isPressed = false;
		UpdateVisuals();
	}

	private void UpdatePointerNorm(PointerRoutedEventArgs e)
	{
		if (ActualWidth > 0 && ActualHeight > 0)
		{
			var pt = e.GetCurrentPoint(this).Position;
			_pointerNorm = new Point(
				Math.Clamp(pt.X / ActualWidth, 0.0, 1.0),
				Math.Clamp(pt.Y / ActualHeight, 0.0, 1.0));
		}
	}

	private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is LiquidGlassBorder control)
		{
			control.UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
		double opacityScale = Math.Clamp(GlassOpacity / 0.80, 0.55, 1.15);

		_outerGlassBorder.CornerRadius = CornerRadius;
		_outerGlassBorder.Background = LiquidGlassEngine.CreateSurfaceBrush(Variant, isDark, opacityScale, _pointerNorm);
		_outerGlassBorder.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(Variant, isDark, _isHovered, Variant == LiquidGlassVariant.AccentLensed, _pointerNorm);

		_presenter.Margin = Padding;
		_presenter.Content = Content;

		_opticalCanvas.Invalidate();
	}

	private void OnOpticalCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		int w = e.Info.Width;
		int h = e.Info.Height;
		if (w <= 4 || h <= 4) return;

		bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
		float cr = (float)Math.Max(4.0, CornerRadius.TopLeft);
		var rect = new SKRect(1f, 1f, w - 1f, h - 1f);
		using var rrect = new SKRoundRect(rect, cr, cr);

		canvas.Save();
		canvas.ClipRoundRect(rrect, SKClipOperation.Intersect, antialias: true);

		// 1. 顶部曲面边缘折射镜面弧光 (Top Curved Lensing Specular Arc)
		float topGlowHeight = Math.Min(h * 0.42f, 64f);
		byte topAlpha = (byte)Math.Clamp((int)(HighlightOpacity * (isDark ? 90 : 185) * (_isHovered ? 1.25 : 1.0)), 0, 255);
		using (var topShader = SKShader.CreateLinearGradient(
			new SKPoint(0, 0),
			new SKPoint(0, topGlowHeight),
			new SKColor[]
			{
				new SKColor(255, 255, 255, topAlpha),
				new SKColor(224, 242, 254, (byte)(topAlpha / 3)),
				SKColors.Transparent
			},
			new float[] { 0f, 0.45f, 1f },
			SKShaderTileMode.Clamp))
		using (var topPaint = new SKPaint { Shader = topShader, IsAntialias = true })
		{
			canvas.DrawRect(0, 0, w, topGlowHeight, topPaint);
		}

		// 2. 底部边缘次表面焦散折射微光 (Bottom Caustic Subsurface Rim)
		float bottomHeight = Math.Min(h * 0.30f, 44f);
		byte causticAlpha = (byte)(isDark ? (_isHovered ? 55 : 32) : (_isHovered ? 85 : 52));
		using (var bottomShader = SKShader.CreateLinearGradient(
			new SKPoint(0, h),
			new SKPoint(0, h - bottomHeight),
			new SKColor[]
			{
				new SKColor(129, 140, 248, causticAlpha),
				new SKColor(56, 189, 248, (byte)(causticAlpha / 2)),
				SKColors.Transparent
			},
			new float[] { 0f, 0.5f, 1f },
			SKShaderTileMode.Clamp))
		using (var bottomPaint = new SKPaint { Shader = bottomShader, IsAntialias = true })
		{
			canvas.DrawRect(0, h - bottomHeight, w, bottomHeight, bottomPaint);
		}

		// 3. 实时指针跟随透镜折射光斑 (Interactive Pointer Specular Glint)
		if (_isHovered || _isPressed)
		{
			float px = (float)_pointerNorm.X * w;
			float py = (float)_pointerNorm.Y * h;
			float glintRadius = Math.Max(w, h) * (_isPressed ? 0.42f : 0.55f);
			byte glintAlpha = (byte)(isDark ? (_isPressed ? 65 : 42) : (_isPressed ? 135 : 95));

			using var glintShader = SKShader.CreateRadialGradient(
				new SKPoint(px, py),
				glintRadius,
				new SKColor[]
				{
					new SKColor(255, 255, 255, glintAlpha),
					new SKColor(186, 230, 253, (byte)(glintAlpha * 0.45f)),
					SKColors.Transparent
				},
				new float[] { 0f, 0.45f, 1f },
				SKShaderTileMode.Clamp);
			using var glintPaint = new SKPaint { Shader = glintShader, IsAntialias = true };
			canvas.DrawCircle(px, py, glintRadius, glintPaint);
		}

		// 4. 内侧非对称高光折射内环 (Inner Specular Lensing Ring)
		var innerRect = new SKRect(1.5f, 1.5f, w - 1.5f, h - 1.5f);
		using var innerRRect = new SKRoundRect(innerRect, Math.Max(2f, cr - 1f), Math.Max(2f, cr - 1f));
		using var rimShader = SKShader.CreateLinearGradient(
			new SKPoint(w * (float)_pointerNorm.X * 0.4f, 0),
			new SKPoint(w, h),
			new SKColor[]
			{
				new SKColor(255, 255, 255, isDark ? (byte)130 : (byte)230),
				new SKColor(255, 255, 255, isDark ? (byte)25 : (byte)75),
				new SKColor(129, 140, 248, isDark ? (byte)95 : (byte)155)
			},
			new float[] { 0f, 0.52f, 1f },
			SKShaderTileMode.Clamp);
		using var rimPaint = new SKPaint
		{
			Shader = rimShader,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1.2f,
			IsAntialias = true
		};
		canvas.DrawRoundRect(innerRRect, rimPaint);

		canvas.Restore();
	}
}
