using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// Apple Liquid Glass 光学材质变体规范
/// 参考 Apple Developer Documentation: Liquid Glass (https://developer.apple.com/documentation/technologyoverviews/liquid-glass)
/// </summary>
public enum LiquidGlassVariant
{
	/// <summary>
	/// 标准液态玻璃 (.regular)：用于主工作台卡片、导航栏、浮动面板。
	/// 具备多层曲面折射梯度、左上 135° 镜面高光环、右下 315° 焦散色散边缘与自适应色彩折射。
	/// </summary>
	Regular,

	/// <summary>
	/// 清透液态玻璃 (.clear)：用于胶囊按钮、顶部状态徽标、列表表头、微交互组件。
	/// 极高通透度 + 强边缘透镜折射高光 + 悬停动态聚光响应。
	/// </summary>
	Clear,

	/// <summary>
	/// 悬浮导航液态玻璃 (.floatingBar)：用于顶栏、侧边导航栏、底部悬浮 MiniPlayer、可拖拽浮动工具窗。
	/// 具备更高的曲面光学厚度感、双重内外折射描边与深邃悬浮环境光晕。
	/// </summary>
	FloatingBar,

	/// <summary>
	/// 焦点高亮液态玻璃 (.accentLensed)：用于激活态胶囊、选中导航项、核心主操作按钮。
	/// 融合枫丹水神深海蓝/紫罗兰色散折射与高亮度镜面弧光。
	/// </summary>
	AccentLensed
}

/// <summary>
/// Apple Liquid Glass 核心光学渲染与交互引擎。
/// 彻底抛弃传统死板的单色磨砂/纯白伪亚克力，实现：
/// 1. 实时透镜边缘折射 (Lensing &amp; Edge Refraction)
/// 2. 动态镜面高光与次表面焦散反射 (Specular Highlights &amp; Caustic Rim Dispersion)
/// 3. 指针跟随流体光度偏移与弹性微形变 (Interactive Pointer Lensing &amp; Fluid Response)
/// 4. 环境流体焦散光场画布 (Ambient Caustic Light Field via SkiaSharp)
/// </summary>
public static class LiquidGlassEngine
{
	private sealed class GlassAttachmentState
	{
		public LiquidGlassVariant Variant { get; set; }
		public bool IsDark { get; set; }
		public bool IsHovered { get; set; }
		public bool IsPressed { get; set; }
		public Point NormalizedPointer { get; set; } = new Point(0.25, 0.2);
		public bool Hooked { get; set; }
	}

	private static readonly ConditionalWeakTable<FrameworkElement, GlassAttachmentState> _attachedElements = new();

	/// <summary>
	/// 检测元素当前是否处于深色主题环境
	/// </summary>
	public static bool IsElementDarkTheme(FrameworkElement? element = null)
	{
		try
		{
			if (element != null && element.ActualTheme != ElementTheme.Default)
			{
				return element.ActualTheme == ElementTheme.Dark;
			}
			if (Application.Current != null)
			{
				return Application.Current.RequestedTheme == ApplicationTheme.Dark;
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>
	/// 创建 Apple Liquid Glass 曲面透镜折射背景画刷 (Lensing Surface Brush)。
	/// 模拟光线穿过曲面玻璃时的非线性折射：左上迎光面高折射透亮、中心清澈通透、右下背光面产生水蓝色散聚光。
	/// </summary>
	public static Brush CreateSurfaceBrush(
		LiquidGlassVariant variant = LiquidGlassVariant.Regular,
		bool isDark = false,
		double opacityScale = 1.0,
		Point? pointerNorm = null)
	{
		double px = pointerNorm?.X ?? 0.22;
		double py = pointerNorm?.Y ?? 0.18;

		// 根据指针位置微微偏移折射中心，产生流体光泽移动感
		var startPt = new Point(Math.Clamp(px * 0.45, 0.0, 0.45), Math.Clamp(py * 0.45, 0.0, 0.45));
		var endPt = new Point(Math.Clamp(0.75 + px * 0.25, 0.65, 1.0), Math.Clamp(0.75 + py * 0.25, 0.65, 1.0));

		var brush = new LinearGradientBrush
		{
			StartPoint = startPt,
			EndPoint = endPt
		};

		byte ScaleAlpha(int baseAlpha) => (byte)Math.Clamp((int)Math.Round(baseAlpha * opacityScale), 0, 255);

		if (!isDark)
		{
			// 浅色/明亮流体光学模式 (Crystal Daylight / Furina Hydro Lensing)
			switch (variant)
			{
				case LiquidGlassVariant.Clear:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(195), 255, 255, 255), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(142), 248, 251, 255), Offset = 0.45 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(168), 238, 245, 255), Offset = 0.85 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(190), 245, 249, 255), Offset = 1.0 });
					break;

