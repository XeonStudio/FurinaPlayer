using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace SonicWave.App.SonicWave_App_XamlTypeInfo;

internal sealed class XamlSystemBaseTypeWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IXamlTypeMethods.IID,
				Vtable = IXamlTypeMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
