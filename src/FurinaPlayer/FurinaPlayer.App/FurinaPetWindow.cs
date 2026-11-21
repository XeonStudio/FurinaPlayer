using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FurinaPlayer.Core.Services;
using SkiaSharp;

namespace FurinaPlayer.App;

public enum PetAnimState
{
	Idle,
	Groove,
	Click
}

/// <summary>
/// 桌面宠物（看板娘）透明悬浮窗：
/// 基于 Win32 WS_EX_LAYERED + UpdateLayeredWindow 实现逐像素 Alpha 透明，
/// 使用 SkiaSharp SKCodec 解码三态 GIF（Idle / Groove / Click）并绘制悬停快捷播控栏。
/// </summary>
public sealed class FurinaPetWindow : IDisposable
{
	private const int BasePetWidth = 180;
	private const int BasePetHeight = 160;
	private const int LyricAreaWidth = 420;
	private const int LyricAreaHeight = 200;
	private const int BarAreaHeight = 40;
	private const int DragThresholdPx = 4;
	private const int ClickStateDurationMs = 900;
	private const int SaveDebounceMs = 350;
	private const int BarHideDelayMs = 2500;
	private const int LyricAnimDurationMs = 380;

	private const int WS_POPUP = unchecked((int)0x80000000);
	private const int WS_EX_TOPMOST = 0x00000008;
	private const int WS_EX_TOOLWINDOW = 0x00000080;
	private const int WS_EX_LAYERED = 0x00080000;
	private const int WS_EX_NOACTIVATE = 0x08000000;

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOMOVE = 0x0002;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_NOACTIVATE = 0x0010;
	private const uint SWP_SHOWWINDOW = 0x0040;
	private const uint SWP_HIDEWINDOW = 0x0080;

	private static readonly IntPtr HWND_TOPMOST = new(-1);

	private const int WM_DESTROY = 0x0002;
	private const int WM_DISPLAYCHANGE = 0x007E;
	private const int WM_NCHITTEST = 0x0084;
	private const int WM_COMMAND = 0x0111;
	private const int WM_TIMER = 0x0113;
	private const int WM_MOUSEMOVE = 0x0200;
	private const int WM_LBUTTONDOWN = 0x0201;
	private const int WM_LBUTTONUP = 0x0202;
	private const int WM_RBUTTONUP = 0x0205;
	private const int WM_MOUSELEAVE = 0x02A3;

	private const int HTCLIENT = 1;
	private const int HTTRANSPARENT = -1;

	private const uint TME_LEAVE = 0x00000002;

	private const uint ULW_ALPHA = 0x00000002;
	private const byte AC_SRC_OVER = 0x00;
	private const byte AC_SRC_ALPHA = 0x01;

	private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
	private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

	private const int TPM_LEFTALIGN = 0x0000;
	private const int TPM_RIGHTBUTTON = 0x0002;
	private const int MF_STRING = 0x00000000;
	private const int MF_SEPARATOR = 0x00000800;

	private const int IDM_PREV = 2001;
	private const int IDM_PLAYPAUSE = 2002;
	private const int IDM_NEXT = 2003;
	private const int IDM_RESET_POS = 2004;
	private const int IDM_HIDE = 2005;
	private const int IDM_TOGGLE_LYRICS = 2006;

	private static readonly IntPtr TIMER_ANIM_ID = new(1);
	private static readonly IntPtr TIMER_SAVE_POS_ID = new(2);
	private static readonly IntPtr TIMER_CLICK_RETURN_ID = new(3);
	private static readonly IntPtr TIMER_HIDE_BAR_ID = new(4);
	private static readonly IntPtr TIMER_LYRIC_ANIM_ID = new(5);

	private sealed class GifFrame : IDisposable
	{
		public SKBitmap Bitmap { get; }
		public int DurationMs { get; }

		public GifFrame(SKBitmap bitmap, int durationMs)
		{
			Bitmap = bitmap;
			DurationMs = durationMs;
		}

		public void Dispose() => Bitmap.Dispose();
	}

	private sealed class GifAnimation : IDisposable
	{
		public List<GifFrame> Frames { get; } = new();

		public void Dispose()
		{
			foreach (var f in Frames)
				f.Dispose();
			Frames.Clear();
		}
	}

	private IntPtr _hwnd;
	private string? _className;
	private WndProcDelegate? _wndProcDelegate;
	private bool _disposed;
	private bool _isVisible;

	private int _scalePercent = 100;
	private int _gifSpeedPercent = 100;
	private int _bubbleScalePercent = 100;
	private int _petWidth = BasePetWidth;
	private int _petHeight = BasePetHeight;
	private int _lyricTopOffset = LyricAreaHeight;
	private int _totalWidth = Math.Max(BasePetWidth, LyricAreaWidth);
	private int _totalHeight = LyricAreaHeight + BasePetHeight + BarAreaHeight;
	private int _left;
	private int _top;

	private IntPtr _memDc;
	private IntPtr _dibBitmap;
	private IntPtr _oldBitmap;
	private IntPtr _dibBits;
	private int _surfaceWidth;
	private int _surfaceHeight;

	private GifAnimation? _idleAnim;
	private GifAnimation? _grooveAnim;
	private GifAnimation? _clickAnim;
	private string? _customIdlePath;
	private string? _customGroovePath;
	private string? _customClickPath;

	private PetAnimState _state = PetAnimState.Idle;
	private bool _isPlaying;
	private int _frameIndex;

	private bool _isHovered;
	private bool _trackingMouseLeave;
	private int _hoverButtonIndex = -1;
	private int _pressedButtonIndex = -1;

	private bool _isPointerDown;
	private bool _isDragging;
	private POINT _dragStartCursor;
	private int _dragStartLeft;
	private int _dragStartTop;
	private bool _pendingPositionSave;

	private SKBitmap? _bubbleTl;
	private SKBitmap? _bubbleTr;
	private SKBitmap? _bubbleBl;
	private SKBitmap? _bubbleBr;
	private bool _lyricsEnabled = true;
	private string? _lyricCur;
	private string? _lyricPrev1;
	private string? _lyricPrev2;
	private string? _lyricExiting;
	private long _lyricAnimStartMs;
	private bool _isLyricAnimating;

	public event Action? PlayPauseRequested;
	public event Action? NextRequested;
	public event Action? PreviousRequested;
	public event Action? ResetPositionRequested;
	public event Action? HideRequested;
	public event Action? ToggleLyricsRequested;
	public event Action<int, int>? PositionChanged;