				case LiquidGlassVariant.FloatingBar:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(222), 255, 255, 255), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(178), 247, 250, 255), Offset = 0.38 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(185), 239, 246, 255), Offset = 0.78 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(212), 244, 248, 255), Offset = 1.0 });
					break;

				case LiquidGlassVariant.AccentLensed:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(225), 238, 244, 255), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(195), 224, 234, 255), Offset = 0.5 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(215), 216, 228, 255), Offset = 1.0 });
					break;

				case LiquidGlassVariant.Regular:
				default:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(214), 255, 255, 255), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(166), 249, 252, 255), Offset = 0.42 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(174), 241, 247, 255), Offset = 0.82 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(202), 248, 251, 255), Offset = 1.0 });
					break;
			}
		}
		else
		{
			// 深色/深海曜石光学模式 (Abyssal Obsidian / Bioluminescent Hydro Lensing)
			switch (variant)
			{
				case LiquidGlassVariant.Clear:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(145), 48, 58, 82), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(105), 26, 33, 50), Offset = 0.5 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(132), 34, 45, 68), Offset = 1.0 });
					break;

				case LiquidGlassVariant.FloatingBar:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(192), 40, 49, 72), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(155), 22, 28, 42), Offset = 0.45 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(175), 28, 37, 58), Offset = 1.0 });
					break;

				case LiquidGlassVariant.AccentLensed:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(190), 56, 78, 138), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(155), 38, 54, 104), Offset = 0.55 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(180), 48, 68, 128), Offset = 1.0 });
					break;

				case LiquidGlassVariant.Regular:
				default:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(168), 42, 52, 74), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(128), 24, 30, 44), Offset = 0.48 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(152), 32, 42, 64), Offset = 1.0 });
					break;
			}
		}

		return brush;
	}

	/// <summary>
	/// 创建 Apple Liquid Glass 标志性的非对称镜面高光与焦散色散描边 (Specular &amp; Caustic Rim Brush)。
	/// 模拟曲面玻璃边缘捕捉光线：迎光侧呈现锐利高光，过渡区半透消隐，背光侧呈现冰蓝/紫罗兰次表面焦散微光。
	/// </summary>
	public static Brush CreateSpecularRimBrush(
		LiquidGlassVariant variant = LiquidGlassVariant.Regular,
		bool isDark = false,
		bool isHovered = false,
		bool isSelected = false,
		Point? pointerNorm = null)
	{
		double px = pointerNorm?.X ?? 0.2;
		double py = pointerNorm?.Y ?? 0.15;

		var brush = new LinearGradientBrush
		{
			StartPoint = new Point(Math.Clamp(px * 0.5, 0.0, 0.5), Math.Clamp(py * 0.5, 0.0, 0.5)),
			EndPoint = new Point(1.0, 1.0)
		};

		if (isSelected || variant == LiquidGlassVariant.AccentLensed)
		{
			// 焦点激活态：高亮度水神蓝/靛青镜面折射环
			brush.GradientStops.Add(new GradientStop { Color = isDark ? Color.FromArgb(245, 165, 215, 255) : Color.FromArgb(245, 99, 102, 241), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = isDark ? Color.FromArgb(170, 99, 102, 241) : Color.FromArgb(165, 129, 140, 248), Offset = 0.45 });
			brush.GradientStops.Add(new GradientStop { Color = isDark ? Color.FromArgb(215, 56, 189, 248) : Color.FromArgb(210, 79, 70, 229), Offset = 1.0 });
			return brush;
		}

		if (!isDark)
		{
			byte topSpecular = (byte)(isHovered ? 255 : 235);
			byte midRim = (byte)(isHovered ? 185 : 135);
			byte causticBottom = (byte)(isHovered ? 225 : 175);

			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(topSpecular, 255, 255, 255), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(midRim, 214, 228, 252), Offset = 0.48 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(causticBottom, isHovered ? (byte)165 : (byte)195, isHovered ? (byte)180 : (byte)210, 252), Offset = 1.0 });
		}
		else
		{
			byte topSpecular = (byte)(isHovered ? 220 : 155);
			byte midRim = (byte)(isHovered ? 95 : 52);
			byte causticBottom = (byte)(isHovered ? 165 : 105);

			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(topSpecular, 225, 240, 255), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(midRim, 110, 145, 215), Offset = 0.5 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(causticBottom, 88, 166, 255), Offset = 1.0 });
		}

		return brush;
	}

	/// <summary>
	/// 为任意 Border / Button / Control 挂载 Apple Liquid Glass 材质与实时指针透镜折射追踪。
	/// </summary>
	public static void AttachInteractiveLiquidGlass(
		FrameworkElement element,
		LiquidGlassVariant variant = LiquidGlassVariant.Regular,
		bool isDark = false,
		bool enablePointerTracking = true)
	{
		if (element == null) return;

		var state = _attachedElements.GetOrCreateValue(element);
		state.Variant = variant;
		state.IsDark = isDark;

		ApplyElementLiquidGlassVisuals(element, state);

		if (!enablePointerTracking || state.Hooked) return;
		state.Hooked = true;

		element.PointerEntered += (s, e) =>
		{
			state.IsHovered = true;
			if (s is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
			{
				var pt = e.GetCurrentPoint(fe).Position;
				state.NormalizedPointer = new Point(
					Math.Clamp(pt.X / fe.ActualWidth, 0.0, 1.0),
					Math.Clamp(pt.Y / fe.ActualHeight, 0.0, 1.0));
			}
			ApplyElementLiquidGlassVisuals(element, state);
		};

		element.PointerMoved += (s, e) =>
		{
			if (!state.IsHovered) return;
			if (s is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
			{
				var pt = e.GetCurrentPoint(fe).Position;
				state.NormalizedPointer = new Point(
					Math.Clamp(pt.X / fe.ActualWidth, 0.0, 1.0),
					Math.Clamp(pt.Y / fe.ActualHeight, 0.0, 1.0));
				ApplyElementLiquidGlassVisuals(element, state);
			}
		};

		element.PointerExited += (s, e) =>
		{
			state.IsHovered = false;
			state.IsPressed = false;
			state.NormalizedPointer = new Point(0.22, 0.18);
			ApplyElementLiquidGlassVisuals(element, state);
		};

		element.PointerPressed += (s, e) =>
		{
			state.IsPressed = true;
			ApplyElementLiquidGlassVisuals(element, state);
		};

		element.PointerReleased += (s, e) =>
		{
			state.IsPressed = false;
			ApplyElementLiquidGlassVisuals(element, state);
		};
	}

	private static void ApplyElementLiquidGlassVisuals(FrameworkElement element, GlassAttachmentState state)
	{
		double opacityScale = state.IsPressed ? 0.94 : (state.IsHovered ? 1.06 : 1.0);
		var surfaceBrush = CreateSurfaceBrush(state.Variant, state.IsDark, opacityScale, state.NormalizedPointer);
		var rimBrush = CreateSpecularRimBrush(state.Variant, state.IsDark, state.IsHovered, state.Variant == LiquidGlassVariant.AccentLensed, state.NormalizedPointer);

		if (element is Border border)
		{
			border.Background = surfaceBrush;
			border.BorderBrush = rimBrush;
			if (border.BorderThickness == default)
			{
				border.BorderThickness = new Thickness(1);
			}
		}
		else if (element is Control control)
		{
			control.Background = surfaceBrush;
			control.BorderBrush = rimBrush;
			if (control.BorderThickness == default)
			{
				control.BorderThickness = new Thickness(1);
			}
		}
		else if (element is Panel panel)
		{
			panel.Background = surfaceBrush;
		}
	}

	/// <summary>
	/// 递归扫描并升级指定容器/页面内的全部传统卡片、面板、胶囊按钮与输入控件为 Apple Liquid Glass 光学材质。
	/// 自动识别并保留高饱和度状态徽标（如绿色的“就绪”、蓝紫色的选中胶囊等），将其升级为 AccentLensed 折射玻璃。
	/// </summary>
	public static void UpgradeVisualTreeToLiquidGlass(DependencyObject root, bool isDark = false)
	{
		if (root == null) return;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			UpgradeSingleNode(child, isDark);
			UpgradeVisualTreeToLiquidGlass(child, isDark);
		}
	}

	private static void UpgradeSingleNode(DependencyObject node, bool isDark)
	{
		if (node is LiquidGlassBorder) return;

		if (node is Border border)
		{
			// 跳过微型色点/圆点徽标 (如 Width <= 24 && Height <= 24)
			if ((border.Width > 0 && border.Width <= 26) || (border.Height > 0 && border.Height <= 26))
			{
				return;
			}

			if (border.Background is SolidColorBrush scb)
			{
				var c = scb.Color;
				if (c.A == 0) return; // 完全透明布局容器不改动

				// 判断是否为浅灰白/纯白背景卡片 (R,G,B 均 >= 225 且色差小)
				int maxC = Math.Max(c.R, Math.Max(c.G, c.B));
				int minC = Math.Min(c.R, Math.Min(c.G, c.B));
				bool isNeutralLight = minC >= 220 && (maxC - minC) <= 22;
				bool isNeutralDark = maxC <= 55 && c.A >= 40;
				bool isSoftIndigoTint = c.B >= 235 && c.R >= 220 && c.G >= 225 && (c.B - c.R) > 8;

				if (isSoftIndigoTint)
				{
					AttachInteractiveLiquidGlass(border, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
				}
				else if (isNeutralLight || isNeutralDark)
				{
					bool isPill = border.CornerRadius.TopLeft >= 14;
					var variant = isPill ? LiquidGlassVariant.Clear : LiquidGlassVariant.Regular;
					AttachInteractiveLiquidGlass(border, variant, isDark, enablePointerTracking: true);
				}
			}
			else if (border.Background is AcrylicBrush)
			{
				AttachInteractiveLiquidGlass(border, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
			}
		}
		else if (node is Button btn)
		{
			if (btn.Background is SolidColorBrush scb)
			{
				var c = scb.Color;
				if (c.A == 0) return; // 纯文本透明按钮保留原样或仅悬停响应

				int maxC = Math.Max(c.R, Math.Max(c.G, c.B));
				int minC = Math.Min(c.R, Math.Min(c.G, c.B));
				bool isNeutralLight = minC >= 215 && (maxC - minC) <= 25;
				bool isSoftIndigo = c.B >= 235 && c.R >= 215 && (c.B - c.R) >= 10;

				if (isSoftIndigo)
				{
					AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
				}
				else if (isNeutralLight)
				{
					AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
				}
			}
		}
		else if (node is ComboBox combo)
		{
			AttachInteractiveLiquidGlass(combo, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
		}
	}

	/// <summary>
	/// 创建基于 SkiaSharp 硬件加速的 Apple Liquid Glass 全景环境流体焦散光场画布 (Ambient Caustic Light Field)。
	/// 呈现类似 iOS 26 / macOS Tahoe / ECHO Pro 的柔和流体色散光斑与曲面玻璃光学环境底色。
	/// </summary>
	public static SKXamlCanvas CreateAmbientCausticCanvas(Func<bool>? isDarkProvider = null)
	{
		var canvas = new SKXamlCanvas
		{
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		canvas.PaintSurface += (sender, e) =>
		{
			bool isDark = isDarkProvider?.Invoke() ?? IsElementDarkTheme(canvas);
			RenderAmbientCausticStage(e.Surface.Canvas, e.Info.Width, e.Info.Height, isDark);
		};

		return canvas;
	}

	/// <summary>
	/// 在指定 Skia 画布上绘制 Apple Liquid Glass 环境流体焦散光场与顶栏透镜高光
	/// </summary>
	public static void RenderAmbientCausticStage(
		SKCanvas skCanvas,
		int width,
		int height,
		bool isDark,
		float pointerNormX = 0.5f,
		float pointerNormY = 0.25f)
	{
		skCanvas.Clear(SKColors.Transparent);
		if (width <= 4 || height <= 4) return;

		float w = width;
		float h = height;

		if (!isDark)
		{
			// 1. 浅色模式：清透冰晶白与枫丹浅海蓝流体光学底色 (Crystal Daylight Stage)
			using (var baseShader = SKShader.CreateLinearGradient(
				new SKPoint(0, 0),
				new SKPoint(w, h),
				new SKColor[]
				{
					new SKColor(236, 243, 255, 245), // 左上冰晶水蓝
					new SKColor(246, 249, 255, 240), // 中心清澈珍珠白
					new SKColor(232, 240, 255, 245)  // 右下柔和浅堇蓝
				},
				new float[] { 0f, 0.52f, 1f },
				SKShaderTileMode.Clamp))
			using (var basePaint = new SKPaint { Shader = baseShader, IsAntialias = true })
			{
				skCanvas.DrawRect(0, 0, w, h, basePaint);
			}

			// 2. 流体焦散折射光球 A (左上 -> 中部水神天青色散 Lensing Lobe)
			DrawCausticLobe(
				skCanvas,
				w * (0.22f + (pointerNormX - 0.5f) * 0.06f),
				h * (0.28f + (pointerNormY - 0.5f) * 0.06f),
				Math.Max(w, h) * 0.52f,
				new SKColor(147, 197, 253, 95),  // Sky 300
				new SKColor(199, 210, 254, 45),  // Indigo 200
				SKColors.Transparent);

			// 3. 流体焦散折射光球 B (右侧深海皇家靛青焦散光带)
			DrawCausticLobe(
				skCanvas,
				w * (0.82f - (pointerNormX - 0.5f) * 0.05f),
				h * (0.62f - (pointerNormY - 0.5f) * 0.05f),
				Math.Max(w, h) * 0.48f,
				new SKColor(129, 140, 248, 82),  // Indigo 400
				new SKColor(165, 243, 252, 40),  // Cyan 200
				SKColors.Transparent);

			// 4. 流体焦散折射光球 C (左下柔光珍珠白高光核)
			DrawCausticLobe(
				skCanvas,
				w * 0.38f,
				h * 0.78f,
				Math.Max(w, h) * 0.42f,
				new SKColor(255, 255, 255, 160),
				new SKColor(224, 242, 254, 70),
				SKColors.Transparent);
		}
		else
		{
			// 1. 深色模式：深海曜石透黑与生物发光水蓝流体光场 (Abyssal Hydro Stage)
			using (var baseShader = SKShader.CreateLinearGradient(
				new SKPoint(0, 0),
				new SKPoint(w, h),
				new SKColor[]
				{
					new SKColor(13, 18, 30, 230),
					new SKColor(18, 24, 38, 222),
					new SKColor(12, 16, 28, 235)
				},
				new float[] { 0f, 0.5f, 1f },
				SKShaderTileMode.Clamp))
			using (var basePaint = new SKPaint { Shader = baseShader, IsAntialias = true })
			{
				skCanvas.DrawRect(0, 0, w, h, basePaint);
			}

			// 2. 深海流体焦散光球 A (左上霓虹水蓝折射晕)
			DrawCausticLobe(
				skCanvas,
				w * (0.20f + (pointerNormX - 0.5f) * 0.08f),
				h * (0.24f + (pointerNormY - 0.5f) * 0.08f),
				Math.Max(w, h) * 0.50f,
				new SKColor(56, 189, 248, 48),   // Sky 400
				new SKColor(99, 102, 241, 24),   // Indigo 500
				SKColors.Transparent);

			// 3. 深海流体焦散光球 B (右下星光靛紫折射晕)
			DrawCausticLobe(
				skCanvas,
				w * (0.80f - (pointerNormX - 0.5f) * 0.06f),
				h * (0.68f - (pointerNormY - 0.5f) * 0.06f),
				Math.Max(w, h) * 0.48f,
				new SKColor(99, 102, 241, 46),   // Indigo 500
				new SKColor(14, 165, 233, 22),   // Sky 500
				SKColors.Transparent);

			// 4. 指针跟随微光透镜 (Interactive Pointer Caustic Glint)
			DrawCausticLobe(
				skCanvas,
				w * pointerNormX,
				h * pointerNormY,
				Math.Min(w, h) * 0.32f,
				new SKColor(125, 211, 252, 26),
				new SKColor(165, 180, 252, 10),
				SKColors.Transparent);
		}

		// 5. 顶部边缘曲面折射高光丝带 (Top Specular Lensing Ribbon)
		using (var topRibbonShader = SKShader.CreateLinearGradient(
			new SKPoint(0, 0),
			new SKPoint(w, 0),
			new SKColor[]
			{
				new SKColor(255, 255, 255, 0),
				new SKColor(255, 255, 255, isDark ? (byte)95 : (byte)215),
				new SKColor(165, 215, 255, isDark ? (byte)110 : (byte)230),
				new SKColor(255, 255, 255, 0)
			},
			new float[] { 0f, 0.25f, 0.72f, 1f },
			SKShaderTileMode.Clamp))
		using (var ribbonPaint = new SKPaint
		{
			Shader = topRibbonShader,
			StrokeWidth = 1.5f,
			IsAntialias = true,
			Style = SKPaintStyle.Stroke
		})
		{
			skCanvas.DrawLine(16, 1f, w - 16, 1f, ribbonPaint);
		}
	}

	private static void DrawCausticLobe(
		SKCanvas canvas,
		float cx,
		float cy,
		float radius,
		SKColor innerColor,
		SKColor midColor,
		SKColor outerColor)
	{
		if (radius <= 1f) return;
		using var shader = SKShader.CreateRadialGradient(
			new SKPoint(cx, cy),
			radius,
			new SKColor[] { innerColor, midColor, outerColor },
			new float[] { 0f, 0.55f, 1f },
			SKShaderTileMode.Clamp);
		using var paint = new SKPaint
		{
			Shader = shader,
			IsAntialias = true,
			BlendMode = SKBlendMode.SrcOver
		};
		canvas.DrawCircle(cx, cy, radius, paint);
	}
}
