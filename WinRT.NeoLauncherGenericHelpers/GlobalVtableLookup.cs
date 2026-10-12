using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using ABI.System.Collections;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

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
		case "System.String[]":
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
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
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
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Text.Json.Nodes.JsonNode]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.Generic.List`1[NeoLauncher.Models.Services.Launcher.BuildInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[NeoLauncher.Models.Services.Launcher.BuildInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[NeoLauncher.Models.Services.Launcher.ReleaseInfo]":
		case "System.Collections.Generic.List`1[NeoLauncher.Models.Services.Launcher.ReleaseInfo]":
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
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Text.Json.Nodes.JsonArray":
		case "System.Text.Json.Nodes.JsonObject":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Text.Json.Nodes.JsonNode]":
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
		case "System.Byte[]":
			_ = IList_byte.Initialized;
			_ = IReadOnlyList_byte.Initialized;
			_ = IReadOnlyList_object.Initialized;
			_ = IEnumerable_byte.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[7]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods.IID,
					Vtable = IListMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IListMethods<byte>.IID,
					Vtable = IListMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<byte>.IID,
					Vtable = IReadOnlyListMethods<byte>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyListMethods<object>.IID,
					Vtable = IReadOnlyListMethods<object>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<byte>.IID,
					Vtable = IEnumerableMethods<byte>.AbiToProjectionVftablePtr
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
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
			_ = IDictionary_string_string.Initialized;
			_ = IReadOnlyDictionary_string_string.Initialized;
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[5]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDictionaryMethods<string, string>.IID,
					Vtable = IDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
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
		case "System.Threading.Tasks.Task":
		case "System.Threading.Tasks.Task`1[System.Object]":
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]":
			_ = IEnumerator_byte.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<byte>.IID,
					Vtable = IEnumeratorMethods<byte>.AbiToProjectionVftablePtr
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
		case "NeoLauncher.Models.Services.Prism.AssetInfo[]":
		case "NeoLauncher.Services.Friends.NeoPresenceView[]":
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
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.String]":
			_ = KeyValuePair_string_string.Initialized;
			return new ComWrappers.ComInterfaceEntry[1]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = KeyValuePairMethods<string, string>.IID,
					Vtable = KeyValuePairMethods<string, string>.AbiToProjectionVftablePtr
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
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.String]]":
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerator_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerator_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[3]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumeratorMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
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
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.String]":
			_ = IReadOnlyDictionary_string_string.Initialized;
			_ = KeyValuePair_string_string.Initialized;
			_ = IEnumerable_System_Collections_Generic_KeyValuePair_string__string_.Initialized;
			_ = IEnumerable_object.Initialized;
			return new ComWrappers.ComInterfaceEntry[4]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IReadOnlyDictionaryMethods<string, string>.IID,
					Vtable = IReadOnlyDictionaryMethods<string, string>.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.IID,
					Vtable = IEnumerableMethods<System.Collections.Generic.KeyValuePair<string, string>>.AbiToProjectionVftablePtr
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
		case "Microsoft.Extensions.DependencyInjection.ServiceProvider":
			return new ComWrappers.ComInterfaceEntry[2]
			{
				new ComWrappers.ComInterfaceEntry
				{
					IID = IServiceProviderMethods.IID,
					Vtable = IServiceProviderMethods.AbiToProjectionVftablePtr
				},
				new ComWrappers.ComInterfaceEntry
				{
					IID = IDisposableMethods.IID,
					Vtable = IDisposableMethods.AbiToProjectionVftablePtr
				}
			};
		case "System.Collections.Generic.HashSet`1[System.String]":
			_ = IEnumerable_string.Initialized;
			_ = IEnumerable_char.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_char_.Initialized;
			_ = IEnumerable_object.Initialized;
			_ = IEnumerable_System_Collections_Generic_IEnumerable_object_.Initialized;
			_ = IEnumerable_System_Collections_IEnumerable.Initialized;
			return new ComWrappers.ComInterfaceEntry[6]
			{
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
					IID = IEnumerableMethods.IID,
					Vtable = IEnumerableMethods.AbiToProjectionVftablePtr
				}
			};
		default:
			return null;
		}
	}

	private static string LookupRuntimeClassName(System.Type type)
	{
		switch (type.ToString())
		{
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Object]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>";
		case "System.String[]":
		case "System.Byte[]":
		case "NeoLauncher.Models.Services.Prism.AssetInfo[]":
		case "NeoLauncher.Services.Friends.NeoPresenceView[]":
			return "Microsoft.UI.Xaml.Interop.IBindableVector";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Text.Json.Nodes.JsonNode]]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.Generic.List`1[NeoLauncher.Models.Services.Launcher.BuildInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[NeoLauncher.Models.Services.Launcher.BuildInfo]":
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[NeoLauncher.Models.Services.Launcher.ReleaseInfo]":
		case "System.Collections.Generic.List`1[NeoLauncher.Models.Services.Launcher.ReleaseInfo]":
			return "Windows.Foundation.Collections.IVectorView`1<Object>";
		case "System.Collections.ObjectModel.ReadOnlyCollection`1[System.String]":
		case "System.Collections.Generic.List`1[System.String]":
			return "Windows.Foundation.Collections.IVector`1<String>";
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.Nodes.JsonNode]":
		case "System.Text.Json.Nodes.JsonArray":
		case "System.Text.Json.Nodes.JsonObject":
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.Text.Json.Nodes.JsonNode]":
			return "Windows.Foundation.Collections.IIterable`1<Object>";
		case "System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.String]":
		case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMap`2<String, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]":
			return "Windows.Foundation.Collections.IIterator`1<Object>";
		case "System.Threading.Tasks.Task":
		case "System.Threading.Tasks.Task`1[System.Object]":
			return "Windows.Foundation.IClosable";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]":
			return "Windows.Foundation.Collections.IIterator`1<UInt8>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.IEnumerable]":
			return "Windows.Foundation.Collections.IIterator`1<Microsoft.UI.Xaml.Interop.IBindableIterable>";
		case "System.Collections.Generic.KeyValuePair`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IKeyValuePair`2<String, String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]":
			return "Windows.Foundation.Collections.IIterator`1<String>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.KeyValuePair`2[System.String,System.String]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IKeyValuePair`2<String, String>>";
		case "ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Collections.Generic.IEnumerable`1[System.Char]]":
			return "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Char>>";
		case "ABI.System.Collections.Generic.ConstantSplittableMap`2[System.String,System.String]":
			return "Windows.Foundation.Collections.IMapView`2<String, String>";
		case "Microsoft.Extensions.DependencyInjection.ServiceProvider":
			return "Microsoft.UI.Xaml.IXamlServiceProvider";
		case "System.Collections.Generic.HashSet`1[System.String]":
			return "Windows.Foundation.Collections.IIterable`1<String>";
		default:
			return null;
		}
	}
}
