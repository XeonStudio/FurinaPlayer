using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace SonicWave.App.SonicWave_App_XamlTypeInfo;

internal sealed class XamlMemberWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IXamlMemberMethods.IID,
				Vtable = IXamlMemberMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
