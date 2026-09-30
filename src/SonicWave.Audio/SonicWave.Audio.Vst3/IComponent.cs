using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("E831FF31-F2D5-4301-928E-BBEE25697802")]
internal partial interface IComponent
{
	[PreserveSig]
	int Initialize(nint context);

	[PreserveSig]
	int Terminate();

	[PreserveSig]
	int GetControllerClassId(nint classId);

	[PreserveSig]
	int SetIoMode(int mode);

	[PreserveSig]
	int GetBusCount(int mediaType, int direction);

	[PreserveSig]
	int GetBusInfo(int mediaType, int direction, int index, nint bus);

	[PreserveSig]
	int GetRoutingInfo(nint inInfo, nint outInfo);

	[PreserveSig]
	int ActivateBus(int mediaType, int direction, int index, byte state);

	[PreserveSig]
	int SetActive(byte state);

	[PreserveSig]
	int SetState(nint stream);

	[PreserveSig]
	int GetState(nint stream);
}


