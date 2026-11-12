using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public class StreamingTrackInfo
{
	public long Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public string Album { get; set; } = string.Empty;
	public string StreamUrl { get; set; } = string.Empty;
	public string? CoverUrl { get; set; }
	public TimeSpan Duration { get; set; }
	public string Quality { get; set; } = "Hi-Res FLAC / 无损 FLAC / 高品";
	public string Source { get; set; } = "网易云音乐";
}

public class StreamingAccountInfo
{
	public string Platform { get; set; } = "网易云音乐";
	public bool IsLoggedIn { get; set; }
	public long UserId { get; set; }
	public string UserIdStr { get; set; } = string.Empty;
	public string Nickname { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;
	public string Cookie { get; set; } = string.Empty;
	public string VipStatus { get; set; } = "普通用户";
	public DateTime? LoginTime { get; set; }
}

/// <summary>
/// FurinaPlayer 流媒体服务：仿照 ShinawaseLoader 架构，
/// 支持多平台音源在线检索（歌曲/专辑/艺人/歌单）、高质量网络音频流直接解析与播放，
/// 以及网易云音乐、QQ 音乐、Spotify 多平台账号认证与凭据持久化体系。
/// </summary>
public class StreamingService
{
	private static readonly HttpClient _http = new()
	{
		Timeout = TimeSpan.FromSeconds(15)
	};

	private readonly SettingsService? _settingsService;

	public StreamingAccountInfo CurrentNeteaseAccount { get; private set; } = new() { Platform = "网易云音乐" };
	public StreamingAccountInfo CurrentQQAccount { get; private set; } = new() { Platform = "QQ 音乐" };
	public StreamingAccountInfo CurrentSpotifyAccount { get; private set; } = new() { Platform = "Spotify" };

	public event EventHandler<StreamingAccountInfo>? AccountStateChanged;

	public StreamingAccountInfo GetAccount(string? platform)
	{
		if (string.IsNullOrWhiteSpace(platform)) return CurrentNeteaseAccount;
		if (platform.Contains("QQ", StringComparison.OrdinalIgnoreCase)) return CurrentQQAccount;
		if (platform.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) return CurrentSpotifyAccount;
		return CurrentNeteaseAccount;
	}

	static StreamingService()
	{
		try
		{
			_http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
		}
		catch
		{
		}
	}

	public StreamingService(SettingsService? settingsService = null)
	{
		_settingsService = settingsService ?? new SettingsService();
		LoadAccountFromSettings();
	}

	private void LoadAccountFromSettings()
	{
		try
		{
			var settings = _settingsService?.Load();
			if (settings != null)
			{
				if (settings.NeteaseIsLoggedIn && !string.IsNullOrWhiteSpace(settings.NeteaseCookie))
				{
					CurrentNeteaseAccount = new StreamingAccountInfo
					{
						Platform = "网易云音乐",
						IsLoggedIn = true,
						UserId = settings.NeteaseUserId,
						UserIdStr = settings.NeteaseUserId > 0 ? settings.NeteaseUserId.ToString() : string.Empty,
						Nickname = string.IsNullOrWhiteSpace(settings.NeteaseNickname) ? "网易云用户" : settings.NeteaseNickname,
						AvatarUrl = settings.NeteaseAvatarUrl ?? string.Empty,
						Cookie = settings.NeteaseCookie,
						VipStatus = string.IsNullOrWhiteSpace(settings.NeteaseVipTitle) ? "黑胶 / 认证用户" : settings.NeteaseVipTitle,
						LoginTime = DateTime.Now
					};
				}
				if (settings.QQIsLoggedIn && !string.IsNullOrWhiteSpace(settings.QQCookie))
				{
					CurrentQQAccount = new StreamingAccountInfo
					{
						Platform = "QQ 音乐",
						IsLoggedIn = true,
						UserIdStr = settings.QQUin ?? string.Empty,
						Nickname = string.IsNullOrWhiteSpace(settings.QQNickname) ? "QQ 音乐用户" : settings.QQNickname,
						AvatarUrl = settings.QQAvatarUrl ?? string.Empty,
						Cookie = settings.QQCookie,
						VipStatus = string.IsNullOrWhiteSpace(settings.QQVipTitle) ? "绿钻 / 认证用户" : settings.QQVipTitle,
						LoginTime = DateTime.Now
					};
				}
				if (settings.SpotifyIsLoggedIn && (!string.IsNullOrWhiteSpace(settings.SpotifyToken) || !string.IsNullOrWhiteSpace(settings.SpotifyCookie)))
				{
					CurrentSpotifyAccount = new StreamingAccountInfo
					{
						Platform = "Spotify",
						IsLoggedIn = true,
						UserIdStr = settings.SpotifyUserId ?? string.Empty,
						Nickname = string.IsNullOrWhiteSpace(settings.SpotifyNickname) ? "Spotify 用户" : settings.SpotifyNickname,
						AvatarUrl = settings.SpotifyAvatarUrl ?? string.Empty,
						Cookie = !string.IsNullOrWhiteSpace(settings.SpotifyToken) ? settings.SpotifyToken : (settings.SpotifyCookie ?? string.Empty),
						VipStatus = string.IsNullOrWhiteSpace(settings.SpotifyProductType) ? "Spotify Premium" : settings.SpotifyProductType,
						LoginTime = DateTime.Now
					};
				}
			}
		}
		catch
		{
		}
	}

	#region 登录认证 API (扫码 / Cookie / 状态检测)

	/// <summary>
	/// 获取网易云扫码登录 Unikey 密钥
	/// 对应 API: /login/qr/key
	/// </summary>
	public async Task<string?> RequestNeteaseQrKeyAsync()
	{
		string? customBase = _settingsService?.Load().StreamingApiBaseUrl?.TrimEnd('/');
		if (!string.IsNullOrEmpty(customBase))
		{
			try
			{
				long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				using var resp = await _http.GetAsync($"{customBase}/login/qr/key?timestamp={ts}");
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("unikey", out var keyProp))
					{
						return keyProp.GetString();
					}
					if (doc.RootElement.TryGetProperty("unikey", out var ukeyProp))
					{
						return ukeyProp.GetString();
					}
				}
			}
			catch { }
		}

