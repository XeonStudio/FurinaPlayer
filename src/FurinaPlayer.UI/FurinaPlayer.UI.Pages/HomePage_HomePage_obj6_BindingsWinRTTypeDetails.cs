using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace FurinaPlayer.UI.Pages;

internal sealed class HomePage_HomePage_obj6_BindingsWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[3]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IDataTemplateExtensionMethods.IID,
				Vtable = IDataTemplateExtensionMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IDataTemplateComponentMethods.IID,
				Vtable = IDataTemplateComponentMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