	public bool IsVisible => _isVisible;

	public static (int Left, int Top) ClampPetBoundsToWorkArea(
		int? savedLeft,
		int? savedTop,
		int width,
		int height,
		int workLeft,
		int workTop,
		int workRight,
		int workBottom)
		=> DesktopPetLayout.ClampPetBoundsToWorkArea(savedLeft, savedTop, width, height, workLeft, workTop, workRight, workBottom);

	public FurinaPetWindow(int scalePercent, int? savedLeft, int? savedTop, string? idlePath, string? groovePath, string? clickPath)
	{
		_scalePercent = Math.Clamp(scalePercent, 60, 180);
		_customIdlePath = idlePath;
		_customGroovePath = groovePath;
		_customClickPath = clickPath;

		UpdateDimensionsFromScale();
		CreateNativeWindow();
		LoadBubbleCornerAssets();
		ReloadAllAnimations();
		ApplyInitialOrSavedPosition(savedLeft, savedTop);
	}

	public void Show()
	{
		if (_disposed || _hwnd == IntPtr.Zero) return;
		_isVisible = true;
		_frameIndex = 0;
		RenderCurrentFrame();
		SetWindowPos(_hwnd, HWND_TOPMOST, _left, _top, _totalWidth, _totalHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE);
		ScheduleNextAnimTick();
	}

	public void Hide()
	{
		if (_disposed || _hwnd == IntPtr.Zero) return;
		_isVisible = false;
		KillTimer(_hwnd, TIMER_ANIM_ID);
		KillTimer(_hwnd, TIMER_CLICK_RETURN_ID);
		KillTimer(_hwnd, TIMER_HIDE_BAR_ID);
		KillTimer(_hwnd, TIMER_LYRIC_ANIM_ID);
		SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_HIDEWINDOW | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
	}

	public void SetLyricsEnabled(bool enabled)
	{
		if (_disposed || _lyricsEnabled == enabled) return;
		_lyricsEnabled = enabled;
		if (!enabled)
		{
			_isLyricAnimating = false;
			_lyricExiting = null;
			if (_hwnd != IntPtr.Zero) KillTimer(_hwnd, TIMER_LYRIC_ANIM_ID);
		}
		if (_isVisible) RenderCurrentFrame();
	}

	public void SetLyrics(string? currentLine, string? prevLine1, string? prevLine2)
	{
		if (_disposed) return;
		string? cur = string.IsNullOrWhiteSpace(currentLine) ? null : currentLine.Trim();
		string? p1 = string.IsNullOrWhiteSpace(prevLine1) ? null : prevLine1.Trim();
		string? p2 = string.IsNullOrWhiteSpace(prevLine2) ? null : prevLine2.Trim();

		if (string.Equals(_lyricCur, cur, StringComparison.Ordinal) &&
			string.Equals(_lyricPrev1, p1, StringComparison.Ordinal) &&
			string.Equals(_lyricPrev2, p2, StringComparison.Ordinal))
		{
			return;
		}

		// 若歌词向前推进了一行，原来的上上行 (_lyricPrev2) 成为历史第四个气泡并渐隐消失
		bool advancedOne = _lyricCur != null && string.Equals(p1, _lyricCur, StringComparison.Ordinal);
		if (advancedOne && !string.IsNullOrEmpty(_lyricPrev2))
		{
			_lyricExiting = _lyricPrev2;
		}
		else
		{
			_lyricExiting = null;
		}

		_lyricCur = cur;
		_lyricPrev1 = p1;
		_lyricPrev2 = p2;

		if (_lyricsEnabled && (_lyricCur != null || _lyricPrev1 != null || _lyricPrev2 != null || _lyricExiting != null))
		{
			_lyricAnimStartMs = Environment.TickCount64;
			_isLyricAnimating = true;
			if (_hwnd != IntPtr.Zero && _isVisible)
			{
				SetTimer(_hwnd, TIMER_LYRIC_ANIM_ID, 30, IntPtr.Zero);
				RenderCurrentFrame();
			}
		}
		else
		{
			_isLyricAnimating = false;
			if (_hwnd != IntPtr.Zero) KillTimer(_hwnd, TIMER_LYRIC_ANIM_ID);
			if (_isVisible) RenderCurrentFrame();
		}
	}

	public void SetPlaybackState(bool isPlaying)
	{
		if (_disposed) return;
		_isPlaying = isPlaying;
		if (_state != PetAnimState.Click)
		{
			var next = isPlaying ? PetAnimState.Groove : PetAnimState.Idle;
			if (_state != next)
			{
				_state = next;
				_frameIndex = 0;
				if (_isVisible)
				{
					RenderCurrentFrame();
					ScheduleNextAnimTick();
				}
			}
			else if (_isVisible && _isHovered)
			{
				// 播放/暂停图标需要刷新
				RenderCurrentFrame();
			}
		}
	}

	public void SetScale(int scalePercent)
	{
		if (_disposed || _hwnd == IntPtr.Zero) return;
		int clamped = Math.Clamp(scalePercent, 60, 180);
		if (_scalePercent == clamped) return;

		_scalePercent = clamped;
		UpdateDimensionsFromScale();

		var work = GetWorkAreaForWindowOrPrimary();
		(_left, _top) = ClampPetBoundsToWorkArea(_left, _top, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);

		if (_isVisible)
		{
			RenderCurrentFrame();
			SetWindowPos(_hwnd, HWND_TOPMOST, _left, _top, _totalWidth, _totalHeight, SWP_NOACTIVATE);
		}
		SchedulePositionSave();
	}

	public void SetGifSpeed(int speedPercent)
	{
		if (_disposed) return;
		int clamped = Math.Clamp(speedPercent, 50, 200);
		if (_gifSpeedPercent == clamped) return;
		_gifSpeedPercent = clamped;
		if (_isVisible)
		{
			ScheduleNextAnimTick();
		}
	}

	public void SetBubbleScale(int scalePercent)
	{
		if (_disposed || _hwnd == IntPtr.Zero) return;
		int clamped = Math.Clamp(scalePercent, 60, 180);
		if (_bubbleScalePercent == clamped) return;

		_bubbleScalePercent = clamped;
		UpdateDimensionsFromScale();

		var work = GetWorkAreaForWindowOrPrimary();
		(_left, _top) = ClampPetBoundsToWorkArea(_left, _top, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);

		if (_isVisible)
		{
			RenderCurrentFrame();
			SetWindowPos(_hwnd, HWND_TOPMOST, _left, _top, _totalWidth, _totalHeight, SWP_NOACTIVATE);
		}
		SchedulePositionSave();
	}

