using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections;
using ABI.System.Collections.Generic;

namespace WinRT.SonicWave_AudioGenericHelpers;

internal static class GlobalVtableLookup
{
	[ModuleInitializer]
	internal static void InitializeGlobalVtableLookup()
	{
		ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup(LookupVtableEntries);
		ComWrappersSupport.RegisterTypeRuntimeClassNameLookup(LookupRuntimeClassName);
	}

	private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(System.Type type)
	{
		switch (type.ToString())
		{
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IEnumerable]":
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Char]]":
			_ = IEnumerable_char.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<char>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.List`1[System.String]":
			_ = IList_string.Initialized;
			_ = IReadOnlyList_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[13]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<string>.IID,
					Vtable = IListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<string>.IID,
					Vtable = IReadOnlyListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<char>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<string>.IID,
					Vtable = IEnumerableMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<char>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[SonicWave.Core.Models.Track]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]":
			_ = IEnumerator_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[6]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<string>.IID,
					Vtable = IEnumeratorMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<char>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.List`1[SonicWave.Core.Models.Track]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]":
			_ = IList_string.Initialized;
			_ = IReadOnlyList_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IReadOnlyList_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IReadOnlyList_System_Collections_IEnumerable.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[13]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<string>.IID,
					Vtable = IListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<string>.IID,
					Vtable = IReadOnlyListMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<char>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable<object>>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<IEnumerable>.IID,
					Vtable = IReadOnlyListMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<string>.IID,
					Vtable = IEnumerableMethods<string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<char>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<char>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable<object>>.IID,
					Vtable = IEnumerableMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<IEnumerable>.IID,
					Vtable = IEnumerableMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Double[]]":
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.Dictionary`2[System.String,System.Double[]]":
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "SonicWave.Core.Models.Vst3PluginState[]":
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Object]]":
			_ = IEnumerable_object.Initialized;
			_ = IEnumerator_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerator_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable<object>>.IID,
					Vtable = IEnumeratorMethods<IEnumerable<object>>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<IEnumerable>.IID,
					Vtable = IEnumeratorMethods<IEnumerable>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "NAudio.Wave.AsioOut":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "NAudio.Wave.WasapiOut":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Double[]]":
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<object>.IID,
					Vtable = IEnumerableMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]":
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<object>.IID,
					Vtable = IEnumeratorMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "NAudio.Wave.WaveOutEvent":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		default:
			return null;
		}
	}

	private static string LookupRuntimeClassName(System.Type type)
	{
		return type.ToString() switch
		{
			"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IEnumerable]" => "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.Interop.IBindableIterable>", 
			"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Char]]" => "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Char>>", 
			"System.Collections.Generic.List`1[System.String]" => "Windows.Foundation.Collections.IVector`1<String>", 
			"System.Collections.ObjectModel.ReadOnlyCollection`1[SonicWave.Core.Models.Track]" => "Windows.Foundation.Collections.IVectorView`1<Object>", 
			"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]" => "Windows.Foundation.Collections.IIterator`1<String>", 
			"System.Collections.Generic.List`1[SonicWave.Core.Models.Track]" => "Windows.Foundation.Collections.IVectorView`1<Object>", 
			"System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]" => "Windows.Foundation.Collections.IVector`1<String>", 
			"ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Double[]]" => "Windows.Foundation.Collections.IIterable`1<Object>", 
			"System.Collections.Generic.Dictionary`2[System.String,System.Double[]]" => "Windows.Foundation.Collections.IIterable`1<Object>", 
			"SonicWave.Core.Models.Vst3PluginState[]" => "Microsoft.UI.Xaml.Interop.IBindableVector", 
			"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Object]]" => "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>", 
			"NAudio.Wave.AsioOut" => "Windows.Foundation.IClosable", 
			"NAudio.Wave.WasapiOut" => "Windows.Foundation.IClosable", 
			"System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Double[]]" => "Windows.Foundation.Collections.IIterable`1<Object>", 
			"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]" => "Windows.Foundation.Collections.IIterator`1<Object>", 
			"NAudio.Wave.WaveOutEvent" => "Windows.Foundation.IClosable", 
			_ => null, 
		};
	}
}
