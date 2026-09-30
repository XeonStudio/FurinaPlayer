using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("DCD7BBE3-7742-448D-A874-AACC979C759E")]
internal partial interface IEditController
{
	[PreserveSig]
	int Initialize(nint context);

	[PreserveSig]
	int Terminate();

	[PreserveSig]
	int SetComponentState(nint stream);

	[PreserveSig]
	int SetState(nint stream);

	[PreserveSig]
	int GetState(nint stream);

	[PreserveSig]
	int GetParameterCount();

	[PreserveSig]
	int GetParameterInfo(int paramIndex, nint info);

	[PreserveSig]
	int GetParamStringByValue(uint id, double valueNormalized, nint str);

	[PreserveSig]
	int GetParamValueByString(uint id, nint str, out double valueNormalized);

	[PreserveSig]
	double NormalizedParamToPlain(uint id, double valueNormalized);

	[PreserveSig]
	double PlainParamToNormalized(uint id, double plainValue);

	[PreserveSig]
	double GetParamNormalized(uint id);

	[PreserveSig]
	int SetParamNormalized(uint id, double value);

	[PreserveSig]
	int SetComponentHandler(nint handler);

	[PreserveSig]
	nint CreateView(nint name);
}