	public void ResetPositionToDefault()
	{
		if (_disposed || _hwnd == IntPtr.Zero) return;
		var work = GetWorkAreaForPrimary();
		(_left, _top) = ClampPetBoundsToWorkArea(null, null, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);
		if (_isVisible)
		{
			SetWindowPos(_hwnd, HWND_TOPMOST, _left, _top, _totalWidth, _totalHeight, SWP_NOSIZE | SWP_NOACTIVATE);
		}
	}

	public void UpdateCustomGif(string kind, string? path)
	{
		if (_disposed) return;
		string key = (kind ?? string.Empty).Trim().ToLowerInvariant();
		switch (key)
		{
			case "idle":
				_customIdlePath = path;
				_idleAnim?.Dispose();
				_idleAnim = LoadGifAnimation(ResolveGifPath(_customIdlePath, "furina_pet_idle.gif"));
				break;
			case "groove":
				_customGroovePath = path;
				_grooveAnim?.Dispose();
				_grooveAnim = LoadGifAnimation(ResolveGifPath(_customGroovePath, "furina_pet_groove.gif"));
				break;
			case "click":
				_customClickPath = path;
				_clickAnim?.Dispose();
				_clickAnim = LoadGifAnimation(ResolveGifPath(_customClickPath, "furina_pet_click.gif"));
				break;
		}
		_frameIndex = 0;
		if (_isVisible)
		{
			RenderCurrentFrame();
			ScheduleNextAnimTick();
		}
	}

	private void UpdateDimensionsFromScale()
	{
		double factor = _scalePercent / 100.0;
		double bubbleFactor = _bubbleScalePercent / 100.0;
		_petWidth = Math.Max(72, (int)Math.Round(BasePetWidth * factor));
		_petHeight = Math.Max(64, (int)Math.Round(BasePetHeight * factor));
		_lyricTopOffset = Math.Max(120, (int)Math.Round(LyricAreaHeight * bubbleFactor));
		int scaledLyricW = Math.Max(260, (int)Math.Round(LyricAreaWidth * bubbleFactor));
		_totalWidth = Math.Max(_petWidth, scaledLyricW);
		_totalHeight = _lyricTopOffset + _petHeight + BarAreaHeight;
	}

	private void ApplyInitialOrSavedPosition(int? savedLeft, int? savedTop)
	{
		RECT work;
		if (savedLeft.HasValue && savedTop.HasValue)
		{
			IntPtr hMon = MonitorFromPoint(new POINT { X = savedLeft.Value + _totalWidth / 2, Y = savedTop.Value + _totalHeight / 2 }, MONITOR_DEFAULTTONEAREST);
			work = GetMonitorWorkArea(hMon);
		}
		else
		{
			work = GetWorkAreaForPrimary();
		}
		(_left, _top) = ClampPetBoundsToWorkArea(savedLeft, savedTop, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);
	}

	private void CreateNativeWindow()
	{
		_wndProcDelegate = WndProc;
		_className = "FurinaPetLayeredWindow_" + Guid.NewGuid().ToString("N");
		var wc = new WNDCLASSEX
		{
			cbSize = Marshal.SizeOf<WNDCLASSEX>(),
			lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
			hInstance = GetModuleHandle(null),
			hCursor = LoadCursor(IntPtr.Zero, 32512), // IDC_ARROW
			lpszClassName = _className
		};
		RegisterClassEx(ref wc);

		int exStyle = WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
		_hwnd = CreateWindowEx(
			exStyle,
			_className,
			"FurinaPet",
			WS_POPUP,
			0, 0, _totalWidth, _totalHeight,
			IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
	}

	private void ReloadAllAnimations()
	{
		_idleAnim?.Dispose();
		_grooveAnim?.Dispose();
		_clickAnim?.Dispose();

		_idleAnim = LoadGifAnimation(ResolveGifPath(_customIdlePath, "furina_pet_idle.gif"));
		_grooveAnim = LoadGifAnimation(ResolveGifPath(_customGroovePath, "furina_pet_groove.gif"));
		_clickAnim = LoadGifAnimation(ResolveGifPath(_customClickPath, "furina_pet_click.gif"));
	}

	private static string ResolveGifPath(string? customPath, string defaultFileName)
	{
		if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
		{
			return customPath;
		}
		return Path.Combine(AppContext.BaseDirectory, "Assets", "Pet", defaultFileName);
	}

	private static GifAnimation? LoadGifAnimation(string path)
	{
		try
		{
			if (!File.Exists(path)) return null;
			using var codec = SKCodec.Create(path);
			if (codec == null) return null;

			var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
			var frameInfos = codec.FrameInfo;
			var anim = new GifAnimation();

			if (frameInfos == null || frameInfos.Length == 0)
			{
				var bmp = new SKBitmap(info);
				if (codec.GetPixels(info, bmp.GetPixels()) == SKCodecResult.Success)
				{
					anim.Frames.Add(new GifFrame(bmp, 100));
				}
				else
				{
					bmp.Dispose();
				}
				return anim;
			}

			for (int i = 0; i < frameInfos.Length; i++)
			{
				var fi = frameInfos[i];
				var bmp = new SKBitmap(info);
				if (fi.RequiredFrame >= 0 && fi.RequiredFrame < anim.Frames.Count)
				{
					using var canvas = new SKCanvas(bmp);
					canvas.Clear(SKColors.Transparent);
					canvas.DrawBitmap(anim.Frames[fi.RequiredFrame].Bitmap, 0, 0);
				}

				var opts = new SKCodecOptions(i, fi.RequiredFrame);
				var res = codec.GetPixels(info, bmp.GetPixels(), opts);
				if (res == SKCodecResult.Success || res == SKCodecResult.IncompleteInput)
				{
					int dur = fi.Duration > 0 ? fi.Duration : 100;
					anim.Frames.Add(new GifFrame(bmp, dur));
				}
				else
				{
					bmp.Dispose();
				}
			}

			return anim;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPetWindow] LoadGifAnimation failed ({path}): {ex.Message}");
			return null;
		}
	}

	private GifAnimation? GetActiveAnimation() => _state switch
	{
		PetAnimState.Click => _clickAnim ?? _grooveAnim ?? _idleAnim,
		PetAnimState.Groove => _grooveAnim ?? _idleAnim,
		_ => _idleAnim ?? _grooveAnim
	};