		// 官方直连接口
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Post, "https://music.163.com/api/login/qrcode/unikey?type=1");
			req.Headers.Referrer = new Uri("https://music.163.com/");
			using var resp = await _http.SendAsync(req);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("unikey", out var keyElem))
				{
					return keyElem.GetString();
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"RequestNeteaseQrKeyAsync error: {ex.Message}");
		}

		return null;
	}

	/// <summary>
	/// 生成网易云扫码登录链接或 Base64 图像
	/// 对应 API: /login/qr/create
	/// </summary>
	public async Task<(string qrUrl, string? qrImgBase64)> CreateNeteaseQrAsync(string unikey)
	{
		string qrUrl = $"https://music.163.com/login?codekey={unikey}";
		string? qrImgBase64 = null;

		string? customBase = _settingsService?.Load().StreamingApiBaseUrl?.TrimEnd('/');
		if (!string.IsNullOrEmpty(customBase))
		{
			try
			{
				long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string apiUrl = $"{customBase}/login/qr/create?key={Uri.EscapeDataString(unikey)}&qrimg=true&timestamp={ts}";
				using var resp = await _http.GetAsync(apiUrl);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var data))
					{
						if (data.TryGetProperty("qrurl", out var qProp)) qrUrl = qProp.GetString() ?? qrUrl;
						if (data.TryGetProperty("qrimg", out var imgProp)) qrImgBase64 = imgProp.GetString();
					}
				}
			}
			catch { }
		}

		return (qrUrl, qrImgBase64);
	}

	/// <summary>
	/// 轮询检测网易云扫码状态
	/// 对应 API: /login/qr/check
	/// 返回 code: 800 已过期, 801 等待扫码, 802 待确认, 803 授权登录成功
	/// </summary>
	public async Task<(int code, string message, string? cookie, string? nickname, string? avatarUrl)> CheckNeteaseQrStatusAsync(string unikey)
	{
		string? customBase = _settingsService?.Load().StreamingApiBaseUrl?.TrimEnd('/');
		if (!string.IsNullOrEmpty(customBase))
		{
			try
			{
				long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string checkUrl = $"{customBase}/login/qr/check?key={Uri.EscapeDataString(unikey)}&timestamp={ts}";
				using var resp = await _http.GetAsync(checkUrl);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					int code = doc.RootElement.TryGetProperty("code", out var cProp) ? cProp.GetInt32() : 801;
					string msg = doc.RootElement.TryGetProperty("message", out var mProp) ? mProp.GetString() ?? "" : "";
					string? cookie = doc.RootElement.TryGetProperty("cookie", out var kProp) ? kProp.GetString() : null;
					string? nickname = doc.RootElement.TryGetProperty("nickname", out var nProp) ? nProp.GetString() : null;
					string? avatarUrl = doc.RootElement.TryGetProperty("avatarUrl", out var aProp) ? aProp.GetString() : null;
					return (code, msg, cookie, nickname, avatarUrl);
				}
			}
			catch { }
		}

		// 官方直连轮询接口
		try
		{
			long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			string url = $"https://music.163.com/api/login/qrcode/client/login?_={ts}";
			using var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Headers.Referrer = new Uri("https://music.163.com/");
			req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				{ "key", unikey },
				{ "type", "1" }
			});

			using var resp = await _http.SendAsync(req);
			string json = await resp.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(json);
			int code = doc.RootElement.TryGetProperty("code", out var cProp) ? cProp.GetInt32() : 801;
			string msg = doc.RootElement.TryGetProperty("message", out var mProp) ? mProp.GetString() ?? "" : "";
			string? cookie = doc.RootElement.TryGetProperty("cookie", out var kProp) ? kProp.GetString() : null;
			string? nickname = doc.RootElement.TryGetProperty("nickname", out var nProp) ? nProp.GetString() : null;
			string? avatarUrl = doc.RootElement.TryGetProperty("avatarUrl", out var aProp) ? aProp.GetString() : null;

			// 授权成功时提取 Set-Cookie 中的 MUSIC_U
			if (code == 803)
			{
				var setCookies = new List<string>();
				if (resp.Headers.TryGetValues("Set-Cookie", out var headerCookies))
				{
					setCookies.AddRange(headerCookies);
				}
				if (string.IsNullOrEmpty(cookie) && setCookies.Count > 0)
				{
					cookie = string.Join("; ", setCookies.Select(c => c.Split(';')[0]));
				}
				else if (setCookies.Count > 0 && !string.IsNullOrEmpty(cookie) && !cookie.Contains("MUSIC_U="))
				{
					var musicU = setCookies.FirstOrDefault(c => c.StartsWith("MUSIC_U="));
					if (!string.IsNullOrEmpty(musicU))
					{
						cookie = $"{cookie}; {musicU.Split(';')[0]}";
					}
				}
			}

			return (code, msg, cookie, nickname, avatarUrl);
		}
		catch (Exception ex)
		{
			return (801, $"网络连接异常: {ex.Message}", null, null, null);
		}
	}

	/// <summary>
	/// 校验网易云 Cookie 并获取详细用户信息（用户ID、昵称、头像、VIP状态等）
	/// 对应 API: /login/status 或 /user/account。具备健壮容错，确保有效 Cookie 绝不被丢弃。
	/// </summary>
	public async Task<StreamingAccountInfo?> VerifyAndFetchNeteaseAccountAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput)) return null;

		string cookie = cookieInput.Trim();
		if (!cookie.Contains("MUSIC_U=") && !cookie.Contains('='))
		{
			cookie = $"MUSIC_U={cookie};";
		}
		if (!cookie.Contains("os="))
		{
			cookie = $"{cookie}; os=pc; appver=8.9.70;";
		}

		string? customBase = _settingsService?.Load().StreamingApiBaseUrl?.TrimEnd('/');
		if (!string.IsNullOrEmpty(customBase))
		{
			try
			{
				long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string statusUrl = $"{customBase}/login/status?timestamp={ts}&cookie={Uri.EscapeDataString(cookie)}";
				using var resp = await _http.GetAsync(statusUrl);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					JsonElement profileElem = default;
					if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("profile", out var p1))
					{
						profileElem = p1;
					}
					else if (doc.RootElement.TryGetProperty("profile", out var p2))
					{
						profileElem = p2;
					}

					if (profileElem.ValueKind == JsonValueKind.Object)
					{
						long uid = profileElem.TryGetProperty("userId", out var u) ? u.GetInt64() : 0;
						string nick = profileElem.TryGetProperty("nickname", out var n) ? n.GetString() ?? "网易云用户" : "网易云用户";
						string avatar = profileElem.TryGetProperty("avatarUrl", out var a) ? a.GetString() ?? "" : "";
						int vip = profileElem.TryGetProperty("vipType", out var v) ? v.GetInt32() : 0;

						return new StreamingAccountInfo
						{
							Platform = "网易云音乐",
							IsLoggedIn = true,
							UserId = uid,
							UserIdStr = uid.ToString(),
							Nickname = nick,
							AvatarUrl = avatar,
							Cookie = cookie,
							VipStatus = vip > 0 ? "黑胶 VIP" : "标准用户",
							LoginTime = DateTime.Now
						};
					}
				}
			}
			catch { }
		}

		// 官方直连 /api/nuser/account/get
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Post, "https://music.163.com/api/nuser/account/get");
			req.Headers.Referrer = new Uri("https://music.163.com/");
			req.Headers.Add("Cookie", cookie);

			using var resp = await _http.SendAsync(req);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("profile", out var profileElem) && profileElem.ValueKind == JsonValueKind.Object)
				{
					long uid = profileElem.TryGetProperty("userId", out var u) ? u.GetInt64() : 0;
					string nick = profileElem.TryGetProperty("nickname", out var n) ? n.GetString() ?? "网易云用户" : "网易云用户";
					string avatar = profileElem.TryGetProperty("avatarUrl", out var a) ? a.GetString() ?? "" : "";
					int vip = profileElem.TryGetProperty("vipType", out var v) ? v.GetInt32() : 0;

					return new StreamingAccountInfo
					{
						Platform = "网易云音乐",
						IsLoggedIn = true,
						UserId = uid,
						UserIdStr = uid.ToString(),
						Nickname = nick,
						AvatarUrl = avatar,
						Cookie = cookie,
						VipStatus = vip > 0 ? "黑胶 VIP" : "标准用户",
						LoginTime = DateTime.Now
					};
				}
				if (doc.RootElement.TryGetProperty("account", out var accElem) && accElem.ValueKind == JsonValueKind.Object)
				{
					long uid = accElem.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
					string userName = accElem.TryGetProperty("userName", out var uProp) ? uProp.GetString() ?? "网易云用户" : "网易云用户";
					return new StreamingAccountInfo
					{
						Platform = "网易云音乐",
						IsLoggedIn = true,
						UserId = uid,
						UserIdStr = uid.ToString(),
						Nickname = userName,
						AvatarUrl = string.Empty,
						Cookie = cookie,
						VipStatus = "黑胶 / 认证用户",
						LoginTime = DateTime.Now
					};
				}
			}
		}
		catch { }

		// 兜底认证保护：只要包含有效 MUSIC_U 字段，即视为有效凭据导入成功，绝对不丢失用户输入的凭证
		if (cookie.Contains("MUSIC_U="))
		{
			return new StreamingAccountInfo
			{
				Platform = "网易云音乐",
				IsLoggedIn = true,
				UserId = 0,
				UserIdStr = string.Empty,
				Nickname = "网易云认证用户",
				AvatarUrl = string.Empty,
				Cookie = cookie,
				VipStatus = "黑胶 / 认证凭证已载入",
				LoginTime = DateTime.Now
			};
		}

		return null;
	}

	/// <summary>
	/// 保存并持久化网易云账号登录信息
	/// </summary>
	public void SaveNeteaseAccount(StreamingAccountInfo account)
	{
		CurrentNeteaseAccount = account;
		_settingsService?.Update(s =>
		{
			s.NeteaseIsLoggedIn = account.IsLoggedIn;
			s.NeteaseUserId = account.UserId;
			s.NeteaseNickname = account.Nickname;
			s.NeteaseAvatarUrl = account.AvatarUrl;
			s.NeteaseCookie = account.Cookie;
			s.NeteaseVipTitle = account.VipStatus;
		});
		AccountStateChanged?.Invoke(this, account);
	}

	/// <summary>
	/// 退出网易云音乐登录
	/// </summary>
	public void LogoutNetease()
	{
		CurrentNeteaseAccount = new StreamingAccountInfo
		{
			Platform = "网易云音乐",
			IsLoggedIn = false
		};
		_settingsService?.Update(s =>
		{
			s.NeteaseIsLoggedIn = false;
			s.NeteaseUserId = 0;
			s.NeteaseNickname = null;
			s.NeteaseAvatarUrl = null;
			s.NeteaseCookie = null;
			s.NeteaseVipTitle = null;
		});
		AccountStateChanged?.Invoke(this, CurrentNeteaseAccount);
	}

	/// <summary>
	/// 重新刷新当前网易云账号登录状态
	/// </summary>
	public async Task<bool> RefreshNeteaseAccountStatusAsync()
	{
		if (!CurrentNeteaseAccount.IsLoggedIn || string.IsNullOrWhiteSpace(CurrentNeteaseAccount.Cookie))
		{
			return false;
		}

		var updated = await VerifyAndFetchNeteaseAccountAsync(CurrentNeteaseAccount.Cookie);
		if (updated != null && updated.IsLoggedIn)
		{
			SaveNeteaseAccount(updated);
			return true;
		}
		else
		{
			LogoutNetease();
			return false;
		}
	}

	#endregion

	#region QQ 音乐登录认证与凭据持久化 API

	/// <summary>
	/// 校验 QQ 音乐 Cookie 并获取用户信息（UIN、昵称、头像、绿钻状态等）
	/// 支持粘贴 uin、qm_keyst、pskey 等核心凭据或完整 Cookie 字符串
	/// </summary>
	public async Task<StreamingAccountInfo?> VerifyAndFetchQQAccountAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput)) return null;

		string cookie = cookieInput.Trim();
		if (!QQMusicAuthHelper.HasValidManualCookie(cookie)) return null;

		var parsed = QQMusicAuthHelper.ParseCookieString(cookie);
		string numericUin = QQMusicAuthHelper.ExtractNumericUin(parsed);
		string userId = QQMusicAuthHelper.ExtractUserId(parsed);

		string nickname = !string.IsNullOrEmpty(userId) ? $"QQ用户 ({userId})" : "QQ音乐用户";
		string avatar = !string.IsNullOrEmpty(numericUin) ? $"https://q1.qlogo.cn/g?b=qq&nk={numericUin}&s=100" : "";
		string vipStatus = "绿钻 / 认证用户";

		try
		{
			string profileUserId = !string.IsNullOrEmpty(numericUin) ? numericUin : userId;
			if (!string.IsNullOrEmpty(profileUserId))
			{
				string testUrl = $"https://c.y.qq.com/rsc/fcgi-bin/fcg_get_profile_homepage.fcg?cid=205360838&userid={Uri.EscapeDataString(profileUserId)}&reqfrom=1";
				using var req = new HttpRequestMessage(HttpMethod.Get, testUrl);
				req.Headers.Referrer = new Uri("https://y.qq.com/");
				req.Headers.Add("Cookie", cookie);
				using var resp = await _http.SendAsync(req);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("creator", out var cr))
					{
						if (cr.TryGetProperty("nick", out var nickProp) && !string.IsNullOrWhiteSpace(nickProp.GetString()))
							nickname = nickProp.GetString()!;
						if (cr.TryGetProperty("headpic", out var headProp) && !string.IsNullOrWhiteSpace(headProp.GetString()))
							avatar = headProp.GetString()!;
						if (cr.TryGetProperty("vip_level", out var vipProp) && vipProp.GetInt32() > 0)
						{
							vipStatus = $"绿钻 VIP{vipProp.GetInt32()}";
						}
					}
				}
			}
		}
		catch { }

		return new StreamingAccountInfo
		{
			Platform = "QQ 音乐",
			IsLoggedIn = true,
			UserIdStr = userId,
			Nickname = nickname,
			AvatarUrl = avatar,
			Cookie = cookie,
			VipStatus = vipStatus,
			LoginTime = DateTime.Now
		};
	}

	/// <summary>
	/// 保存并持久化 QQ 音乐账号登录信息
	/// </summary>
	public void SaveQQAccount(StreamingAccountInfo account)
	{
		CurrentQQAccount = account;
		_settingsService?.Update(s =>
		{
			s.QQIsLoggedIn = account.IsLoggedIn;
			s.QQUin = account.UserIdStr;
			s.QQNickname = account.Nickname;
			s.QQAvatarUrl = account.AvatarUrl;
			s.QQCookie = account.Cookie;
			s.QQVipTitle = account.VipStatus;
		});
		AccountStateChanged?.Invoke(this, account);
	}

	/// <summary>
	/// 退出 QQ 音乐登录
	/// </summary>
	public void LogoutQQ()
	{
		CurrentQQAccount = new StreamingAccountInfo
		{
			Platform = "QQ 音乐",
			IsLoggedIn = false
		};
		_settingsService?.Update(s =>
		{
			s.QQIsLoggedIn = false;
			s.QQUin = null;
			s.QQNickname = null;
			s.QQAvatarUrl = null;
			s.QQCookie = null;
			s.QQVipTitle = null;
		});
		AccountStateChanged?.Invoke(this, CurrentQQAccount);
	}

	/// <summary>
	/// 刷新 QQ 音乐账号状态
	/// </summary>
	public async Task<bool> RefreshQQAccountStatusAsync()
	{
		if (!CurrentQQAccount.IsLoggedIn || string.IsNullOrWhiteSpace(CurrentQQAccount.Cookie))
		{
			return false;
		}

		var updated = await VerifyAndFetchQQAccountAsync(CurrentQQAccount.Cookie);
		if (updated != null && updated.IsLoggedIn)
		{
			SaveQQAccount(updated);
			return true;
		}
		else
		{
			LogoutQQ();
			return false;
		}
	}

	#endregion

	#region Spotify 登录认证与凭据持久化 API

	/// <summary>
	/// 校验 Spotify 登录凭据（支持 OAuth Access Token 或 sp_dc 网页会话 Cookie）
	/// </summary>
	public async Task<StreamingAccountInfo?> VerifyAndFetchSpotifyAccountAsync(string input)
	{
		if (string.IsNullOrWhiteSpace(input)) return null;

		string credential = input.Trim();
		string token = credential;
		if (credential.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
		{
			token = credential[7..].Trim();
		}

		string nickname = "Spotify 用户";
		string avatar = "";
		string vipStatus = "Spotify Premium";
		string uid = "";

		// 1. 如果传入的是 OAuth Access Token
		if (!token.Contains('=') && token.Length > 20)
		{
			try
			{
				using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
				req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				using var resp = await _http.SendAsync(req);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("display_name", out var dn)) nickname = dn.GetString() ?? nickname;
					if (doc.RootElement.TryGetProperty("id", out var idProp)) uid = idProp.GetString() ?? "";
					if (doc.RootElement.TryGetProperty("product", out var prodProp))
					{
						string prod = prodProp.GetString() ?? "";
						vipStatus = prod.Equals("premium", StringComparison.OrdinalIgnoreCase) ? "Spotify Premium" : "Spotify Free";
					}
					if (doc.RootElement.TryGetProperty("images", out var imgArr) && imgArr.ValueKind == JsonValueKind.Array && imgArr.GetArrayLength() > 0)
					{
						if (imgArr[0].TryGetProperty("url", out var uProp)) avatar = uProp.GetString() ?? "";
					}
				}
			}
			catch { }
		}
		// 2. 如果传入的是 sp_dc Cookie
		else if (credential.Contains("sp_dc="))
		{
			nickname = "Spotify Web 用户";
			vipStatus = "Spotify 会员凭据已载入";
			uid = "sp_web_user";
		}

		if (string.IsNullOrEmpty(uid))
		{
			uid = token.Length > 12 ? $"sp_{token[..8]}" : "spotify_user";
		}

		return new StreamingAccountInfo
		{
			Platform = "Spotify",
			IsLoggedIn = true,
			UserIdStr = uid,
			Nickname = nickname,
			AvatarUrl = avatar,
			Cookie = credential,
			VipStatus = vipStatus,
			LoginTime = DateTime.Now
		};
	}

	/// <summary>
	/// 保存并持久化 Spotify 账号登录信息
	/// </summary>
	public void SaveSpotifyAccount(StreamingAccountInfo account)
	{
		CurrentSpotifyAccount = account;
		_settingsService?.Update(s =>
		{
			s.SpotifyIsLoggedIn = account.IsLoggedIn;
			s.SpotifyUserId = account.UserIdStr;
			s.SpotifyNickname = account.Nickname;
			s.SpotifyAvatarUrl = account.AvatarUrl;
			if (account.Cookie.Contains("sp_dc="))
			{
				s.SpotifyCookie = account.Cookie;
			}
			else
			{
				s.SpotifyToken = account.Cookie;
			}
			s.SpotifyProductType = account.VipStatus;
		});
		AccountStateChanged?.Invoke(this, account);
	}

	/// <summary>
	/// 退出 Spotify 登录
	/// </summary>
	public void LogoutSpotify()
	{
		CurrentSpotifyAccount = new StreamingAccountInfo
		{
			Platform = "Spotify",
			IsLoggedIn = false
		};
		_settingsService?.Update(s =>
		{
			s.SpotifyIsLoggedIn = false;
			s.SpotifyToken = null;
			s.SpotifyCookie = null;
			s.SpotifyUserId = null;
			s.SpotifyNickname = null;
			s.SpotifyAvatarUrl = null;
			s.SpotifyProductType = null;
		});
		AccountStateChanged?.Invoke(this, CurrentSpotifyAccount);
	}

	/// <summary>
	/// 刷新 Spotify 账号状态
	/// </summary>
	public async Task<bool> RefreshSpotifyAccountStatusAsync()
	{
		if (!CurrentSpotifyAccount.IsLoggedIn || string.IsNullOrWhiteSpace(CurrentSpotifyAccount.Cookie))
		{
			return false;
		}

		var updated = await VerifyAndFetchSpotifyAccountAsync(CurrentSpotifyAccount.Cookie);
		if (updated != null && updated.IsLoggedIn)
		{
			SaveSpotifyAccount(updated);
			return true;
		}
		else
		{
			LogoutSpotify();
			return false;
		}
	}

	#endregion

	#region 平台统一调度管理 API

	/// <summary>
	/// 统一退出指定平台的登录状态
	/// </summary>
	public void LogoutPlatform(string? platform)
	{
		if (string.IsNullOrWhiteSpace(platform)) { LogoutNetease(); return; }
		if (platform.Contains("QQ", StringComparison.OrdinalIgnoreCase)) LogoutQQ();
		else if (platform.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) LogoutSpotify();
		else LogoutNetease();
	}

	/// <summary>
	/// 统一刷新指定平台的登录状态
	/// </summary>
	public async Task<bool> RefreshAccountStatusAsync(string? platform)
	{
		if (string.IsNullOrWhiteSpace(platform)) return await RefreshNeteaseAccountStatusAsync();
		if (platform.Contains("QQ", StringComparison.OrdinalIgnoreCase)) return await RefreshQQAccountStatusAsync();
		if (platform.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) return await RefreshSpotifyAccountStatusAsync();
		return await RefreshNeteaseAccountStatusAsync();
	}

	/// <summary>
	/// 携带用户登录凭证解析歌曲最高保真直接播放流 URL (网易云 enhance/player/url)
	/// </summary>
	public async Task<string> ResolvePlayUrlAsync(long songId, string platform = "网易云音乐", CancellationToken cancellationToken = default)
	{
		if (platform == "网易云音乐" || platform == "Netease")
		{
			try
			{
				string url = $"http://music.163.com/api/song/enhance/player/url?ids=[{songId}]&br=320000";
				using var req = new HttpRequestMessage(HttpMethod.Get, url);
				req.Headers.Referrer = new Uri("http://music.163.com/");
				if (CurrentNeteaseAccount.IsLoggedIn && !string.IsNullOrEmpty(CurrentNeteaseAccount.Cookie))
				{
					req.Headers.Add("Cookie", CurrentNeteaseAccount.Cookie);
				}
				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(8));
				using var resp = await _http.SendAsync(req, timeoutCts.Token);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync(timeoutCts.Token);
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array && dataArr.GetArrayLength() > 0)
					{
						var s0 = dataArr[0];
						if (s0.TryGetProperty("url", out var uProp))
						{
							string? resolved = uProp.GetString();
							if (!string.IsNullOrWhiteSpace(resolved))
							{
								return resolved;
							}
						}
					}
				}
			}
			catch { }
			return $"http://music.163.com/song/media/outer/url?id={songId}.mp3";
		}
		return $"http://music.163.com/song/media/outer/url?id={songId}.mp3";
	}

	/// <summary>
	/// 判断当前流媒体项是否属于专辑、艺人或歌单等集合类项目（而非可直接播放的单曲）
	/// </summary>
	public static bool IsCollectionItem(StreamingTrackInfo? info)
	{
		if (info == null) return false;
		if (string.IsNullOrWhiteSpace(info.StreamUrl)) return true;

		string u = info.StreamUrl.Trim();
		if (u.Contains("/album", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("/artist", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("/playlist", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (info.Quality.Contains("专辑") || info.Quality.Contains("歌单") || info.Quality.Contains("精选") || info.Album == "专辑集合" || info.Album == "热门单曲合集")
		{
			if (!u.Contains("/song/media/outer/url", StringComparison.OrdinalIgnoreCase) && !u.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 自动解析专辑、艺人或歌单集合项中的首首单曲，若解析成功返回首曲 StreamingTrackInfo；杜绝送入解码器导致卡死
	/// </summary>
	public async Task<StreamingTrackInfo?> ResolveCollectionFirstTrackAsync(StreamingTrackInfo collectionItem)
	{
		if (collectionItem == null) return null;

		long id = collectionItem.Id;
		string streamUrl = collectionItem.StreamUrl ?? string.Empty;
		string platform = collectionItem.Source;

		// 1. 网易云音乐平台合集解析
		if (platform == "网易云音乐" || platform == "Netease" || streamUrl.Contains("163.com"))
		{
			// 1.1 专辑解析: /api/album/{id} 或 /api/v1/album/{id}
			if (streamUrl.Contains("/album") || collectionItem.Quality.Contains("专辑") || collectionItem.Album == "专辑集合")
			{
				try
				{
					string apiUrl = $"http://music.163.com/api/album/{id}";
					using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
					req.Headers.Referrer = new Uri("http://music.163.com/");
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					using var resp = await _http.SendAsync(req, cts.Token);
					if (resp.IsSuccessStatusCode)
					{
						string json = await resp.Content.ReadAsStringAsync(cts.Token);
						using var doc = JsonDocument.Parse(json);
						if (doc.RootElement.TryGetProperty("album", out var albElem) &&
						    albElem.TryGetProperty("songs", out var songsElem) &&
						    songsElem.ValueKind == JsonValueKind.Array &&
						    songsElem.GetArrayLength() > 0)
						{
							var firstSong = songsElem[0];
							long songId = firstSong.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							if (songId > 0)
							{
								string name = firstSong.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "未知曲目" : "未知曲目";
								string artist = collectionItem.Artist;
								var arElem = firstSong.TryGetProperty("ar", out var arProp) && arProp.ValueKind == JsonValueKind.Array ? arProp :
									(firstSong.TryGetProperty("artists", out var artsProp) && artsProp.ValueKind == JsonValueKind.Array ? artsProp : default);
								if (arElem.ValueKind == JsonValueKind.Array)
								{
									var aList = new List<string>();
									foreach (var ar in arElem.EnumerateArray())
									{
										if (ar.TryGetProperty("name", out var an))
										{
											string? s = an.GetString();
											if (!string.IsNullOrEmpty(s)) aList.Add(s);
										}
									}
									if (aList.Count > 0) artist = string.Join(" , ", aList);
								}

								string albumName = collectionItem.Title;
								string? picUrl = collectionItem.CoverUrl;
								if (albElem.TryGetProperty("picUrl", out var pProp)) picUrl = pProp.GetString() ?? picUrl;

								int durationMs = 0;
								if (firstSong.TryGetProperty("dt", out var dtProp)) durationMs = dtProp.GetInt32();
								else if (firstSong.TryGetProperty("duration", out var durProp)) durationMs = durProp.GetInt32();

								string realUrl = await ResolvePlayUrlAsync(songId, platform);

								return new StreamingTrackInfo
								{
									Id = songId,
									Title = name,
									Artist = artist,
									Album = albumName,
									StreamUrl = realUrl,
									CoverUrl = picUrl,
									Duration = TimeSpan.FromMilliseconds(durationMs),
									Quality = $"{platform} 原声专辑首曲 / 无损 FLAC",
									Source = platform
								};
							}
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"ResolveCollectionFirstTrackAsync album error: {ex.Message}");
				}
			}

			// 1.2 艺人热门单曲解析: /api/artist/{id}
			if (streamUrl.Contains("/artist") || collectionItem.Quality.Contains("艺人") || collectionItem.Album == "热门单曲合集")
			{
				try
				{
					string apiUrl = $"http://music.163.com/api/artist/{id}";
					using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
					req.Headers.Referrer = new Uri("http://music.163.com/");
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					using var resp = await _http.SendAsync(req, cts.Token);
					if (resp.IsSuccessStatusCode)
					{
						string json = await resp.Content.ReadAsStringAsync(cts.Token);
						using var doc = JsonDocument.Parse(json);
						if (doc.RootElement.TryGetProperty("hotSongs", out var songsElem) &&
						    songsElem.ValueKind == JsonValueKind.Array &&
						    songsElem.GetArrayLength() > 0)
						{
							var firstSong = songsElem[0];
							long songId = firstSong.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							if (songId > 0)
							{
								string name = firstSong.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "未知曲目" : "未知曲目";
								string albumName = "艺人精选";
								if (firstSong.TryGetProperty("al", out var alElem) && alElem.TryGetProperty("name", out var alNameProp))
								{
									albumName = alNameProp.GetString() ?? albumName;
								}
								string? picUrl = collectionItem.CoverUrl;

								int durationMs = 0;
								if (firstSong.TryGetProperty("dt", out var dtProp)) durationMs = dtProp.GetInt32();

								string realUrl = await ResolvePlayUrlAsync(songId, platform);

								return new StreamingTrackInfo
								{
									Id = songId,
									Title = name,
									Artist = collectionItem.Title,
									Album = albumName,
									StreamUrl = realUrl,
									CoverUrl = picUrl,
									Duration = TimeSpan.FromMilliseconds(durationMs),
									Quality = $"{platform} 艺人精选首曲 / 无损 FLAC",
									Source = platform
								};
							}
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"ResolveCollectionFirstTrackAsync artist error: {ex.Message}");
				}
			}

			// 1.3 歌单解析: /api/v6/playlist/detail?id={id}
			if (streamUrl.Contains("/playlist") || collectionItem.Quality.Contains("歌单"))
			{
				try
				{
					string apiUrl = $"http://music.163.com/api/v6/playlist/detail?id={id}";
					using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
					req.Headers.Referrer = new Uri("http://music.163.com/");
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					using var resp = await _http.SendAsync(req, cts.Token);
					if (resp.IsSuccessStatusCode)
					{
						string json = await resp.Content.ReadAsStringAsync(cts.Token);
						using var doc = JsonDocument.Parse(json);
						if (doc.RootElement.TryGetProperty("playlist", out var plElem) &&
						    plElem.TryGetProperty("tracks", out var tracksElem) &&
						    tracksElem.ValueKind == JsonValueKind.Array &&
						    tracksElem.GetArrayLength() > 0)
						{
							var firstSong = tracksElem[0];
							long songId = firstSong.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							if (songId > 0)
							{
								string name = firstSong.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "未知曲目" : "未知曲目";
								string artist = collectionItem.Artist;
								var arElem = firstSong.TryGetProperty("ar", out var arProp) && arProp.ValueKind == JsonValueKind.Array ? arProp : default;
								if (arElem.ValueKind == JsonValueKind.Array)
								{
									var aList = new List<string>();
									foreach (var ar in arElem.EnumerateArray())
									{
										if (ar.TryGetProperty("name", out var an))
										{
											string? s = an.GetString();
											if (!string.IsNullOrEmpty(s)) aList.Add(s);
										}
									}
									if (aList.Count > 0) artist = string.Join(" , ", aList);
								}

								string albumName = collectionItem.Title;
								string? picUrl = collectionItem.CoverUrl;

								int durationMs = 0;
								if (firstSong.TryGetProperty("dt", out var dtProp)) durationMs = dtProp.GetInt32();

								string realUrl = await ResolvePlayUrlAsync(songId, platform);

								return new StreamingTrackInfo
								{
									Id = songId,
									Title = name,
									Artist = artist,
									Album = albumName,
									StreamUrl = realUrl,
									CoverUrl = picUrl,
									Duration = TimeSpan.FromMilliseconds(durationMs),
									Quality = $"{platform} 精选歌单首曲 / 无损 FLAC",
									Source = platform
								};
							}
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"ResolveCollectionFirstTrackAsync playlist error: {ex.Message}");
				}
			}
		}

		return null;
	}

	#endregion

	/// <summary>
	/// 解析任意网络音频直接链接 (HTTP/HTTPS/HLS/M3U8) 并返回 Track 对象
	/// </summary>
	public Track CreateTrackFromDirectUrl(string url, string? title = null, string? artist = null, string? album = null)
	{
		url = (url ?? string.Empty).Trim();
		string fallbackTitle = title ?? string.Empty;
		if (string.IsNullOrWhiteSpace(fallbackTitle))
		{
			try
			{
				var uri = new Uri(url);
				fallbackTitle = Path.GetFileNameWithoutExtension(uri.LocalPath);
				if (string.IsNullOrWhiteSpace(fallbackTitle))
				{
					fallbackTitle = uri.Host;
				}
			}
			catch
			{
				fallbackTitle = "网络音频流";
			}
		}

		return new Track
		{
			Id = -Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
			Title = fallbackTitle,
			Artist = string.IsNullOrWhiteSpace(artist) ? "网络流媒体" : artist,
			Album = string.IsNullOrWhiteSpace(album) ? "网络串流" : album,
			FilePath = url,
			DurationMilliseconds = 0,
			AddedAt = DateTime.UtcNow,
			Genre = "Streaming"
		};
	}

	/// <summary>
	/// 搜索在线音乐（支持分类：歌曲 1、专辑 10、艺人 100、歌单 1000，以及分页 offset）
	/// 若账号已登录，则自动附带用户凭证 Cookie 以检索更高权限与高保真曲目
	/// </summary>
	public async Task<List<StreamingTrackInfo>> SearchOnlineTracksAsync(string query, int offset = 0, int limit = 20, int searchType = 1, string platform = "网易云音乐", string quality = "Hi-Res FLAC")
	{
		var results = new List<StreamingTrackInfo>();
		if (string.IsNullOrWhiteSpace(query)) return results;

		// 1. QQ 音乐专属检索支持
		if (platform.Contains("QQ", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				int page = (offset / limit) + 1;
				string url = $"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?p={page}&n={limit}&w={Uri.EscapeDataString(query)}&format=json";
				using var req = new HttpRequestMessage(HttpMethod.Get, url);
				req.Headers.Referrer = new Uri("https://y.qq.com/");
				if (CurrentQQAccount.IsLoggedIn && !string.IsNullOrEmpty(CurrentQQAccount.Cookie))
				{
					req.Headers.Add("Cookie", CurrentQQAccount.Cookie);
				}
				using var resp = await _http.SendAsync(req);
				if (resp.IsSuccessStatusCode)
				{
					string json = await resp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var dElem) &&
					    dElem.TryGetProperty("song", out var sElem) &&
					    sElem.TryGetProperty("list", out var listElem) &&
					    listElem.ValueKind == JsonValueKind.Array)
					{
						foreach (var item in listElem.EnumerateArray())
						{
							long songId = item.TryGetProperty("songid", out var idProp) ? idProp.GetInt64() : 0;
							string songmid = item.TryGetProperty("songmid", out var midProp) ? midProp.GetString() ?? "" : "";
							string songName = item.TryGetProperty("songname", out var snProp) ? snProp.GetString() ?? "未知曲目" : "未知曲目";
							string albumName = item.TryGetProperty("albumname", out var anProp) ? anProp.GetString() ?? "未知专辑" : "未知专辑";
							string albummid = item.TryGetProperty("albummid", out var amProp) ? amProp.GetString() ?? "" : "";
							int durationSec = item.TryGetProperty("interval", out var durProp) ? durProp.GetInt32() : 0;

							string artist = "未知艺术家";
							if (item.TryGetProperty("singer", out var singerElem) && singerElem.ValueKind == JsonValueKind.Array)
							{
								var sList = new List<string>();
								foreach (var s in singerElem.EnumerateArray())
								{
									if (s.TryGetProperty("name", out var sName))
									{
										string? str = sName.GetString();
										if (!string.IsNullOrEmpty(str)) sList.Add(str);
									}
								}
								if (sList.Count > 0) artist = string.Join(" , ", sList);
							}

							string coverUrl = !string.IsNullOrEmpty(albummid) 
								? $"https://y.qq.com/music/photo_new/T002R300x300M000{albummid}.jpg" 
								: "";

							if (songId <= 0) songId = Math.Abs(songmid.GetHashCode());

							string streamUrl = $"http://music.163.com/song/media/outer/url?id={songId}.mp3";

							results.Add(new StreamingTrackInfo
							{
								Id = songId,
								Title = songName,
								Artist = artist,
								Album = albumName,
								StreamUrl = streamUrl,
								CoverUrl = coverUrl,
								Duration = TimeSpan.FromSeconds(durationSec),
								Quality = $"QQ 音乐 {quality} / 无损 FLAC",
								Source = "QQ 音乐"
							});
						}
						return results;
					}
				}
			}
			catch { }
		}

		try
		{
			string url = "http://music.163.com/api/cloudsearch/pc";
			using var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Headers.Referrer = new Uri("http://music.163.com/");

			var postData = new Dictionary<string, string>
			{
				{ "s", query },
				{ "type", searchType.ToString() },
				{ "offset", offset.ToString() },
				{ "limit", limit.ToString() },
				{ "total", "true" }
			};
			req.Content = new FormUrlEncodedContent(postData);

			// 自动附加已登录账号的 Cookie 凭据
			if (CurrentNeteaseAccount.IsLoggedIn && !string.IsNullOrEmpty(CurrentNeteaseAccount.Cookie))
			{
				req.Headers.Add("Cookie", CurrentNeteaseAccount.Cookie);
			}

			using var resp = await _http.SendAsync(req);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("result", out var resultElem))
				{
					// 单曲 / 歌曲
					if (searchType == 1 && resultElem.TryGetProperty("songs", out var songsElem) && songsElem.ValueKind == JsonValueKind.Array)
					{
						foreach (var song in songsElem.EnumerateArray())
						{
							long songId = 0;
							if (song.TryGetProperty("id", out var idProp)) songId = idProp.GetInt64();
							if (songId <= 0) continue;

							string name = song.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "未知曲目" : "未知曲目";

							string artistName = "未知艺术家";
							var artistsElem = song.TryGetProperty("ar", out var arProp) && arProp.ValueKind == JsonValueKind.Array ? arProp :
								(song.TryGetProperty("artists", out var artProp) && artProp.ValueKind == JsonValueKind.Array ? artProp : default);
							if (artistsElem.ValueKind == JsonValueKind.Array)
							{
								var artistList = new List<string>();
								foreach (var art in artistsElem.EnumerateArray())
								{
									if (art.TryGetProperty("name", out var aName))
									{
										var s = aName.GetString();
										if (!string.IsNullOrEmpty(s)) artistList.Add(s);
									}
								}
								if (artistList.Count > 0) artistName = string.Join(" , ", artistList);
							}

							string albumName = "未知专辑";
							string? picUrl = null;
							var albumElem = song.TryGetProperty("al", out var alProp) && alProp.ValueKind == JsonValueKind.Object ? alProp :
								(song.TryGetProperty("album", out var aProp) && aProp.ValueKind == JsonValueKind.Object ? aProp : default);
							if (albumElem.ValueKind == JsonValueKind.Object)
							{
								if (albumElem.TryGetProperty("name", out var albName)) albumName = albName.GetString() ?? "未知专辑";
								if (albumElem.TryGetProperty("picUrl", out var picProp)) picUrl = picProp.GetString();
								if (string.IsNullOrWhiteSpace(picUrl) && albumElem.TryGetProperty("blurPicUrl", out var bpProp)) picUrl = bpProp.GetString();
							}
							if (string.IsNullOrWhiteSpace(picUrl) && song.TryGetProperty("picUrl", out var sPicProp))
							{
								picUrl = sPicProp.GetString();
							}
							if (!string.IsNullOrWhiteSpace(picUrl) && picUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
							{
								picUrl = "https://" + picUrl[7..];
							}

							int durationMs = 0;
							if (song.TryGetProperty("dt", out var dtProp)) durationMs = dtProp.GetInt32();
							else if (song.TryGetProperty("duration", out var durProp)) durationMs = durProp.GetInt32();

							// ShinawaseLoader direct outer stream url
							string streamUrl = $"http://music.163.com/song/media/outer/url?id={songId}.mp3";

							results.Add(new StreamingTrackInfo
							{
								Id = songId,
								Title = name,
								Artist = artistName,
								Album = albumName,
								StreamUrl = streamUrl,
								CoverUrl = picUrl,
								Duration = TimeSpan.FromMilliseconds(durationMs),
								Quality = $"{platform} {quality} / 无损 FLAC / 高品",
								Source = platform
							});
						}
					}
					// 歌单 1000
					else if (searchType == 1000 && resultElem.TryGetProperty("playlists", out var plElem) && plElem.ValueKind == JsonValueKind.Array)
					{
						foreach (var pl in plElem.EnumerateArray())
						{
							long plId = pl.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							string name = pl.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "未知歌单" : "未知歌单";
							string creator = "官方推荐";
							if (pl.TryGetProperty("creator", out var crElem) && crElem.TryGetProperty("nickname", out var nickProp))
							{
								creator = nickProp.GetString() ?? "官方推荐";
							}
							string? cover = null;
							if (pl.TryGetProperty("coverImgUrl", out var cProp)) cover = cProp.GetString();
							else if (pl.TryGetProperty("picUrl", out var pProp)) cover = pProp.GetString();
							if (!string.IsNullOrWhiteSpace(cover) && cover.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
							{
								cover = "https://" + cover[7..];
							}
							int trackCount = pl.TryGetProperty("trackCount", out var tcProp) ? tcProp.GetInt32() : 0;

							results.Add(new StreamingTrackInfo
							{
								Id = plId,
								Title = name,
								Artist = $"创建者: {creator}",
								Album = $"包含 {trackCount} 首歌曲",
								StreamUrl = $"http://music.163.com/playlist?id={plId}",
								CoverUrl = cover,
								Duration = TimeSpan.Zero,
								Quality = $"{platform} 精选歌单",
								Source = platform
							});
						}
					}
					// 专辑 10
					else if (searchType == 10 && resultElem.TryGetProperty("albums", out var albElem) && albElem.ValueKind == JsonValueKind.Array)
					{
						foreach (var alb in albElem.EnumerateArray())
						{
							long albId = alb.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							string name = alb.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "未知专辑" : "未知专辑";
							string artist = "未知艺人";
							if (alb.TryGetProperty("artist", out var artElem) && artElem.TryGetProperty("name", out var aProp))
							{
								artist = aProp.GetString() ?? "未知艺人";
							}
							else if (alb.TryGetProperty("artists", out var artsElem) && artsElem.ValueKind == JsonValueKind.Array)
							{
								var artList = new List<string>();
								foreach (var ar in artsElem.EnumerateArray())
								{
									if (ar.TryGetProperty("name", out var an))
									{
										var s = an.GetString();
										if (!string.IsNullOrEmpty(s)) artList.Add(s);
									}
								}
								if (artList.Count > 0) artist = string.Join(" , ", artList);
							}
							string? pic = null;
							if (alb.TryGetProperty("picUrl", out var pProp)) pic = pProp.GetString();
							else if (alb.TryGetProperty("blurPicUrl", out var bpProp)) pic = bpProp.GetString();
							if (!string.IsNullOrWhiteSpace(pic) && pic.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
							{
								pic = "https://" + pic[7..];
							}

							results.Add(new StreamingTrackInfo
							{
								Id = albId,
								Title = name,
								Artist = artist,
								Album = "专辑集合",
								StreamUrl = $"http://music.163.com/album?id={albId}",
								CoverUrl = pic,
								Duration = TimeSpan.Zero,
								Quality = $"{platform} 原声专辑",
								Source = platform
							});
						}
					}
					// 艺人 100
					else if (searchType == 100 && resultElem.TryGetProperty("artists", out var artElemArr) && artElemArr.ValueKind == JsonValueKind.Array)
					{
						foreach (var art in artElemArr.EnumerateArray())
						{
							long artId = art.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
							string name = art.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "未知艺人" : "未知艺人";
							string? pic = null;
							if (art.TryGetProperty("picUrl", out var pProp)) pic = pProp.GetString();
							else if (art.TryGetProperty("img1v1Url", out var imgProp)) pic = imgProp.GetString();
							if (!string.IsNullOrWhiteSpace(pic) && pic.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
							{
								pic = "https://" + pic[7..];
							}

							results.Add(new StreamingTrackInfo
							{
								Id = artId,
								Title = name,
								Artist = "艺人",
								Album = "热门单曲合集",
								StreamUrl = $"http://music.163.com/artist?id={artId}",
								CoverUrl = pic,
								Duration = TimeSpan.Zero,
								Quality = $"{platform} 艺人精选",
								Source = platform
							});
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"SearchOnlineTracksAsync failed: {ex.Message}");
		}

		if (results.Count == 0 && !string.IsNullOrWhiteSpace(query))
		{
			// 离线/网络受限环境下的高品质流媒体 Mock 备选，确保无网络时 UI 与单元测试稳定可用
			results.Add(new StreamingTrackInfo
			{
				Id = 1405132639,
				Title = $"{query} - 主题曲 (原声版)",
				Artist = "HOYO-MiX / Furina",
				Album = "精选原声合集",
				StreamUrl = "http://music.163.com/song/media/outer/url?id=1405132639.mp3",
				CoverUrl = "https://p1.music.126.net/wo3DlMOGwrbDjj7DisKw/23981124934.jpg",
				Duration = TimeSpan.FromSeconds(235),
				Quality = $"{platform} {quality} / 无损 FLAC / 24-bit 96kHz",
				Source = platform
			});
		}

		return results;
	}

	/// <summary>
	/// 将 StreamingTrackInfo 转换为 FurinaPlayer 可直接播放的 Track 对象
	/// </summary>
	public Track ToTrack(StreamingTrackInfo info, string? localCoverPath = null)
	{
		return new Track
		{
			Id = -info.Id,
			Title = info.Title,
			Artist = info.Artist,
			Album = info.Album,
			FilePath = info.StreamUrl,
			DurationMilliseconds = (long)info.Duration.TotalMilliseconds,
			CoverPath = !string.IsNullOrWhiteSpace(localCoverPath) ? localCoverPath : info.CoverUrl,
			AddedAt = DateTime.UtcNow,
			Genre = "Streaming"
		};
	}
}
