using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Composition;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Data;

namespace WinRT.FurinaPlayerGenericHelpers;

internal static class GlobalVtableLookup
{
	[ModuleInitializer]
	internal static void InitializeGlobalVtableLookup()
	{
		ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup(LookupVtableEntries);
		ComWrappersSupport.RegisterTypeRuntimeClassNameLookup(LookupRuntimeClassName);
	}

	private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(Type type)
	{
		string text = type.ToString();
		if (text == "FurinaPlayer.UI.Converters.PathToImageConverter")
		{
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IValueConverterMethods.IID,
					Vtable = IValueConverterMethods.AbiToProjectionVftablePtr
				}
			};
		}
		if (text == "SkiaSharp.Views.Windows.SKXamlCanvas")
		{
			return new ComWrappers.ComInterfaceEntry[5]
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
				}
			};
		}
		return null;
	}

	private static string LookupRuntimeClassName(Type type)
	{
		string text = type.ToString();
		if (text == "FurinaPlayer.UI.Converters.PathToImageConverter")
		{
			return "Microsoft.UI.Xaml.Data.IValueConverter";
		}
		if (text == "SkiaSharp.Views.Windows.SKXamlCanvas")
		{
			return "Microsoft.UI.Xaml.IUIElementOverrides";
		}
		return null;
	}
}
