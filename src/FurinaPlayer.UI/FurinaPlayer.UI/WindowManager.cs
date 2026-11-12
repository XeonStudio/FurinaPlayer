using System;
using Microsoft.UI.Xaml;

namespace FurinaPlayer.UI;

public static class WindowManager
{
	public static Window? CurrentWindow { get; set; }
	public static Action? StageGlassInvalidateRequested { get; set; }

	public static void InvalidateStageGlass()
	{
		StageGlassInvalidateRequested?.Invoke();
	}
}
