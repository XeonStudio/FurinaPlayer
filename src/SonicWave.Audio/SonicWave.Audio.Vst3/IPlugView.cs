using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("5BC32507-D060-49EA-A615-1B522B755B29")]
internal partial interface IPlugView
{
	[PreserveSig]
	int IsPlatformTypeSupported(nint type);

	[PreserveSig]
	int Attached(nint parent, nint type);

	[PreserveSig]
	int Removed();

	[PreserveSig]
	int OnWheel(float distance);

	[PreserveSig]
	int OnKeyDown(ushort key, short keyCode, short modifiers);

	[PreserveSig]
	int OnKeyUp(ushort key, short keyCode, short modifiers);

	[PreserveSig]
	int GetSize(nint size);

	[PreserveSig]
	int OnSize(nint newSize);

	[PreserveSig]
	int OnFocus(byte state);

	[PreserveSig]
	int SetFrame(nint frame);

	[PreserveSig]
	int CanResize();

	[PreserveSig]
	int CheckSizeConstraint(nint rect);
}


