using System;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;

namespace FurinaPlayer.UI;

public static class WindowManager
{
	public static Window? CurrentWindow { get; set; }
	public static DspViewModel? SharedDspViewModel { get; set; }
	public static Action? StageGlassInvalidateRequested { get; set; }
	public static Action<bool>? FullscreenSlideshowRequested { get; set; }

	public static void InvalidateStageGlass()
	{
		StageGlassInvalidateRequested?.Invoke();
	}
}
