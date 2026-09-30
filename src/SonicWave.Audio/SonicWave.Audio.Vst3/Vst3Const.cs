using System.Text;

namespace SonicWave.Audio.Vst3;

internal static class Vst3Const
{
	public const int MediaAudio = 0;

	public const int BusInput = 0;

	public const int BusOutput = 1;

	public const int BusMain = 0;

	public const int ProcessModeRealtime = 0;

	public const int SampleSize32 = 0;

	public const int DefaultMaxBlock = 8192;

	public const uint LoadWithAlteredSearchPath = 8u;

	public static readonly byte[] PlatformHwnd = Encoding.ASCII.GetBytes("HWND\0");

	public static readonly byte[] EditorName = Encoding.ASCII.GetBytes("editor\0");

	public static readonly byte[] AudioEffectCategory = Encoding.ASCII.GetBytes("Audio Module Effect");

	public static readonly byte[] FactoryProc = Encoding.ASCII.GetBytes("GetPluginFactory");
}
