using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Composition;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Controls;
using ABI.Microsoft.UI.Xaml.Markup;
using WinRT;

namespace FurinaPlayer.UI.Pages;

internal sealed class HomePageWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[8]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IUIElementOverridesMethods.IID,
				Vtable = IUIElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IAnimationObjectMethods.IID,
				Vtable = IAnimationObjectMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IVisualElementMethods.IID,
				Vtable = IVisualElementMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IVisualElement2Methods.IID,
				Vtable = IVisualElement2Methods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IFrameworkElementOverridesMethods.IID,
				Vtable = IFrameworkElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IControlOverridesMethods.IID,
				Vtable = IControlOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IPageOverridesMethods.IID,
				Vtable = IPageOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
