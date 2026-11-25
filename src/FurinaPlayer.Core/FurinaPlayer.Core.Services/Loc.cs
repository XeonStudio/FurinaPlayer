using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FurinaPlayer.Core.Services;

public sealed record LanguageOption(string Code, string NativeName);

/// <summary>
/// FurinaPlayer 全局多语言本地化引擎（支持 zh-CN / zh-TW / en-US / fr-FR / ru-RU / es-ES / ja-JP / ko-KR）。
/// </summary>
public static partial class Loc
{
	public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } = new[]
	{
		new LanguageOption("zh-CN", "简体中文"),
		new LanguageOption("zh-TW", "繁體中文"),
		new LanguageOption("en-US", "English"),
		new LanguageOption("fr-FR", "Français"),
		new LanguageOption("ru-RU", "Русский"),
		new LanguageOption("es-ES", "Español"),
		new LanguageOption("ja-JP", "日本語"),
		new LanguageOption("ko-KR", "한국어")
	};

	private static readonly ConcurrentDictionary<string, string[]> _entries = new(StringComparer.Ordinal);
	private static readonly object _initLock = new();
	private static bool _initialized;
	private static string _currentLanguage = "zh-CN";

	public static event Action<string>? LanguageChanged;

	public static string CurrentLanguage
	{
		get => _currentLanguage;
		set
		{
			string norm = NormalizeLanguage(value);
			if (!string.Equals(_currentLanguage, norm, StringComparison.Ordinal))
			{
				_currentLanguage = norm;
				LanguageChanged?.Invoke(norm);
			}
		}
	}

	public static void SetLanguageSilently(string? lang)
	{
		_currentLanguage = NormalizeLanguage(lang);
	}

	public static string NormalizeLanguage(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang)) return "zh-CN";
		string s = lang.Trim().ToLowerInvariant();
		if (s.StartsWith("zh-tw") || s.StartsWith("zh-hk") || s.StartsWith("zh-hant")) return "zh-TW";
		if (s.StartsWith("zh")) return "zh-CN";
		if (s.StartsWith("en")) return "en-US";
		if (s.StartsWith("fr")) return "fr-FR";
		if (s.StartsWith("ru")) return "ru-RU";
		if (s.StartsWith("es")) return "es-ES";
		if (s.StartsWith("ja")) return "ja-JP";
		if (s.StartsWith("ko")) return "ko-KR";
		return "zh-CN";
	}

	private static int LanguageIndex(string lang) => lang switch
	{
		"zh-TW" => 0,
		"en-US" => 1,
		"fr-FR" => 2,
		"ru-RU" => 3,
		"es-ES" => 4,
		"ja-JP" => 5,
		"ko-KR" => 6,
		_ => -1
	};

	private static void EnsureInitialized()
	{
		if (_initialized) return;
		lock (_initLock)
		{
			if (_initialized) return;
			RegisterShell();
			RegisterHome();
			RegisterStreaming();
			RegisterConverter();
			RegisterEditor();
			RegisterDspCore();
			RegisterDspModulesA();
			RegisterDspModulesB();
			RegisterSettings();
			RegisterCheckRepair();
			RegisterControls();
			RegisterXbfStatic();
			_initialized = true;
		}
	}

	internal static void Add(string zhCn, string zhTw, string en, string fr, string ru, string es, string ja, string ko)
	{
		if (string.IsNullOrEmpty(zhCn)) return;
		_entries[zhCn] = new[] { zhTw, en, fr, ru, es, ja, ko };
	}

	public static string T(string zhCnText)
	{
		if (string.IsNullOrEmpty(zhCnText)) return zhCnText;
		string lang = _currentLanguage;
		if (lang == "zh-CN") return zhCnText;
		EnsureInitialized();
		int idx = LanguageIndex(lang);
		if (idx < 0) return zhCnText;
		if (_entries.TryGetValue(zhCnText, out string[]? arr) && idx < arr.Length && !string.IsNullOrEmpty(arr[idx]))
		{
			return arr[idx];
		}
		return zhCnText;
	}

	public static string F(string zhCnFormat, params object?[] args)
	{
		return string.Format(T(zhCnFormat), args);
	}

	static partial void RegisterShell();
	static partial void RegisterHome();
	static partial void RegisterStreaming();
	static partial void RegisterConverter();
	static partial void RegisterEditor();
	static partial void RegisterDspCore();
	static partial void RegisterDspModulesA();
	static partial void RegisterDspModulesB();
	static partial void RegisterSettings();
	static partial void RegisterCheckRepair();
	static partial void RegisterControls();
	static partial void RegisterXbfStatic();
}
