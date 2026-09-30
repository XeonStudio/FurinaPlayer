using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace FurinaPlayer.UI.FurinaPlayer_UI_XamlTypeInfo;

internal sealed class XamlUserTypeWinRTTypeDetails : IWinRTExposedTypeDetails
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
