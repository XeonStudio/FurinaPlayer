namespace SonicWave.Audio.Core;

public enum PlaybackErrorKind
{
	None,
	FileNotFound,
	FileUnreadable,
	Decode,
	Output,
	SampleRate,
	NativeHost,
	UserConflict,
	System,
	Unknown
}