	private void ScheduleNextAnimTick()
	{
		if (_disposed || _hwnd == IntPtr.Zero || !_isVisible) return;
		var anim = GetActiveAnimation();
		int delay = 125;
		if (anim != null && anim.Frames.Count > 0)
		{
			int idx = _frameIndex % anim.Frames.Count;
			delay = Math.Clamp(anim.Frames[idx].DurationMs, 20, 2000);
		}
		int scaledDelay = Math.Clamp((int)Math.Round(delay * 100.0 / Math.Clamp(_gifSpeedPercent, 50, 200)), 20, 4000);
		SetTimer(_hwnd, TIMER_ANIM_ID, (uint)scaledDelay, IntPtr.Zero);
	}

	private void TriggerClickAnimation()
	{
		_state = PetAnimState.Click;
		_frameIndex = 0;
		RenderCurrentFrame();
		ScheduleNextAnimTick();
		SetTimer(_hwnd, TIMER_CLICK_RETURN_ID, ClickStateDurationMs, IntPtr.Zero);
	}

	private void EnsureSurface(int width, int height)
	{
		if (_surfaceWidth == width && _surfaceHeight == height && _dibBitmap != IntPtr.Zero)
			return;

		ReleaseSurface();

		IntPtr screenDc = GetDC(IntPtr.Zero);
		_memDc = CreateCompatibleDC(screenDc);
		var bmi = new BITMAPINFO
		{
			bmiHeader = new BITMAPINFOHEADER
			{
				biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
				biWidth = width,
				biHeight = -height, // top-down DIB
				biPlanes = 1,
				biBitCount = 32,
				biCompression = 0 // BI_RGB
			}
		};
		_dibBitmap = CreateDIBSection(screenDc, ref bmi, 0, out _dibBits, IntPtr.Zero, 0);
		_oldBitmap = SelectObject(_memDc, _dibBitmap);
		ReleaseDC(IntPtr.Zero, screenDc);

		_surfaceWidth = width;
		_surfaceHeight = height;
	}

	private void ReleaseSurface()
	{
		if (_memDc != IntPtr.Zero)
		{
			if (_oldBitmap != IntPtr.Zero)
			{
				SelectObject(_memDc, _oldBitmap);
				_oldBitmap = IntPtr.Zero;
			}
			if (_dibBitmap != IntPtr.Zero)
			{
				DeleteObject(_dibBitmap);
				_dibBitmap = IntPtr.Zero;
			}
			DeleteDC(_memDc);
			_memDc = IntPtr.Zero;
		}
		_dibBits = IntPtr.Zero;
		_surfaceWidth = 0;
		_surfaceHeight = 0;
	}

	private void RenderCurrentFrame()
	{
		if (_disposed || _hwnd == IntPtr.Zero || _totalWidth <= 0 || _totalHeight <= 0) return;

		try
		{
			EnsureSurface(_totalWidth, _totalHeight);
		if (_dibBits == IntPtr.Zero) return;

		var info = new SKImageInfo(_totalWidth, _totalHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info, _dibBits, _totalWidth * 4);
		if (surface == null) return;

		var canvas = surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		var anim = GetActiveAnimation();
		if (anim != null && anim.Frames.Count > 0)
		{
			int idx = _frameIndex % anim.Frames.Count;
			var frameBmp = anim.Frames[idx].Bitmap;
			float petX = (_totalWidth - _petWidth) / 2f;
			var destRect = new SKRect(petX, _lyricTopOffset, petX + _petWidth, _lyricTopOffset + _petHeight);
			using var paint = new SKPaint
			{
				IsAntialias = true,
				FilterQuality = SKFilterQuality.High
			};
			canvas.DrawBitmap(frameBmp, destRect, paint);
		}

		if (_lyricsEnabled)
		{
			DrawLyricBubbles(canvas);
		}

		if (_isHovered)
		{
			DrawHoverControlBar(canvas);
		}

		canvas.Flush();

		IntPtr screenDc = GetDC(IntPtr.Zero);
		var ptDst = new POINT { X = _left, Y = _top };
		var size = new SIZE { cx = _totalWidth, cy = _totalHeight };
		var ptSrc = new POINT { X = 0, Y = 0 };
		var blend = new BLENDFUNCTION
		{
			BlendOp = AC_SRC_OVER,
			BlendFlags = 0,
			SourceConstantAlpha = 255,
			AlphaFormat = AC_SRC_ALPHA
		};
		UpdateLayeredWindow(_hwnd, screenDc, ref ptDst, ref size, _memDc, ref ptSrc, 0, ref blend, ULW_ALPHA);
		ReleaseDC(IntPtr.Zero, screenDc);
		}
		catch (Exception ex)
		{
			CrashReporter.WriteManaged(ex, "FurinaPetWindow.RenderCurrentFrame");
		}
	}

	private SKRect GetBarRect()
	{
		float barW = Math.Min(_totalWidth - 8, 156);
		float barH = 30;
		float barX = (_totalWidth - barW) / 2f;
		float barY = _lyricTopOffset + _petHeight + 5;
		return new SKRect(barX, barY, barX + barW, barY + barH);
	}

	private void LoadBubbleCornerAssets()
	{
		string dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Pet");
		_bubbleTl = TryLoadPng(Path.Combine(dir, "pet_bubble_tl.png"));
		_bubbleTr = TryLoadPng(Path.Combine(dir, "pet_bubble_tr.png"));
		_bubbleBl = TryLoadPng(Path.Combine(dir, "pet_bubble_bl.png"));
		_bubbleBr = TryLoadPng(Path.Combine(dir, "pet_bubble_br.png"));
	}

	private static SKBitmap? TryLoadPng(string path)
	{
		try
		{
			return File.Exists(path) ? SKBitmap.Decode(path) : null;
		}
		catch
		{
			return null;
		}
	}

