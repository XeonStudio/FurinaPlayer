using System;

namespace FurinaPlayer.Core.Services;

public static class DesktopPetLayout
{
	public const int DefaultMarginPx = 28;

	/// <summary>
	/// Clamps desktop pet window coordinates to the given monitor work area,
	/// defaulting to bottom-right with a 28px margin when saved coordinates are null.
	/// </summary>
	public static (int Left, int Top) ClampPetBoundsToWorkArea(
		int? savedLeft,
		int? savedTop,
		int width,
		int height,
		int workLeft,
		int workTop,
		int workRight,
		int workBottom)
	{
		int w = Math.Max(1, width);
		int h = Math.Max(1, height);
		int maxLeft = Math.Max(workLeft, workRight - w);
		int maxTop = Math.Max(workTop, workBottom - h);

		int targetLeft = savedLeft ?? (workRight - w - DefaultMarginPx);
		int targetTop = savedTop ?? (workBottom - h - DefaultMarginPx);

		int clampedLeft = Math.Clamp(targetLeft, workLeft, maxLeft);
		int clampedTop = Math.Clamp(targetTop, workTop, maxTop);
		return (clampedLeft, clampedTop);
	}
}
