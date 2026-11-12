using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicWave.Core.Services;

/// <summary>
/// QQ 音乐 Web 扫码与 OAuth2.0 登录辅助类。
/// 集中维护 QQ 互联（client_id=100497308）与微信开放平台（appid=wx48db31d50e334801）登录地址、
/// y.qq.com wx_redirect.html 回调识别以及 QQ/微信登录态 Cookie 解析逻辑。
/// </summary>
public static class QQMusicAuthHelper
{
	public const string QQConnectAppId = "100497308";
	public const string WeChatOpenAppId = "wx48db31d50e334801";
	public const string RedirectCallbackPrefix = "https://y.qq.com/portal/wx_redirect.html";

	/// <summary>
	/// QQ 扫码 / QQ 桌面快捷登录官方 OAuth2.0 入口。
	/// 授权完成后跳转至 y.qq.com/portal/wx_redirect.html?login_type=1，由官方回调脚本兑换 qqmusic_key/qm_keyst。
	/// </summary>
	public const string QQLoginAuthorizeUrl =
		"https://graph.qq.com/oauth2.0/authorize?response_type=code&client_id=100497308" +
		"&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D1%26surl%3Dhttps%253A%252F%252Fy.qq.com%252F" +
		"&state=state&display=pc";

	/// <summary>
	/// 微信扫码 / 微信桌面快捷登录官方开放平台入口。
	/// 授权完成后跳转至 y.qq.com/portal/wx_redirect.html?login_type=2，由官方回调脚本兑换 qqmusic_key/qm_keyst。
	/// </summary>
	public const string WeChatLoginQrUrl =
		"https://open.weixin.qq.com/connect/qrconnect?appid=wx48db31d50e334801" +
		"&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D2%26surl%3Dhttps%253A%252F%252Fy.qq.com%252F" +
		"&response_type=code&scope=snsapi_login&state=STATE&self_redirect=true#wechat_redirect";

	/// <summary>
	/// QQ 互联登录页（graph.qq.com/oauth2.0/show）在窄容器（如 460px 弹窗）中的自适应脚本。
	/// 触发其内置 .vertical 布局并隐藏顶部导航条，避免 688px 宽双栏布局出现横向裁切。
	/// </summary>
	public const string CompactOAuthLayoutScript =
		"(function(){" +
		"var c=document.getElementById('combine_page'),t=document.getElementById('lay_top');" +
		"if(c){c.classList.add('vertical');c.style.width='100%';}" +
		"if(t){t.classList.add('hide');}" +
		"})();";

	/// <summary>
	/// WebView2 CookieManager 需要读取与清理的 QQ 音乐及相关认证域名列表。
	/// </summary>
	public static readonly IReadOnlyList<string> CookieOrigins =
	[
		"https://y.qq.com",
		"https://u.y.qq.com",
		"https://c.y.qq.com",
		"https://qq.com",
		"https://graph.qq.com",
		"https://xui.ptlogin2.qq.com"
	];

	private static readonly string[] NumericUinKeys = ["uin", "p_uin", "o_cookie", "wxuin", "musicid"];
	private static readonly string[] FallbackIdKeys = ["euin", "wxunionid", "wxopenid"];
	private static readonly string[] PrimaryMusicAuthKeys = ["qqmusic_key", "qm_keyst", "musickey"];
	private static readonly string[] SecondaryQQAuthKeys = ["pskey", "p_skey", "skey"];

	public static Dictionary<string, string> ParseCookieString(string? cookieInput)
	{
		var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(cookieInput)) return dict;

		foreach (string part in cookieInput.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int eqIdx = part.IndexOf('=');
			if (eqIdx <= 0) continue;

			string name = part[..eqIdx].Trim();
			string value = part[(eqIdx + 1)..].Trim();
			if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
			{
				dict[name] = value;
			}
		}