	private void DrawLyricBubbles(SKCanvas canvas)
	{
		if (_lyricCur == null && _lyricPrev1 == null && _lyricPrev2 == null && _lyricExiting == null)
			return;

		float t = 1f;
		if (_isLyricAnimating)
		{
			long elapsed = Environment.TickCount64 - _lyricAnimStartMs;
			t = Math.Clamp(elapsed / (float)LyricAnimDurationMs, 0f, 1f);
		}
		float e = 1f - MathF.Pow(1f - t, 3f); // EaseOutCubic

		float bs = _bubbleScalePercent / 100f;
		float gap = 9f * bs;
		float slideOffset = _isLyricAnimating ? 38f * bs * (1f - e) : 0f;
		float cursorBottomY = _lyricTopOffset - 10f * bs; // 紧贴看板娘上方

		// 当前歌词（最下方，第1个气泡）
		if (_lyricCur != null)
		{
			float y = cursorBottomY + (_isLyricAnimating ? 18f * bs * (1f - e) : 0f);
			float alpha = _isLyricAnimating ? e : 1.0f;
			float topY = DrawSingleLyricBubble(canvas, _lyricCur, y, alpha, bs);
			cursorBottomY = (topY < y ? topY : y - 30f * bs) - gap;
		}

		// 上一行歌词（中间，第2个气泡）
		if (_lyricPrev1 != null)
		{
			float y = cursorBottomY + slideOffset;
			float alpha = _isLyricAnimating ? 1.0f + (0.86f - 1.0f) * e : 0.86f;
			float topY = DrawSingleLyricBubble(canvas, _lyricPrev1, y, alpha, bs);
			cursorBottomY = (topY < y ? topY : y - 30f * bs) - gap;
		}

		// 上上行歌词（最上方，第3个气泡）
		if (_lyricPrev2 != null)
		{
			float y = cursorBottomY + slideOffset;
			float alpha = _isLyricAnimating ? 0.86f + (0.68f - 0.86f) * e : 0.68f;
			float topY = DrawSingleLyricBubble(canvas, _lyricPrev2, y, alpha, bs);
			cursorBottomY = (topY < y ? topY : y - 30f * bs) - gap;
		}

		// 历史第四个对话气泡渐隐向上消失
		if (_lyricExiting != null && t < 1f)
		{
			float exitBottomY = cursorBottomY - 38f * bs * e;
			float exitAlpha = 0.68f * (1f - e);
			if (exitAlpha > 0.02f)
				DrawSingleLyricBubble(canvas, _lyricExiting, exitBottomY, exitAlpha, bs);
		}
	}

