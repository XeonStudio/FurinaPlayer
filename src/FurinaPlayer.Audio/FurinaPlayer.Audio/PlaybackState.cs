namespace FurinaPlayer.Audio;

public enum PlaybackState
{
	Idle,
	Loading,
	Playing,
	Paused,
	Seeking,
	Buffering,
	Ended,
	Stopped,
	Error
}