		return dict;
	}

	/// <summary>
	/// 从 Cookie 字典中提取纯数字 QQ/微信音乐 UIN（兼容 o123456789 前缀与 123456789 纯数字格式）。
	/// </summary>
	public static string ExtractNumericUin(IReadOnlyDictionary<string, string> cookies)
	{
		foreach (string key in NumericUinKeys)
		{
			if (!cookies.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
				continue;

			Match m = Regex.Match(raw.Trim(), @"^[oO]?0*(\d+)$");
			if (m.Success)
			{
				return m.Groups[1].Value;
			}

			Match digitsMatch = Regex.Match(raw, @"\d+");
			if (digitsMatch.Success)
			{
				string trimmed = digitsMatch.Value.TrimStart('0');
				if (!string.IsNullOrEmpty(trimmed)) return trimmed;
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// 提取用户标识（优先返回纯数字 UIN，微信登录若无纯数字 UIN 则回退到 euin / wxunionid / wxopenid）。
	/// </summary>
	public static string ExtractUserId(IReadOnlyDictionary<string, string> cookies)
	{
		string numeric = ExtractNumericUin(cookies);
		if (!string.IsNullOrEmpty(numeric)) return numeric;

		foreach (string key in FallbackIdKeys)
		{
			if (cookies.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val))
			{
				return val.Trim();
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// 判断手动粘贴的 Cookie 字符串是否包含有效 QQ 音乐凭据（至少包含核心密钥或有效 UIN）。
	/// </summary>
	public static bool HasValidManualCookie(string? cookieInput)
	{
		var cookies = ParseCookieString(cookieInput);
		if (cookies.Count == 0) return false;

		bool hasAuth = PrimaryMusicAuthKeys.Any(cookies.ContainsKey) || SecondaryQQAuthKeys.Any(cookies.ContainsKey);
		string userId = ExtractUserId(cookies);
		return hasAuth || (!string.IsNullOrEmpty(userId) && userId.Length >= 5);
	}

	/// <summary>
	/// 从 WebView2 拦截到的 Cookie 字典中判定是否已完成 y.qq.com 最终登录态下发，
	/// 避免在 ptlogin2 中间态仅凭 graph.qq.com 的 p_skey/skey 提前截断而漏掉 qqmusic_key/qm_keyst。
	/// </summary>
	public static bool TryBuildAuthenticatedCookieHeader(
		IReadOnlyDictionary<string, string> cookies,
		string? currentWebViewUrl,
		out string userId,
		out string cookieHeader)
	{
		userId = string.Empty;
		cookieHeader = string.Empty;

		if (cookies.Count == 0) return false;

		bool hasPrimaryMusicKey = PrimaryMusicAuthKeys.Any(cookies.ContainsKey);
		bool hasLandedOnYqqHome = IsLandedOnYqqPortal(currentWebViewUrl);
		bool hasSecondaryKey = SecondaryQQAuthKeys.Any(cookies.ContainsKey);

		if (!hasPrimaryMusicKey && !(hasLandedOnYqqHome && hasSecondaryKey))
		{
			return false;
		}

		string numericUin = ExtractNumericUin(cookies);
		userId = !string.IsNullOrEmpty(numericUin) ? numericUin : ExtractUserId(cookies);

		bool hasValidNumeric = !string.IsNullOrEmpty(numericUin) && numericUin.Length >= 5;
		bool hasValidFallbackId = string.IsNullOrEmpty(numericUin) && !string.IsNullOrEmpty(userId);
		if (!hasValidNumeric && !hasValidFallbackId)
		{
			return false;
		}

		var normalized = new Dictionary<string, string>(cookies, StringComparer.OrdinalIgnoreCase);
		if (!normalized.ContainsKey("uin") && hasValidNumeric)
		{
			normalized["uin"] = "o" + numericUin;
		}

		cookieHeader = string.Join("; ", normalized.Select(kv => $"{kv.Key}={kv.Value}"));
		return true;
	}

	private static bool IsLandedOnYqqPortal(string? currentWebViewUrl)
	{
		if (string.IsNullOrWhiteSpace(currentWebViewUrl)) return false;
		if (!Uri.TryCreate(currentWebViewUrl, UriKind.Absolute, out var uri)) return false;

		bool isYqqHost = uri.Host.Equals("y.qq.com", StringComparison.OrdinalIgnoreCase);
		bool isStillExchangingCode = currentWebViewUrl.StartsWith(RedirectCallbackPrefix, StringComparison.OrdinalIgnoreCase);
		return isYqqHost && !isStillExchangingCode;
	}
}
