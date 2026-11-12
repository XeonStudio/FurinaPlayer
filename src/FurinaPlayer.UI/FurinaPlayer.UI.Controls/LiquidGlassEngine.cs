using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
/// ECHO 主题预设模型 (支持内置预设、随机对比度安全生成与自定义主题导入/新建)
/// </summary>
public sealed class EchoThemePreset
{
	public string Id { get; set; } = "classic";
	public string Name { get; set; } = "经典";
	public string Category { get; set; } = "简洁";
	public string Description { get; set; } = "";
	public bool IsDark { get; set; }
	public Color StageTopColor { get; set; }
	public Color StageMidColor { get; set; }
	public Color StageBottomColor { get; set; }
	public Color CardTintColor { get; set; }
	public Color AccentPrimary { get; set; }
	public Color AccentSecondary { get; set; }
	public Color Swatch1 { get; set; }
	public Color Swatch2 { get; set; }
	public Color Swatch3 { get; set; }
	public bool IsCustom { get; set; }
}

/// <summary>
/// Apple Liquid Glass &amp; ECHO 外观系统核心光学渲染与交互引擎。
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
	private static readonly List<WeakReference<FrameworkElement>> _trackedElements = new();
	private static readonly object _trackLock = new();
	private static readonly Random _rng = new();

	public static readonly string[] ThemeCategories = new[]
	{
		"全部", "我的", "简洁", "柔和", "和纸", "夜色", "工作室", "个性"
	};

	public static IReadOnlyList<EchoThemePreset> BuiltInPresets { get; } = CreateBuiltInPresets();
	public static List<EchoThemePreset> CustomPresets { get; } = new();

	public static EchoThemePreset ActivePreset { get; private set; } = BuiltInPresets[0];
	public static string ActiveThemeMode { get; set; } = "liquid-glass"; // "light", "dark", "system", "ambient", "acrylic", "liquid-glass"
	public static double AcrylicOpacityScale { get; set; } = 1.0; // 0.0 (不透明) .. 1.0 (100% 通透)
	public static bool KeepTranslucentWhenUnfocused { get; set; } = true;
	public static bool IsWindowFocused { get; set; } = true;
	public static bool RainbowModeEnabled { get; set; } = false;
	public static float RainbowHueDegrees { get; set; } = 0f;
	public static bool InvertModeEnabled { get; set; } = false;

	public static Color ParseHexColor(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex)) return Colors.White;
		string s = hex.Trim().TrimStart('#');
		try
		{
			if (s.Length == 6)
			{
				byte r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
				byte g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
				byte b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
				return Color.FromArgb(255, r, g, b);
			}
			if (s.Length == 8)
			{
				byte a = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
				byte r = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
				byte g = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
				byte b = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber);
				return Color.FromArgb(a, r, g, b);
			}
		}
		catch
		{
		}
		return Colors.White;
	}

	public static string ToHexColor(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

	private static EchoThemePreset MakePreset(
		string id,
		string name,
		string category,
		string desc,
		bool isDark,
		string stageTopHex,
		string stageMidHex,
		string accentPrimaryHex,
		string? accentSecondaryHex = null,
		string? cardTintHex = null,
		bool isCustom = false)
	{
		var top = ParseHexColor(stageTopHex);
		var mid = ParseHexColor(stageMidHex);
		var pri = ParseHexColor(accentPrimaryHex);
		var sec = !string.IsNullOrEmpty(accentSecondaryHex) ? ParseHexColor(accentSecondaryHex!) : BlendColor(pri, mid, 0.35);
		var bot = BlendColor(mid, sec, 0.16);
		var card = !string.IsNullOrEmpty(cardTintHex) ? ParseHexColor(cardTintHex!) : BlendColor(top, isDark ? Color.FromArgb(255, 28, 36, 54) : Colors.White, 0.55);

		return new EchoThemePreset
		{
			Id = id,
			Name = name,
			Category = category,
			Description = desc,
			IsDark = isDark,
			StageTopColor = top,
			StageMidColor = mid,
			StageBottomColor = bot,
			CardTintColor = card,
			AccentPrimary = pri,
			AccentSecondary = sec,
			Swatch1 = top,
			Swatch2 = mid,
			Swatch3 = pri,
			IsCustom = isCustom
		};
	}

	private static List<EchoThemePreset> CreateBuiltInPresets()
	{
		return new List<EchoThemePreset>
		{
			MakePreset("classic", "经典", "简洁", "保留经典蓝紫主色，以低饱和灰阶区分背景与面板。", false, "#F8FAFC", "#EEF2FF", "#6366F1", "#38BDF8", "#FFFFFF"),
			MakePreset("twilight-peach", "暮光桃雾", "柔和", "老版 ECHO 的暖粉渐变感。", false, "#FFF5F2", "#FCE7F3", "#E11D48", "#FB7185", "#FFF9FB"),
			MakePreset("sakura-cream", "樱粉奶霜", "柔和", "奶白粉底配樱粉强调。", false, "#FFF8FA", "#FCE7F3", "#DB2777", "#F472B6", "#FFFAFC"),
			MakePreset("peach-soda", "蜜桃苏打", "柔和", "蜜桃橙和苏打青的轻快组合。", false, "#FFF7ED", "#FFEDD5", "#EA580C", "#0D9488", "#FFFBF5"),
			MakePreset("mint-gummy", "薄荷软糖", "柔和", "薄荷绿、奶油白和一点桃粉。", false, "#F0FDF4", "#DCFCE7", "#059669", "#EC4899", "#F7FEF9"),
			MakePreset("blueberry-star", "蓝莓星糖", "柔和", "莓紫云白，带一点梦幻粉光。", false, "#F5F3FF", "#EDE9FE", "#7C3AED", "#EC4899", "#FAF8FF"),
			MakePreset("matcha-butter", "抹茶奶油", "和纸", "抹茶绿和奶油黄，更安静耐看。", false, "#F7FEE7", "#ECFCCB", "#4D7C0F", "#D97706", "#FCFDF5"),
			MakePreset("washi-ink", "宣纸墨韵", "和纸", "温润和纸质感配水墨灰蓝强调。", false, "#FAF8F5", "#F3EFE6", "#475569", "#64748B", "#FDFBF7"),
			MakePreset("pure-minimal", "极简纯白", "简洁", "纯净高对比度的现代极简浅色面板。", false, "#FFFFFF", "#F1F5F9", "#2563EB", "#3B82F6", "#FFFFFF"),
			MakePreset("fontaine-hydro", "枫丹水神", "简洁", "芙宁娜专属水神蓝白晶莹配色。", false, "#F0F7FF", "#E0F2FE", "#3B82F6", "#6366F1", "#F8FBFF"),
			MakePreset("obsidian-night", "深海曜石", "夜色", "深邃海渊蓝黑底色，柔和护眼。", true, "#0D121E", "#182236", "#38BDF8", "#6366F1", "#1E293B"),
			MakePreset("midnight-neon", "午夜霓虹", "夜色", "暗夜紫黑背景配流光霓虹强调。", true, "#0F0E1A", "#1E1B36", "#818CF8", "#C084FC", "#252140"),
			MakePreset("mastering-dark", "母带暗房", "工作室", "专业音频工作室暗色高对比监听风格。", true, "#121316", "#1E2026", "#F59E0B", "#10B981", "#242730"),
			MakePreset("titanium-studio", "钛灰监听", "工作室", "精密仪器级深灰金属质感与翠绿指示。", true, "#18181B", "#27272A", "#10B981", "#06B6D4", "#27272A"),
			MakePreset("sunset-gold", "落日鎏金", "个性", "琥珀暖金与香槟白渐变格调。", false, "#FFFBEB", "#FEF3C7", "#D97706", "#EA580C", "#FFFDF5")
		};
	}

	public static IEnumerable<EchoThemePreset> GetAllPresets()
	{
		foreach (var p in CustomPresets)
		{
			yield return p;
		}
		foreach (var p in BuiltInPresets)
		{
			yield return p;
		}
	}

	public static EchoThemePreset GetPresetById(string? id)
	{
		if (!string.IsNullOrWhiteSpace(id))
		{
			var match = GetAllPresets().FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
			if (match != null) return match;
		}
		return BuiltInPresets[0];
	}

	public static void SelectPreset(string? id)
	{
		ActivePreset = GetPresetById(id);
	}

	public static void SelectPreset(EchoThemePreset preset)
	{
		if (preset != null)
		{
			ActivePreset = preset;
		}
	}

	/// <summary>
	/// 生成随机高可读性主题预设 (随机主题卡片)
	/// </summary>
	public static EchoThemePreset GenerateRandomPreset(bool preferDark = false)
	{
		float hue = (float)(_rng.NextDouble() * 360.0);
		float secHue = (hue + 38f + (float)(_rng.NextDouble() * 45.0)) % 360f;
		string[] poeticNames = new[]
		{
			"星海琉璃", "晨曦薄雾", "鸢尾流光", "青柠微风", "珊瑚浅滩", "松烟墨玉", "晴空飞羽", "紫晶幻梦"
		};
		string title = poeticNames[_rng.Next(poeticNames.Length)];

		Color top, mid, pri, sec, card;
		if (!preferDark)
		{
			top = HslToRgb(hue, 0.65f, 0.97f);
			mid = HslToRgb(hue, 0.72f, 0.92f);
			pri = HslToRgb(hue, 0.78f, 0.45f);
			sec = HslToRgb(secHue, 0.75f, 0.50f);
			card = HslToRgb(hue, 0.50f, 0.985f);
		}
		else
		{
			top = HslToRgb(hue, 0.38f, 0.09f);
			mid = HslToRgb(hue, 0.42f, 0.15f);
			pri = HslToRgb(hue, 0.85f, 0.62f);
			sec = HslToRgb(secHue, 0.80f, 0.58f);
			card = HslToRgb(hue, 0.35f, 0.18f);
		}

		var preset = new EchoThemePreset
		{
			Id = "random-" + _rng.Next(1000, 9999),
			Name = title,
			Category = "个性",
			Description = "智能生成并校验对比度，保证文字和按钮清晰可读。",
			IsDark = preferDark,
			StageTopColor = top,
			StageMidColor = mid,
			StageBottomColor = BlendColor(mid, sec, 0.18),
			CardTintColor = card,
			AccentPrimary = pri,
			AccentSecondary = sec,
			Swatch1 = top,
			Swatch2 = mid,
			Swatch3 = pri,
			IsCustom = true
		};

		// Keep most recent random or custom preset accessible
		CustomPresets.RemoveAll(p => p.Id.StartsWith("random-", StringComparison.OrdinalIgnoreCase));
		CustomPresets.Insert(0, preset);
		ActivePreset = preset;
		return preset;
	}

	public static EchoThemePreset AddCustomPreset(string name, string stageTopHex, string stageMidHex, string accentHex, bool isDark)
	{
		string id = "custom-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var preset = MakePreset(
			id,
			string.IsNullOrWhiteSpace(name) ? "我的主题" : name.Trim(),
			"我的",
			"用户自定义专属主题配色预设。",
			isDark,
			stageTopHex,
			stageMidHex,
			accentHex,
			isCustom: true);
		CustomPresets.Insert(0, preset);
		ActivePreset = preset;
		return preset;
	}

	public static string SerializeCustomPresets()
	{
		try
		{
			var dtos = CustomPresets
				.Where(p => !p.Id.StartsWith("random-", StringComparison.OrdinalIgnoreCase))
				.Select(p => new Dictionary<string, string>
				{
					["id"] = p.Id,
					["name"] = p.Name,
					["category"] = p.Category,
					["desc"] = p.Description,
					["isDark"] = p.IsDark ? "1" : "0",
					["top"] = ToHexColor(p.StageTopColor),
					["mid"] = ToHexColor(p.StageMidColor),
					["pri"] = ToHexColor(p.AccentPrimary),
					["sec"] = ToHexColor(p.AccentSecondary)
				})
				.ToList();
			return JsonSerializer.Serialize(dtos);
		}
		catch
		{
			return "[]";
		}
	}

	public static void LoadCustomPresetsFromJson(string? json)
	{
		if (string.IsNullOrWhiteSpace(json)) return;
		try
		{
			var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
			if (list == null) return;
			CustomPresets.Clear();
			foreach (var item in list)
			{
				string id = item.TryGetValue("id", out var vId) ? vId : ("custom-" + _rng.Next(1000, 9999));
				string name = item.TryGetValue("name", out var vName) ? vName : "我的主题";
				string cat = item.TryGetValue("category", out var vCat) ? vCat : "我的";
				string desc = item.TryGetValue("desc", out var vDesc) ? vDesc : "自定义主题配色预设。";
				bool isDark = item.TryGetValue("isDark", out var vDark) && vDark == "1";
				string top = item.TryGetValue("top", out var vTop) ? vTop : "#F8FAFC";
				string mid = item.TryGetValue("mid", out var vMid) ? vMid : "#EEF2FF";
				string pri = item.TryGetValue("pri", out var vPri) ? vPri : "#6366F1";
				string? sec = item.TryGetValue("sec", out var vSec) ? vSec : null;
				CustomPresets.Add(MakePreset(id, name, cat, desc, isDark, top, mid, pri, sec, isCustom: true));
			}
		}
		catch
		{
		}
	}

	private static Color BlendColor(Color a, Color b, double t)
	{
		t = Math.Clamp(t, 0.0, 1.0);
		byte r = (byte)Math.Clamp((int)Math.Round(a.R + (b.R - a.R) * t), 0, 255);
		byte g = (byte)Math.Clamp((int)Math.Round(a.G + (b.G - a.G) * t), 0, 255);
		byte bl = (byte)Math.Clamp((int)Math.Round(a.B + (b.B - a.B) * t), 0, 255);
		byte al = (byte)Math.Clamp((int)Math.Round(a.A + (b.A - a.A) * t), 0, 255);
		return Color.FromArgb(al, r, g, bl);
	}

	private static Color TransformActiveColor(Color c)
	{
		Color result = c;
		if (RainbowModeEnabled && Math.Abs(RainbowHueDegrees) > 0.01f)
		{
			RgbToHsl(result, out float h, out float s, out float l);
			h = (h + RainbowHueDegrees) % 360f;
			if (h < 0f) h += 360f;
			s = Math.Clamp(s * 1.12f, 0.15f, 0.95f);
			var shifted = HslToRgb(h, s, l);
			result = Color.FromArgb(result.A, shifted.R, shifted.G, shifted.B);
		}
		if (InvertModeEnabled)
		{
			RgbToHsl(result, out float h, out float s, out float l);
			float invL = Math.Clamp(1.0f - l, 0.08f, 0.94f);
			var inv = HslToRgb(h, s, invL);
			result = Color.FromArgb(result.A, inv.R, inv.G, inv.B);
		}
		return result;
	}

	private static void RgbToHsl(Color c, out float h, out float s, out float l)
	{
		float r = c.R / 255f;
		float g = c.G / 255f;
		float b = c.B / 255f;
		float max = Math.Max(r, Math.Max(g, b));
		float min = Math.Min(r, Math.Min(g, b));
		l = (max + min) * 0.5f;
		if (Math.Abs(max - min) < 0.0001f)
		{
			h = 0f;
			s = 0f;
			return;
		}
		float d = max - min;
		s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
		if (Math.Abs(max - r) < 0.0001f)
			h = (g - b) / d + (g < b ? 6f : 0f);
		else if (Math.Abs(max - g) < 0.0001f)
			h = (b - r) / d + 2f;
		else
			h = (r - g) / d + 4f;
		h *= 60f;
	}

	private static Color HslToRgb(float h, float s, float l)
	{
		h = ((h % 360f) + 360f) % 360f;
		s = Math.Clamp(s, 0f, 1f);
		l = Math.Clamp(l, 0f, 1f);
		float c = (1f - Math.Abs(2f * l - 1f)) * s;
		float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
		float m = l - c * 0.5f;
		float r1, g1, b1;
		if (h < 60f) { r1 = c; g1 = x; b1 = 0f; }
		else if (h < 120f) { r1 = x; g1 = c; b1 = 0f; }
		else if (h < 180f) { r1 = 0f; g1 = c; b1 = x; }
		else if (h < 240f) { r1 = 0f; g1 = x; b1 = c; }
		else if (h < 300f) { r1 = x; g1 = 0f; b1 = c; }
		else { r1 = c; g1 = 0f; b1 = x; }
		return Color.FromArgb(
			255,
			(byte)Math.Clamp((int)Math.Round((r1 + m) * 255f), 0, 255),
			(byte)Math.Clamp((int)Math.Round((g1 + m) * 255f), 0, 255),
			(byte)Math.Clamp((int)Math.Round((b1 + m) * 255f), 0, 255));
	}

	/// <summary>
	/// 检测元素当前是否处于深色主题环境
	/// </summary>
	public static bool IsElementDarkTheme(FrameworkElement? element = null)
	{
		try
		{
			if (element != null && element.IsLoaded && element.ActualTheme != ElementTheme.Default)
			{
				// RootGrid.RequestedTheme already reflects MainWindow's resolved theme (including InvertModeEnabled).
				return element.ActualTheme == ElementTheme.Dark;
			}
			if (string.Equals(ActiveThemeMode, "dark", StringComparison.OrdinalIgnoreCase)) return !InvertModeEnabled;
			if (string.Equals(ActiveThemeMode, "light", StringComparison.OrdinalIgnoreCase)) return InvertModeEnabled;
			if (ActivePreset != null && (ActiveThemeMode == "ambient" || ActiveThemeMode == "acrylic" || ActiveThemeMode == "liquid-glass"))
			{
				return InvertModeEnabled ? !ActivePreset.IsDark : ActivePreset.IsDark;
			}
			// 优先获取 Windows 系统真实 UI 主题颜色 (UISettings)
			try
			{
				var uiSettings = new Windows.UI.ViewManagement.UISettings();
				var bg = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
				bool sysDark = (bg.R < 128 && bg.G < 128 && bg.B < 128);
				return InvertModeEnabled ? !sysDark : sysDark;
			}
			catch
			{
			}
			if (Application.Current != null)
			{
				bool appDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
				return InvertModeEnabled ? !appDark : appDark;
			}
		}
		catch
		{
		}
		return InvertModeEnabled;
	}

	/// <summary>
	/// 创建 Apple Liquid Glass 曲面透镜折射背景画刷 (Lensing Surface Brush)。
	/// 融合当前 ECHO 主题预设色彩、彩虹色相偏移与亚克力通透度。
	/// </summary>
	public static Brush CreateSurfaceBrush(
		LiquidGlassVariant variant = LiquidGlassVariant.Regular,
		bool isDark = false,
		double opacityScale = 1.0,
		Point? pointerNorm = null)
	{
		double px = pointerNorm?.X ?? 0.22;
		double py = pointerNorm?.Y ?? 0.18;

		var startPt = new Point(Math.Clamp(px * 0.45, 0.0, 0.45), Math.Clamp(py * 0.45, 0.0, 0.45));
		var endPt = new Point(Math.Clamp(0.75 + px * 0.25, 0.65, 1.0), Math.Clamp(0.75 + py * 0.25, 0.65, 1.0));

		var brush = new LinearGradientBrush
		{
			StartPoint = startPt,
			EndPoint = endPt
		};

		// 当处于亚克力/液态玻璃/氛围模式或设置了亚克力透明度时，卡片表面适度透出后方焦散光斑与桌面背景，同时保证文字对比度
		bool isTranslucentMode = ActiveThemeMode == "acrylic" || ActiveThemeMode == "liquid-glass" || ActiveThemeMode == "ambient" || ActiveThemeMode == "system";
		double opacityClamped = Math.Clamp(AcrylicOpacityScale, 0.0, 1.0);
		double modeAlphaFactor = isTranslucentMode
			? (0.65 + (1.0 - opacityClamped) * 0.33)
			: (0.80 + (1.0 - opacityClamped) * 0.18);

		if (isTranslucentMode && !KeepTranslucentWhenUnfocused && !IsWindowFocused)
		{
			modeAlphaFactor = 0.96;
		}

		byte ScaleAlpha(int baseAlpha) => (byte)Math.Clamp((int)Math.Round(baseAlpha * opacityScale * modeAlphaFactor), 0, 255);

		var preset = ActivePreset ?? BuiltInPresets[0];
		Color topTint = TransformActiveColor(preset.StageTopColor);
		Color midTint = TransformActiveColor(preset.StageMidColor);
		Color cardTint = TransformActiveColor(preset.CardTintColor);
		Color accentPri = TransformActiveColor(preset.AccentPrimary);

		if (!isDark)
		{
			Color c0 = BlendColor(Colors.White, cardTint, 0.35);
			Color c1 = BlendColor(Colors.White, topTint, 0.55);
			Color c2 = BlendColor(Colors.White, midTint, 0.62);
			Color c3 = BlendColor(Colors.White, cardTint, 0.45);

			switch (variant)
			{
				case LiquidGlassVariant.Clear:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(188), c0.R, c0.G, c0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(138), c1.R, c1.G, c1.B), Offset = 0.45 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(162), c2.R, c2.G, c2.B), Offset = 0.85 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(182), c3.R, c3.G, c3.B), Offset = 1.0 });
					break;

				case LiquidGlassVariant.FloatingBar:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(215), c0.R, c0.G, c0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(172), c1.R, c1.G, c1.B), Offset = 0.38 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(180), c2.R, c2.G, c2.B), Offset = 0.78 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(205), c3.R, c3.G, c3.B), Offset = 1.0 });
					break;

				case LiquidGlassVariant.AccentLensed:
				{
					Color a0 = BlendColor(Colors.White, accentPri, 0.16);
					Color a1 = BlendColor(Colors.White, accentPri, 0.26);
					Color a2 = BlendColor(Colors.White, accentPri, 0.20);
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(224), a0.R, a0.G, a0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(196), a1.R, a1.G, a1.B), Offset = 0.5 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(214), a2.R, a2.G, a2.B), Offset = 1.0 });
					break;
				}

				case LiquidGlassVariant.Regular:
				default:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(206), c0.R, c0.G, c0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(158), c1.R, c1.G, c1.B), Offset = 0.42 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(168), c2.R, c2.G, c2.B), Offset = 0.82 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(196), c3.R, c3.G, c3.B), Offset = 1.0 });
					break;
			}
		}
		else
		{
			Color baseDark0 = preset.IsDark ? BlendColor(Color.FromArgb(255, 38, 48, 70), cardTint, 0.55) : Color.FromArgb(255, 42, 52, 74);
			Color baseDark1 = preset.IsDark ? BlendColor(Color.FromArgb(255, 22, 28, 42), topTint, 0.55) : Color.FromArgb(255, 24, 30, 44);
			Color baseDark2 = preset.IsDark ? BlendColor(Color.FromArgb(255, 30, 40, 60), midTint, 0.55) : Color.FromArgb(255, 32, 42, 64);

			switch (variant)
			{
				case LiquidGlassVariant.Clear:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(148), baseDark0.R, baseDark0.G, baseDark0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(108), baseDark1.R, baseDark1.G, baseDark1.B), Offset = 0.5 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(135), baseDark2.R, baseDark2.G, baseDark2.B), Offset = 1.0 });
					break;

				case LiquidGlassVariant.FloatingBar:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(192), baseDark0.R, baseDark0.G, baseDark0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(155), baseDark1.R, baseDark1.G, baseDark1.B), Offset = 0.45 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(175), baseDark2.R, baseDark2.G, baseDark2.B), Offset = 1.0 });
					break;

				case LiquidGlassVariant.AccentLensed:
				{
					Color ad0 = BlendColor(baseDark0, accentPri, 0.42);
					Color ad1 = BlendColor(baseDark1, accentPri, 0.34);
					Color ad2 = BlendColor(baseDark2, accentPri, 0.38);
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(195), ad0.R, ad0.G, ad0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(160), ad1.R, ad1.G, ad1.B), Offset = 0.55 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(185), ad2.R, ad2.G, ad2.B), Offset = 1.0 });
					break;
				}

				case LiquidGlassVariant.Regular:
				default:
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(172), baseDark0.R, baseDark0.G, baseDark0.B), Offset = 0.0 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(132), baseDark1.R, baseDark1.G, baseDark1.B), Offset = 0.48 });
					brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(ScaleAlpha(156), baseDark2.R, baseDark2.G, baseDark2.B), Offset = 1.0 });
					break;
			}
		}

		return brush;
	}

	/// <summary>
	/// 创建 Apple Liquid Glass 标志性的非对称镜面高光与焦散色散描边 (Specular &amp; Caustic Rim Brush)。
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

		var preset = ActivePreset ?? BuiltInPresets[0];
		Color accentPri = TransformActiveColor(preset.AccentPrimary);
		Color accentSec = TransformActiveColor(preset.AccentSecondary);

		if (isSelected || variant == LiquidGlassVariant.AccentLensed)
		{
			Color topC = isDark ? BlendColor(Colors.White, accentPri, 0.45) : accentPri;
			Color midC = BlendColor(accentPri, accentSec, 0.5);
			Color botC = isDark ? accentSec : BlendColor(accentPri, Colors.Black, 0.12);
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(245, topC.R, topC.G, topC.B), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(170, midC.R, midC.G, midC.B), Offset = 0.45 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(215, botC.R, botC.G, botC.B), Offset = 1.0 });
			return brush;
		}

		if (!isDark)
		{
			byte topSpecular = (byte)(isHovered ? 255 : 235);
			byte midRim = (byte)(isHovered ? 185 : 138);
			byte causticBottom = (byte)(isHovered ? 220 : 170);

			Color midColor = BlendColor(Color.FromArgb(255, 214, 228, 252), accentPri, 0.25);
			Color botColor = BlendColor(Color.FromArgb(255, 195, 210, 252), accentSec, isHovered ? 0.45 : 0.28);

			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(topSpecular, 255, 255, 255), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(midRim, midColor.R, midColor.G, midColor.B), Offset = 0.48 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(causticBottom, botColor.R, botColor.G, botColor.B), Offset = 1.0 });
		}
		else
		{
			byte topSpecular = (byte)(isHovered ? 220 : 155);
			byte midRim = (byte)(isHovered ? 105 : 58);
			byte causticBottom = (byte)(isHovered ? 175 : 112);

			Color midColor = BlendColor(Color.FromArgb(255, 110, 145, 215), accentPri, 0.35);
			Color botColor = BlendColor(Color.FromArgb(255, 88, 166, 255), accentSec, 0.45);

			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(topSpecular, 225, 240, 255), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(midRim, midColor.R, midColor.G, midColor.B), Offset = 0.5 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(causticBottom, botColor.R, botColor.G, botColor.B), Offset = 1.0 });
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
		bool enablePointerTracking = false)
	{
		if (element == null) return;

		// 检查若按钮 Background 为透明或无边框，绝不添加边框或顶部高光
		if (element is Button btn && IsTransparentOrBorderlessButton(btn))
		{
			btn.BorderThickness = new Thickness(0);
			btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
			return;
		}

		bool alreadyTracked = _attachedElements.TryGetValue(element, out _);
		var state = _attachedElements.GetOrCreateValue(element);
		state.Variant = variant;
		state.IsDark = isDark;

		if (!alreadyTracked)
		{
			lock (_trackLock)
			{
				_trackedElements.Add(new WeakReference<FrameworkElement>(element));
			}
		}

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

		// 移除 PointerMoved 中的高频重绘，子元素禁用 PointerMoved 重绘，彻底消除闪烁

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

	/// <summary>
	/// 检查按钮是否为透明背景、无边框或纯图标按钮，避免误添加带顶部高光的渐变边框
	/// </summary>
	public static bool IsTransparentOrBorderlessButton(Button btn)
	{
		if (btn == null) return false;

		// 1. 图标类按钮 (FontIcon, PathIcon, SymbolIcon)
		if (btn.Content is FontIcon || btn.Content is PathIcon || btn.Content is SymbolIcon)
		{
			return true;
		}

		// 2. Background 为透明或未设置
		if (btn.Background == null)
		{
			return true;
		}
		if (btn.Background is SolidColorBrush scb && (scb.Color.A == 0 || scb.Color == Colors.Transparent))
		{
			return true;
		}

		// 3. 边框显式为 0 且无边框画刷或画刷透明
		if (btn.BorderThickness.Left == 0 && btn.BorderThickness.Top == 0 &&
		    btn.BorderThickness.Right == 0 && btn.BorderThickness.Bottom == 0)
		{
			if (btn.BorderBrush == null || (btn.BorderBrush is SolidColorBrush bscb && (bscb.Color.A == 0 || bscb.Color == Colors.Transparent)))
			{
				return true;
			}
		}

		return false;
	}

	private static void ApplyElementLiquidGlassVisuals(FrameworkElement element, GlassAttachmentState state)
	{
		// 检查若按钮 Background 为透明或无边框，绝不添加边框或顶部高光
		if (element is Button btn && IsTransparentOrBorderlessButton(btn))
		{
			btn.BorderThickness = new Thickness(0);
			btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
			return;
		}

		double opacityScale = state.IsPressed ? 0.94 : (state.IsHovered ? 1.06 : 1.0);
		var surfaceBrush = CreateSurfaceBrush(state.Variant, state.IsDark, opacityScale, state.NormalizedPointer);
		var rimBrush = CreateSpecularRimBrush(state.Variant, state.IsDark, state.IsHovered, state.Variant == LiquidGlassVariant.AccentLensed, state.NormalizedPointer);

		if (element is Border border)
		{
			// 若 Border 为透明或无边框，绝不添加边框或顶部高光
			bool isBorderless = border.BorderThickness.Left == 0 && border.BorderThickness.Top == 0 &&
			                    border.BorderThickness.Right == 0 && border.BorderThickness.Bottom == 0;
			bool isTransparentBg = border.Background == null ||
			                       (border.Background is SolidColorBrush scb && (scb.Color.A == 0 || scb.Color == Colors.Transparent));

			if (isBorderless && isTransparentBg)
			{
				border.BorderThickness = new Thickness(0);
				border.BorderBrush = new SolidColorBrush(Colors.Transparent);
				return;
			}

			border.Background = surfaceBrush;
			border.BorderBrush = rimBrush;
			if (border.BorderThickness == default)
			{
				border.BorderThickness = new Thickness(1);
			}
		}
		else if (element is Control control)
		{
			bool isBorderless = control.BorderThickness.Left == 0 && control.BorderThickness.Top == 0 &&
			                    control.BorderThickness.Right == 0 && control.BorderThickness.Bottom == 0;
			bool isTransparentBg = control.Background == null ||
			                       (control.Background is SolidColorBrush scb && (scb.Color.A == 0 || scb.Color == Colors.Transparent));

			if (isBorderless || isTransparentBg)
			{
				// 若为透明或无边框控件，绝不添加边框或顶部高光
				control.BorderThickness = new Thickness(0);
				control.BorderBrush = new SolidColorBrush(Colors.Transparent);
			}
			else
			{
				control.Background = surfaceBrush;
				control.BorderBrush = rimBrush;
				if (control.BorderThickness == default)
				{
					control.BorderThickness = new Thickness(1);
				}
			}
		}
		else if (element is Panel panel)
		{
			panel.Background = surfaceBrush;
		}
	}

	/// <summary>
	/// 当主题预设、深浅色、彩虹模式、反色模式或透明度变更时，实时刷新所有已挂载的液态玻璃控件
	/// </summary>
	public static void RefreshAllAttachedElements(bool isDark)
	{
		lock (_trackLock)
		{
			for (int i = _trackedElements.Count - 1; i >= 0; i--)
			{
				if (_trackedElements[i].TryGetTarget(out var element) && element != null)
				{
					if (_attachedElements.TryGetValue(element, out var state))
					{
						state.IsDark = isDark;
						ApplyElementLiquidGlassVisuals(element, state);
					}
				}
				else
				{
					_trackedElements.RemoveAt(i);
				}
			}
		}
	}

	/// <summary>
	/// 递归扫描并升级指定容器/页面内的全部传统卡片、面板、胶囊按钮与输入控件为 Apple Liquid Glass 光学材质。
	/// 同时支持对已升级节点在切换主题/预设时实时刷新。
	/// </summary>
	public static void UpgradeVisualTreeToLiquidGlass(DependencyObject root, bool isDark = false)
	{
		if (root == null) return;

		// 如果 root 本身就是透明/无边框按钮或纯图标按钮，避免修改与深入其内部模板
		if (root is Button rootBtn && IsTransparentOrBorderlessButton(rootBtn))
		{
			rootBtn.BorderThickness = new Thickness(0);
			rootBtn.BorderBrush = new SolidColorBrush(Colors.Transparent);
			return;
		}

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Button childBtn && IsTransparentOrBorderlessButton(childBtn))
			{
				childBtn.BorderThickness = new Thickness(0);
				childBtn.BorderBrush = new SolidColorBrush(Colors.Transparent);
				continue;
			}
			UpgradeSingleNode(child, isDark);
			UpgradeVisualTreeToLiquidGlass(child, isDark);
		}
	}

	private static void UpgradeSingleNode(DependencyObject node, bool isDark)
	{
		if (node is LiquidGlassBorder) return;

		if (node is FrameworkElement fe && _attachedElements.TryGetValue(fe, out var existingState))
		{
			if (fe is Button existingBtn && IsTransparentOrBorderlessButton(existingBtn))
			{
				existingBtn.BorderThickness = new Thickness(0);
				existingBtn.BorderBrush = new SolidColorBrush(Colors.Transparent);
				return;
			}
			existingState.IsDark = isDark;
			ApplyElementLiquidGlassVisuals(fe, existingState);
			return;
		}

		if (node is Border border)
		{
			if ((border.Width > 0 && border.Width <= 26) || (border.Height > 0 && border.Height <= 26))
			{
				return;
			}

			// 若 Border 为透明或未设置背景，绝不强加高光边框
			if (border.Background == null || (border.Background is SolidColorBrush bscb && (bscb.Color.A == 0 || bscb.Color == Colors.Transparent)))
			{
				return;
			}

			if (border.Background is SolidColorBrush scb)
			{
				var c = scb.Color;
				if (c.A == 0) return;

				int maxC = Math.Max(c.R, Math.Max(c.G, c.B));
				int minC = Math.Min(c.R, Math.Min(c.G, c.B));
				bool isNeutralLight = minC >= 220 && (maxC - minC) <= 22;
				bool isNeutralDark = maxC <= 55 && c.A >= 40;
				bool isSoftIndigoTint = c.B >= 235 && c.R >= 220 && c.G >= 225 && (c.B - c.R) > 8;

				if (isSoftIndigoTint)
				{
					AttachInteractiveLiquidGlass(border, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: false);
				}
				else if (isNeutralLight || isNeutralDark)
				{
					bool isPill = border.CornerRadius.TopLeft >= 14;
					var variant = isPill ? LiquidGlassVariant.Clear : LiquidGlassVariant.Regular;
					AttachInteractiveLiquidGlass(border, variant, isDark, enablePointerTracking: false);
				}
			}
			else if (border.Background is AcrylicBrush)
			{
				AttachInteractiveLiquidGlass(border, LiquidGlassVariant.Regular, isDark, enablePointerTracking: false);
			}
		}
		else if (node is Button btn)
		{
			// 检查若按钮 Background 为透明或无边框，或者是纯图标按钮，绝不添加边框或顶部高光
			if (IsTransparentOrBorderlessButton(btn))
			{
				btn.BorderThickness = new Thickness(0);
				btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
				return;
			}

			if (btn.Background is SolidColorBrush scb)
			{
				var c = scb.Color;
				if (c.A == 0) return;

				int maxC = Math.Max(c.R, Math.Max(c.G, c.B));
				int minC = Math.Min(c.R, Math.Min(c.G, c.B));
				bool isNeutralLight = minC >= 215 && (maxC - minC) <= 25;
				bool isSoftIndigo = c.B >= 235 && c.R >= 215 && (c.B - c.R) >= 10;

				if (isSoftIndigo)
				{
					AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: false);
				}
				else if (isNeutralLight)
				{
					AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: false);
				}
			}
		}
		else if (node is ComboBox combo)
		{
			AttachInteractiveLiquidGlass(combo, LiquidGlassVariant.Clear, isDark, enablePointerTracking: false);
		}
		else if (node is TextBlock tb)
		{
			UpgradeTextBlockColors(tb, isDark);
		}
		else if (node is FontIcon fi)
		{
			UpgradeFontIconColors(fi, isDark);
		}
	}

	private static void UpgradeTextBlockColors(TextBlock tb, bool isDark)
	{
		if (tb.Foreground is SolidColorBrush scb)
		{
			var c = scb.Color;
			if (isDark)
			{
				// 极暗/主标题类 (如 #0F172A, #1E293B, #000000) -> #F8FAFC 或 #E2E8F0
				if (c.R <= 40 && c.G <= 50 && c.B <= 70)
				{
					if (tb.FontSize >= 14 || tb.FontWeight.Weight >= 600)
					{
						tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
					}
					else
					{
						tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
					}
				}
				// 次级暗色 (如 #334155) -> #E2E8F0
				else if (c.R <= 65 && c.G <= 75 && c.B <= 95)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
				}
				// 中度暗色 (如 #475569) -> #CBD5E1
				else if (c.R <= 90 && c.G <= 105 && c.B <= 125)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225));
				}
				// 浅灰暗色 (如 #64748B) -> #94A3B8
				else if (c.R <= 130 && c.G <= 145 && c.B <= 165)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
				}
			}
			else
			{
				if (c.R == 248 && c.G == 250 && c.B == 252)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42));
				}
				else if (c.R == 226 && c.G == 232 && c.B == 240)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85));
				}
				else if (c.R == 203 && c.G == 213 && c.B == 225)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
				}
				else if (c.R == 148 && c.G == 163 && c.B == 184)
				{
					tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
				}
			}
		}
	}

	private static void UpgradeFontIconColors(FontIcon fi, bool isDark)
	{
		if (fi.Foreground is SolidColorBrush scb)
		{
			var c = scb.Color;
			if (isDark)
			{
				if (c.R <= 40 && c.G <= 50 && c.B <= 70)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
				}
				else if (c.R <= 65 && c.G <= 75 && c.B <= 95)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
				}
				else if (c.R <= 90 && c.G <= 105 && c.B <= 125)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225));
				}
				else if (c.R <= 130 && c.G <= 145 && c.B <= 165)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
				}
			}
			else
			{
				if (c.R == 248 && c.G == 250 && c.B == 252)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42));
				}
				else if (c.R == 226 && c.G == 232 && c.B == 240)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85));
				}
				else if (c.R == 203 && c.G == 213 && c.B == 225)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
				}
				else if (c.R == 148 && c.G == 163 && c.B == 184)
				{
					fi.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
				}
			}
		}
	}

	/// <summary>
	/// 创建基于 SkiaSharp 硬件加速的 Apple Liquid Glass 全景环境流体焦散光场画布 (Ambient Caustic Light Field)。
	/// </summary>
	public static SKXamlCanvas CreateAmbientCausticCanvas(Func<bool>? isDarkProvider = null, bool enablePointLights = true)
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
			RenderAmbientCausticStage(e.Surface.Canvas, e.Info.Width, e.Info.Height, isDark, 0.5f, 0.25f, enablePointLights);
		};

		return canvas;
	}

	/// <summary>
	/// 在指定 Skia 画布上绘制 Apple Liquid Glass &amp; ECHO 环境流体焦散光场与顶栏透镜高光。
	/// 关键修复：在亚克力/液态玻璃/氛围模式下，绝不绘制 96% 不透明度的遮罩，而是根据 AcrylicOpacityScale 动态调节可读薄纱遮罩 (alpha 55..235)，
	/// 让系统 DesktopAcrylicBackdrop 桌面高斯模糊与壁纸完美透出，同时确保文字与按钮高对比度清晰可读！
	/// </summary>
	public static void RenderAmbientCausticStage(
		SKCanvas skCanvas,
		int width,
		int height,
		bool isDark,
		float pointerNormX = 0.5f,
		float pointerNormY = 0.25f,
		bool enablePointLights = true)
	{
		skCanvas.Clear(SKColors.Transparent);
		if (width <= 4 || height <= 4) return;

		float w = width;
		float h = height;

		var preset = ActivePreset ?? BuiltInPresets[0];
		Color topC = TransformActiveColor(preset.StageTopColor);
		Color midC = TransformActiveColor(preset.StageMidColor);
		Color botC = TransformActiveColor(preset.StageBottomColor);
		Color priC = TransformActiveColor(preset.AccentPrimary);
		Color secC = TransformActiveColor(preset.AccentSecondary);

		bool isTranslucentMode = ActiveThemeMode == "acrylic" || ActiveThemeMode == "liquid-glass" || ActiveThemeMode == "ambient" || ActiveThemeMode == "system";
		double translucency = Math.Clamp(AcrylicOpacityScale, 0.0, 1.0);
		if (isTranslucentMode && !KeepTranslucentWhenUnfocused && !IsWindowFocused)
		{
			translucency = 0.12; // 失焦且未开启“失焦时保持通透”时，自动收敛为高遮罩可读态
		}

		// 计算底色薄纱遮罩 Alpha：
		// 当处于亚克力/液态玻璃且 100% 通透度时，veilAlpha 约为 48..65 (浅色) / 60..78 (深色)，既让后方桌面亚克力/壁纸清澈透出，又保有温润的色彩氛围与文字对比度；
		// 当拖动透明度滑块从 100% -> 0% 时，veilAlpha 平滑过渡到 238 (接近纯色背景)。
		byte veilAlphaTop, veilAlphaMid, veilAlphaBot;
		if (isTranslucentMode)
		{
			int minVeil = isDark ? 62 : 48;
			int maxVeil = isDark ? 236 : 242;
			int computed = (int)Math.Round(maxVeil - translucency * (maxVeil - minVeil));
			veilAlphaTop = (byte)Math.Clamp(computed + 6, 38, 248);
			veilAlphaMid = (byte)Math.Clamp(computed - 6, 34, 245);
			veilAlphaBot = (byte)Math.Clamp(computed + 8, 42, 248);
		}
		else
		{
			// 非通透模式也响应微调（80%..100% 遮罩）
			int baseVeil = isDark ? 228 : 238;
			int computed = (int)Math.Round(baseVeil - (translucency * 45));
			veilAlphaTop = (byte)Math.Clamp(computed + 4, 160, 248);
			veilAlphaMid = (byte)Math.Clamp(computed - 4, 150, 245);
			veilAlphaBot = (byte)Math.Clamp(computed + 6, 165, 248);
		}

		if (!isDark)
		{
			Color sTop = preset.IsDark ? Color.FromArgb(255, 240, 246, 255) : topC;
			Color sMid = preset.IsDark ? Color.FromArgb(255, 232, 240, 255) : midC;
			Color sBot = preset.IsDark ? Color.FromArgb(255, 226, 236, 255) : botC;

			using (var baseShader = SKShader.CreateLinearGradient(
				new SKPoint(0, 0),
				new SKPoint(w, h),
				new SKColor[]
				{
					new SKColor(sTop.R, sTop.G, sTop.B, veilAlphaTop),
					new SKColor(sMid.R, sMid.G, sMid.B, veilAlphaMid),
					new SKColor(sBot.R, sBot.G, sBot.B, veilAlphaBot)
				},
				new float[] { 0f, 0.52f, 1f },
				SKShaderTileMode.Clamp))
			using (var basePaint = new SKPaint { Shader = baseShader, IsAntialias = true })
			{
				skCanvas.DrawRect(0, 0, w, h, basePaint);
			}

			if (enablePointLights)
			{
				// 流体焦散折射光球 A (主色调左上 -> 中部色散 Lensing Lobe)
				DrawCausticLobe(
					skCanvas,
					w * (0.22f + (pointerNormX - 0.5f) * 0.06f),
					h * (0.28f + (pointerNormY - 0.5f) * 0.06f),
					Math.Max(w, h) * 0.52f,
					new SKColor(priC.R, priC.G, priC.B, 76),
					new SKColor(sMid.R, sMid.G, sMid.B, 42),
					SKColors.Transparent);

				// 流体焦散折射光球 B (次强调色右侧焦散光带)
				DrawCausticLobe(
					skCanvas,
					w * (0.82f - (pointerNormX - 0.5f) * 0.05f),
					h * (0.62f - (pointerNormY - 0.5f) * 0.05f),
					Math.Max(w, h) * 0.48f,
					new SKColor(secC.R, secC.G, secC.B, 68),
					new SKColor(priC.R, priC.G, priC.B, 32),
					SKColors.Transparent);

				// 流体焦散折射光球 C (左下柔光珍珠白高光核)
				DrawCausticLobe(
					skCanvas,
					w * 0.38f,
					h * 0.78f,
					Math.Max(w, h) * 0.42f,
					new SKColor(255, 255, 255, 135),
					new SKColor(sTop.R, sTop.G, sTop.B, 60),
					SKColors.Transparent);
			}
		}
		else
		{
			Color dTop = preset.IsDark ? topC : Color.FromArgb(255, 13, 18, 30);
			Color dMid = preset.IsDark ? midC : Color.FromArgb(255, 18, 24, 38);
			Color dBot = preset.IsDark ? botC : Color.FromArgb(255, 12, 16, 28);

			using (var baseShader = SKShader.CreateLinearGradient(
				new SKPoint(0, 0),
				new SKPoint(w, h),
				new SKColor[]
				{
					new SKColor(dTop.R, dTop.G, dTop.B, veilAlphaTop),
					new SKColor(dMid.R, dMid.G, dMid.B, veilAlphaMid),
					new SKColor(dBot.R, dBot.G, dBot.B, veilAlphaBot)
				},
				new float[] { 0f, 0.5f, 1f },
				SKShaderTileMode.Clamp))
			using (var basePaint = new SKPaint { Shader = baseShader, IsAntialias = true })
			{
				skCanvas.DrawRect(0, 0, w, h, basePaint);
			}

			if (enablePointLights)
			{
				// 深海流体焦散光球 A
				DrawCausticLobe(
					skCanvas,
					w * (0.20f + (pointerNormX - 0.5f) * 0.08f),
					h * (0.24f + (pointerNormY - 0.5f) * 0.08f),
					Math.Max(w, h) * 0.50f,
					new SKColor(priC.R, priC.G, priC.B, 52),
					new SKColor(secC.R, secC.G, secC.B, 26),
					SKColors.Transparent);
	
				// 深海流体焦散光球 B
				DrawCausticLobe(
					skCanvas,
					w * (0.80f - (pointerNormX - 0.5f) * 0.06f),
					h * (0.68f - (pointerNormY - 0.5f) * 0.06f),
					Math.Max(w, h) * 0.48f,
					new SKColor(secC.R, secC.G, secC.B, 48),
					new SKColor(priC.R, priC.G, priC.B, 24),
					SKColors.Transparent);
	
				// 指针跟随微光透镜
				DrawCausticLobe(
					skCanvas,
					w * pointerNormX,
					h * pointerNormY,
					Math.Min(w, h) * 0.32f,
					new SKColor(priC.R, priC.G, priC.B, 28),
					new SKColor(secC.R, secC.G, secC.B, 12),
					SKColors.Transparent);
			}
		}

		// 顶部边缘曲面折射高光丝带 (Top Specular Lensing Ribbon)
		using (var topRibbonShader = SKShader.CreateLinearGradient(
			new SKPoint(0, 0),
			new SKPoint(w, 0),
			new SKColor[]
			{
				new SKColor(255, 255, 255, 0),
				new SKColor(255, 255, 255, isDark ? (byte)95 : (byte)215),
				new SKColor(priC.R, priC.G, priC.B, isDark ? (byte)115 : (byte)220),
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
