using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SonicWave.App;

/// <summary>
/// 仿照 ECHO 实现的系统电源与设备唤醒管理器：
/// 在音乐高保真播放期间调用 Windows API 阻止系统自动进入睡眠/休眠，
/// 暂停、停止或应用退出时自动恢复 Windows 默认节能策略。
/// </summary>
public static class PowerWakeManager
{
	[Flags]
	public enum ExecutionState : uint
	{
		ES_SYSTEM_REQUIRED = 0x00000001,
		ES_DISPLAY_REQUIRED = 0x00000002,
		ES_USER_PRESENT = 0x00000004,
		ES_AWAYMODE_REQUIRED = 0x00000040,
		ES_CONTINUOUS = 0x80000000
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

	private static bool _isWakeLocked = false;

	public static void SetKeepAwake(bool enable)
	{
		try
		{
			if (enable)
			{
				if (!_isWakeLocked)
				{
					// 持续阻止系统休眠，保持音频后台流式处理稳定
					SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED);
					_isWakeLocked = true;
					Debug.WriteLine("[PowerWakeManager] System keep-awake enabled during playback.");
				}
			}
			else
			{
				if (_isWakeLocked)
				{
					// 恢复系统默认电源策略
					SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
					_isWakeLocked = false;
					Debug.WriteLine("[PowerWakeManager] System keep-awake released.");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[PowerWakeManager] Error toggling execution state: {ex.Message}");
		}
	}
}
