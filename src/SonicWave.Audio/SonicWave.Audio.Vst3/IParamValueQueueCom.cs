using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("01263A18-ED07-4F6F-98C9-D3564686F9BA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IParamValueQueueCom
{
	[PreserveSig]
	uint GetParameterId();

	[PreserveSig]
	int GetPointCount();

	[PreserveSig]
	int GetPoint(int index, out int sampleOffset, out double value);

	[PreserveSig]
	int AddPoint(int sampleOffset, double value, out int index);
}


