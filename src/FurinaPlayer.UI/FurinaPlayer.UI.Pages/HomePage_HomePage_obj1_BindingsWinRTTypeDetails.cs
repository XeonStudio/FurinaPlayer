using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace FurinaPlayer.UI.Pages;

internal sealed class HomePage_HomePage_obj1_BindingsWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[1]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