	private float DrawSingleLyricBubble(SKCanvas canvas, string text, float bottomY, float alpha, float bs)
	{
		if (alpha <= 0.01f) return bottomY;

		using var textPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(0x23, 0x2B, 0x5C),
			TextSize = 12.5f * bs,
			Typeface = SKTypeface.FromFamilyName("Microsoft YaHei UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
			TextAlign = SKTextAlign.Center
		};

		float minBubbleW = 116f * bs;
		float maxBubbleW = 344f * bs;
		float horizPad = 24f * bs;
		float maxTextW = maxBubbleW - horizPad * 2f;

		var lines = WrapLyricText(text, textPaint, maxTextW, maxLines: 3);
		if (lines.Count == 0) return bottomY;
		float measuredMax = 0f;
		foreach (string line in lines)
		{
			float w = textPaint.MeasureText(line);
			if (w > measuredMax) measuredMax = w;
		}

		float bw = Math.Clamp(measuredMax + horizPad * 2f, minBubbleW, maxBubbleW);
		float bh = (lines.Count switch { 1 => 30f, 2 => 44f, _ => 58f }) * bs;
		float bx = (_totalWidth - bw) / 2f;
		float by = bottomY - bh;

		if (by < 10f * bs) return bottomY;

		byte layerAlpha = (byte)Math.Clamp((int)Math.Round(alpha * 255f), 0, 255);
		canvas.SaveLayer(new SKPaint { Color = new SKColor(255, 255, 255, layerAlpha) });

		var rect = new SKRect(bx, by, bx + bw, by + bh);
		using var bgPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(255, 255, 255, 242),
			Style = SKPaintStyle.Fill
		};
		using var borderPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(0x34, 0x39, 0x93),
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 2.4f * bs
		};
		canvas.DrawRoundRect(rect, 4f * bs, 4f * bs, bgPaint);
		canvas.DrawRoundRect(rect, 4f * bs, 4f * bs, borderPaint);

		// 绘制四角芙宁娜像素装饰贴纸（精准对齐原始边框角点）
		float s = 0.30f * bs;
		using var imgPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
		if (_bubbleTl != null)
		{
			float dx = rect.Left - 40f * s;
			float dy = rect.Top - 84f * s;
			canvas.DrawBitmap(_bubbleTl, new SKRect(dx, dy, dx + _bubbleTl.Width * s, dy + _bubbleTl.Height * s), imgPaint);
		}
		if (_bubbleTr != null)
		{
			float dx = rect.Right - 37f * s;
			float dy = rect.Top - 84f * s;
			canvas.DrawBitmap(_bubbleTr, new SKRect(dx, dy, dx + _bubbleTr.Width * s, dy + _bubbleTr.Height * s), imgPaint);
		}
		if (_bubbleBl != null)
		{
			float dx = rect.Left - 35f * s;
			float dy = rect.Bottom - 48f * s;
			canvas.DrawBitmap(_bubbleBl, new SKRect(dx, dy, dx + _bubbleBl.Width * s, dy + _bubbleBl.Height * s), imgPaint);
		}
		if (_bubbleBr != null)
		{
			float dx = rect.Right - 46f * s;
			float dy = rect.Bottom - 44f * s;
			canvas.DrawBitmap(_bubbleBr, new SKRect(dx, dy, dx + _bubbleBr.Width * s, dy + _bubbleBr.Height * s), imgPaint);
		}

		var fm = textPaint.FontMetrics;
		float lineHeight = fm.Descent - fm.Ascent + 2f;
		float totalTextH = lines.Count * lineHeight;
		float startBaselineY = rect.MidY - totalTextH / 2f - fm.Ascent;
		for (int i = 0; i < lines.Count; i++)
		{
			canvas.DrawText(lines[i], rect.MidX, startBaselineY + i * lineHeight, textPaint);
		}

		canvas.Restore();
		return by;
	}

	private static List<string> WrapLyricText(string text, SKPaint paint, float maxWidth, int maxLines)
	{
		var result = new List<string>(maxLines);
		string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
		var segments = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		foreach (string rawSeg in segments)
		{
			if (result.Count >= maxLines) break;

			// 清理任何残留控制字符，避免绘制出方块占位符
			var sb = new System.Text.StringBuilder(rawSeg.Length);
			foreach (char ch in rawSeg)
			{
				if (!char.IsControl(ch))
					sb.Append(ch);
			}
			string seg = sb.ToString().Trim();
			if (seg.Length == 0) continue;

			if (paint.MeasureText(seg) <= maxWidth)
			{
				result.Add(seg);
				continue;
			}

			int start = 0;
			while (start < seg.Length && result.Count < maxLines)
			{
				string rem = seg.Substring(start);
				if (paint.MeasureText(rem) <= maxWidth)
				{
					result.Add(rem.TrimEnd());
					break;
				}

				bool isLastAllowed = result.Count == maxLines - 1;
				if (isLastAllowed)
				{
					int len = rem.Length;
					while (len > 1 && paint.MeasureText(rem.Substring(0, len) + "…") > maxWidth)
						len--;
					result.Add(rem.Substring(0, len) + "…");
					break;
				}

				int cut = start + 1;
				int lastSpace = -1;
				while (cut <= seg.Length)
				{
					string sub = seg.Substring(start, cut - start);
					if (paint.MeasureText(sub) > maxWidth)
					{
						cut--;
						break;
					}
					if (seg[cut - 1] == ' ') lastSpace = cut;
					cut++;
				}
				if (cut > seg.Length) cut = seg.Length;
				if (cut <= start) cut = start + 1;
				if (lastSpace > start && cut < seg.Length) cut = lastSpace;
				result.Add(seg.Substring(start, cut - start).TrimEnd());
				start = cut;
				while (start < seg.Length && seg[start] == ' ') start++;
			}
		}
		return result;
	}

	private SKRect GetBarButtonRect(int index)
	{
		var bar = GetBarRect();
		float slotW = bar.Width / 5f;
		float cx = bar.Left + slotW * (index + 0.5f);
		float cy = bar.MidY;
		float r = 12f;
		return new SKRect(cx - r, cy - r, cx + r, cy + r);
	}

	private int HitTestBarButton(int x, int y)
	{
		if (!_isHovered) return -1;
		var bar = GetBarRect();
		if (!bar.Contains(x, y)) return -1;
		for (int i = 0; i < 5; i++)
		{
			if (GetBarButtonRect(i).Contains(x, y))
				return i;
		}
		return -1;
	}

	private void DrawHoverControlBar(SKCanvas canvas)
	{
		var bar = GetBarRect();
		using var bgPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(18, 25, 42, 218),
			Style = SKPaintStyle.Fill
		};
		canvas.DrawRoundRect(bar, 15f, 15f, bgPaint);

		using var borderPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(125, 211, 252, 140),
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1.1f
		};
		canvas.DrawRoundRect(bar, 15f, 15f, borderPaint);

		for (int i = 0; i < 5; i++)
		{
			var btnRect = GetBarButtonRect(i);
			float cx = btnRect.MidX;
			float cy = btnRect.MidY;

			if (i == _hoverButtonIndex || i == _pressedButtonIndex)
			{
				using var hoverPaint = new SKPaint
				{
					IsAntialias = true,
					Color = i == _pressedButtonIndex
						? new SKColor(125, 211, 252, 110)
						: new SKColor(125, 211, 252, 60),
					Style = SKPaintStyle.Fill
				};
				canvas.DrawCircle(cx, cy, 11.5f, hoverPaint);
			}

			using var iconPaint = new SKPaint
			{
				IsAntialias = true,
				Color = i == 4 ? new SKColor(252, 165, 165, 240) : new SKColor(240, 249, 255, 245),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 1.6f,
				StrokeCap = SKStrokeCap.Round,
				StrokeJoin = SKStrokeJoin.Round
			};
			using var fillPaint = new SKPaint
			{
				IsAntialias = true,
				Color = iconPaint.Color,
				Style = SKPaintStyle.Fill
			};

			switch (i)
			{
				case 0: // Previous
					canvas.DrawLine(cx - 4.5f, cy - 4f, cx - 4.5f, cy + 4f, iconPaint);
					using (var p = new SKPath())
					{
						p.MoveTo(cx + 4.5f, cy - 4.5f);
						p.LineTo(cx - 2.5f, cy);
						p.LineTo(cx + 4.5f, cy + 4.5f);
						p.Close();
						canvas.DrawPath(p, fillPaint);
					}
					break;
				case 1: // Play / Pause
					if (_isPlaying)
					{
						canvas.DrawLine(cx - 2.6f, cy - 4.2f, cx - 2.6f, cy + 4.2f, iconPaint);
						canvas.DrawLine(cx + 2.6f, cy - 4.2f, cx + 2.6f, cy + 4.2f, iconPaint);
					}
					else
					{
						using var p = new SKPath();
						p.MoveTo(cx - 3.2f, cy - 4.8f);
						p.LineTo(cx + 4.8f, cy);
						p.LineTo(cx - 3.2f, cy + 4.8f);
						p.Close();
						canvas.DrawPath(p, fillPaint);
					}
					break;
				case 2: // Next
					canvas.DrawLine(cx + 4.5f, cy - 4f, cx + 4.5f, cy + 4f, iconPaint);
					using (var p = new SKPath())
					{
						p.MoveTo(cx - 4.5f, cy - 4.5f);
						p.LineTo(cx + 2.5f, cy);
						p.LineTo(cx - 4.5f, cy + 4.5f);
						p.Close();
						canvas.DrawPath(p, fillPaint);
					}
					break;
				case 3: // Reset Position (crosshair / home target)
					canvas.DrawCircle(cx, cy, 4.2f, iconPaint);
					canvas.DrawLine(cx, cy - 6.2f, cx, cy - 2.8f, iconPaint);
					canvas.DrawLine(cx, cy + 2.8f, cx, cy + 6.2f, iconPaint);
					canvas.DrawLine(cx - 6.2f, cy, cx - 2.8f, cy, iconPaint);
					canvas.DrawLine(cx + 2.8f, cy, cx + 6.2f, cy, iconPaint);
					break;
				case 4: // Hide (X)
					canvas.DrawLine(cx - 4f, cy - 4f, cx + 4f, cy + 4f, iconPaint);
					canvas.DrawLine(cx + 4f, cy - 4f, cx - 4f, cy + 4f, iconPaint);
					break;
			}
		}
	}

	private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		try
		{
		switch ((int)msg)
		{
			case WM_NCHITTEST:
			{
				int sx = unchecked((short)(lParam.ToInt64() & 0xFFFF));
				int sy = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
				int lx = sx - _left;
				int ly = sy - _top;
				// 歌词气泡区域始终鼠标穿透
				if (ly < _lyricTopOffset)
				{
					return new IntPtr(HTTRANSPARENT);
				}
				// 看板娘本体区域按水平范围命中，避免宽画布两侧遮挡鼠标
				if (ly < _lyricTopOffset + _petHeight)
				{
					float petX = (_totalWidth - _petWidth) / 2f;
					if (lx < petX || lx >= petX + _petWidth)
						return new IntPtr(HTTRANSPARENT);
					return new IntPtr(HTCLIENT);
				}
				// 下方播控栏区域：仅在显示状态且位于播控栏附近时命中
				if (!_isHovered)
				{
					return new IntPtr(HTTRANSPARENT);
				}
				var barRect = GetBarRect();
				if (lx < barRect.Left - 10 || lx > barRect.Right + 10)
				{
					return new IntPtr(HTTRANSPARENT);
				}
				return new IntPtr(HTCLIENT);
			}

			case WM_TIMER:
				if (wParam == TIMER_ANIM_ID)
				{
					var anim = GetActiveAnimation();
					if (anim != null && anim.Frames.Count > 1)
					{
						_frameIndex = (_frameIndex + 1) % anim.Frames.Count;
						RenderCurrentFrame();
						ScheduleNextAnimTick();
					}
				}
				else if (wParam == TIMER_CLICK_RETURN_ID)
				{
					KillTimer(_hwnd, TIMER_CLICK_RETURN_ID);
					if (_state == PetAnimState.Click)
					{
						_state = _isPlaying ? PetAnimState.Groove : PetAnimState.Idle;
						_frameIndex = 0;
						if (_isVisible)
						{
							RenderCurrentFrame();
							ScheduleNextAnimTick();
						}
					}
				}
				else if (wParam == TIMER_SAVE_POS_ID)
				{
					KillTimer(_hwnd, TIMER_SAVE_POS_ID);
					FlushPendingPositionSave();
				}
				else if (wParam == TIMER_HIDE_BAR_ID)
				{
					KillTimer(_hwnd, TIMER_HIDE_BAR_ID);
					if (!_isPointerDown && !_isDragging && _isHovered)
					{
						GetCursorPos(out var cur);
						int lx = cur.X - _left;
						int ly = cur.Y - _top;
						float petX = (_totalWidth - _petWidth) / 2f;
						bool inPet = lx >= petX && lx < petX + _petWidth && ly >= _lyricTopOffset && ly < _lyricTopOffset + _petHeight;
						var bar = GetBarRect();
						bool inBar = lx >= bar.Left - 10 && lx <= bar.Right + 10 && ly >= _lyricTopOffset + _petHeight && ly < _totalHeight;
						if (!inPet && !inBar)
						{
							_isHovered = false;
							_hoverButtonIndex = -1;
							if (_isVisible) RenderCurrentFrame();
						}
					}
				}
				else if (wParam == TIMER_LYRIC_ANIM_ID)
				{
					long elapsed = Environment.TickCount64 - _lyricAnimStartMs;
					if (elapsed >= LyricAnimDurationMs)
					{
						_isLyricAnimating = false;
						_lyricExiting = null;
						KillTimer(_hwnd, TIMER_LYRIC_ANIM_ID);
					}
					if (_isVisible) RenderCurrentFrame();
				}
				return IntPtr.Zero;

			case WM_MOUSEMOVE:
			{
				KillTimer(_hwnd, TIMER_HIDE_BAR_ID);
				if (!_trackingMouseLeave)
				{
					var tme = new TRACKMOUSEEVENT
					{
						cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
						dwFlags = TME_LEAVE,
						hwndTrack = _hwnd
					};
					TrackMouseEvent(ref tme);
					_trackingMouseLeave = true;
				}

				int lx = unchecked((short)(lParam.ToInt64() & 0xFFFF));
				int ly = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));

				bool needRedraw = !_isHovered;
				_isHovered = true;

				int newHoverBtn = HitTestBarButton(lx, ly);
				if (newHoverBtn != _hoverButtonIndex)
				{
					_hoverButtonIndex = newHoverBtn;
					needRedraw = true;
				}

				if (_isPointerDown && _pressedButtonIndex < 0)
				{
					GetCursorPos(out var cur);
					int dx = cur.X - _dragStartCursor.X;
					int dy = cur.Y - _dragStartCursor.Y;
					if (!_isDragging && (dx * dx + dy * dy) >= DragThresholdPx * DragThresholdPx)
					{
						_isDragging = true;
					}
					if (_isDragging)
					{
						int rawLeft = _dragStartLeft + dx;
						int rawTop = _dragStartTop + dy;
						IntPtr hMon = MonitorFromPoint(cur, MONITOR_DEFAULTTONEAREST);
						var work = GetMonitorWorkArea(hMon);
						(_left, _top) = ClampPetBoundsToWorkArea(rawLeft, rawTop, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);
						SetWindowPos(_hwnd, IntPtr.Zero, _left, _top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
						SchedulePositionSave();
					}
				}

				if (needRedraw && _isVisible)
				{
					RenderCurrentFrame();
				}
				return IntPtr.Zero;
			}

			case WM_MOUSELEAVE:
				_trackingMouseLeave = false;
				if (_hoverButtonIndex != -1)
				{
					_hoverButtonIndex = -1;
					if (_isVisible) RenderCurrentFrame();
				}
				if (_isHovered)
				{
					// 延迟 2.5s 关闭下方播控栏，确保鼠标移向播控按钮时不会提前收起
					SetTimer(_hwnd, TIMER_HIDE_BAR_ID, BarHideDelayMs, IntPtr.Zero);
				}
				return IntPtr.Zero;

			case WM_LBUTTONDOWN:
			{
				int lx = unchecked((short)(lParam.ToInt64() & 0xFFFF));
				int ly = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
				GetCursorPos(out _dragStartCursor);
				_dragStartLeft = _left;
				_dragStartTop = _top;
				_isPointerDown = true;
				_isDragging = false;
				_pressedButtonIndex = HitTestBarButton(lx, ly);
				SetCapture(_hwnd);
				if (_pressedButtonIndex >= 0 && _isVisible)
				{
					RenderCurrentFrame();
				}
				return IntPtr.Zero;
			}

			case WM_LBUTTONUP:
			{
				int lx = unchecked((short)(lParam.ToInt64() & 0xFFFF));
				int ly = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
				ReleaseCapture();

				int pressed = _pressedButtonIndex;
				bool wasDragging = _isDragging;
				bool wasDown = _isPointerDown;

				_pressedButtonIndex = -1;
				_isPointerDown = false;
				_isDragging = false;

				if (pressed >= 0)
				{
					if (_isVisible) RenderCurrentFrame();
					if (HitTestBarButton(lx, ly) == pressed)
					{
						ExecuteBarAction(pressed);
					}
				}
				else if (wasDragging)
				{
					SchedulePositionSave();
				}
				else if (wasDown)
				{
					TriggerClickAnimation();
				}
				return IntPtr.Zero;
			}

			case WM_RBUTTONUP:
				ShowContextMenu();
				return IntPtr.Zero;

			case WM_COMMAND:
			{
				int cmd = wParam.ToInt32() & 0xFFFF;
				switch (cmd)
				{
					case IDM_PREV:
						PreviousRequested?.Invoke();
						break;
					case IDM_PLAYPAUSE:
						PlayPauseRequested?.Invoke();
						break;
					case IDM_NEXT:
						NextRequested?.Invoke();
						break;
					case IDM_RESET_POS:
						ResetPositionRequested?.Invoke();
						break;
					case IDM_HIDE:
						HideRequested?.Invoke();
						break;
					case IDM_TOGGLE_LYRICS:
						ToggleLyricsRequested?.Invoke();
						break;
				}
				return IntPtr.Zero;
			}

			case WM_DISPLAYCHANGE:
			{
				var work = GetWorkAreaForWindowOrPrimary();
				(_left, _top) = ClampPetBoundsToWorkArea(_left, _top, _totalWidth, _totalHeight, work.left, work.top, work.right, work.bottom);
				if (_isVisible)
				{
					SetWindowPos(_hwnd, IntPtr.Zero, _left, _top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
				}
				return IntPtr.Zero;
			}

			case WM_DESTROY:
				return IntPtr.Zero;
		}
		}
		catch (Exception ex)
		{
			CrashReporter.WriteManaged(ex, "FurinaPetWindow.WndProc");
		}

		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	private void ExecuteBarAction(int index)
	{
		switch (index)
		{
			case 0:
				PreviousRequested?.Invoke();
				break;
			case 1:
				PlayPauseRequested?.Invoke();
				break;
			case 2:
				NextRequested?.Invoke();
				break;
			case 3:
				ResetPositionRequested?.Invoke();
				break;
			case 4:
				HideRequested?.Invoke();
				break;
		}
	}

	private void ShowContextMenu()
	{
		if (_hwnd == IntPtr.Zero) return;
		GetCursorPos(out var pt);
		SetForegroundWindow(_hwnd);

		IntPtr hMenu = CreatePopupMenu();
		if (hMenu == IntPtr.Zero) return;

		try
		{
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_PREV, Loc.T("上一曲"));
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_PLAYPAUSE, Loc.T("播放 / 暂停"));
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_NEXT, Loc.T("下一曲"));
			AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_TOGGLE_LYRICS, $"{Loc.T("看板娘歌词气泡")}: {(_lyricsEnabled ? Loc.T("开") : Loc.T("关"))}");
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_RESET_POS, Loc.T("重置位置"));
			AppendMenu(hMenu, MF_STRING, (IntPtr)IDM_HIDE, Loc.T("隐藏看板娘"));

			TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
		}
		finally
		{
			DestroyMenu(hMenu);
		}
	}

	private void SchedulePositionSave()
	{
		_pendingPositionSave = true;
		if (_hwnd != IntPtr.Zero)
		{
			SetTimer(_hwnd, TIMER_SAVE_POS_ID, SaveDebounceMs, IntPtr.Zero);
		}
	}

	private void FlushPendingPositionSave()
	{
		if (!_pendingPositionSave) return;
		_pendingPositionSave = false;
		PositionChanged?.Invoke(_left, _top);
	}

	private RECT GetWorkAreaForPrimary()
	{
		IntPtr hMon = MonitorFromPoint(new POINT { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
		return GetMonitorWorkArea(hMon);
	}

	private RECT GetWorkAreaForWindowOrPrimary()
	{
		IntPtr hMon = _hwnd != IntPtr.Zero
			? MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST)
			: MonitorFromPoint(new POINT { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
		return GetMonitorWorkArea(hMon);
	}

	private static RECT GetMonitorWorkArea(IntPtr hMonitor)
	{
		var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
		if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref mi))
		{
			return mi.rcWork;
		}
		return new RECT { left = 0, top = 0, right = 1920, bottom = 1040 };
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		FlushPendingPositionSave();

		if (_hwnd != IntPtr.Zero)
		{
			KillTimer(_hwnd, TIMER_ANIM_ID);
			KillTimer(_hwnd, TIMER_SAVE_POS_ID);
			KillTimer(_hwnd, TIMER_CLICK_RETURN_ID);
			KillTimer(_hwnd, TIMER_HIDE_BAR_ID);
			KillTimer(_hwnd, TIMER_LYRIC_ANIM_ID);
			DestroyWindow(_hwnd);
			_hwnd = IntPtr.Zero;
		}

		ReleaseSurface();
		_bubbleTl?.Dispose();
		_bubbleTr?.Dispose();
		_bubbleBl?.Dispose();
		_bubbleBr?.Dispose();
		_idleAnim?.Dispose();
		_grooveAnim?.Dispose();
		_clickAnim?.Dispose();

		if (_className != null)
		{
			UnregisterClass(_className, GetModuleHandle(null));
			_className = null;
		}
	}

	#region Win32 Interop

	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct SIZE
	{
		public int cx;
		public int cy;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MONITORINFO
	{
		public int cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BLENDFUNCTION
	{
		public byte BlendOp;
		public byte BlendFlags;
		public byte SourceConstantAlpha;
		public byte AlphaFormat;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BITMAPINFOHEADER
	{
		public int biSize;
		public int biWidth;
		public int biHeight;
		public short biPlanes;
		public short biBitCount;
		public int biCompression;
		public int biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public int biClrUsed;
		public int biClrImportant;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BITMAPINFO
	{
		public BITMAPINFOHEADER bmiHeader;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct TRACKMOUSEEVENT
	{
		public int cbSize;
		public uint dwFlags;
		public IntPtr hwndTrack;
		public uint dwHoverTime;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct WNDCLASSEX
	{
		public int cbSize;
		public int style;
		public IntPtr lpfnWndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		public string? lpszMenuName;
		public string lpszClassName;
		public IntPtr hIconSm;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr GetModuleHandle(string? lpModuleName);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

	[DllImport("user32.dll")]
	private static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern IntPtr SetCapture(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	[DllImport("user32.dll")]
	private static extern IntPtr CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string lpNewItem);

	[DllImport("user32.dll")]
	private static extern bool TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(IntPtr hMenu);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr ho);

	#endregion
}
