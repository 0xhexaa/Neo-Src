using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

internal static class IReadOnlyDictionary_string_string
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IReadOnlyDictionaryMethods<string, nint, string, nint>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(&Do_Abi_Lookup_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint, byte*, int>)(&Do_Abi_HasKey_2), (delegate* unmanaged[Stdcall]<nint, nint*, nint*, int>)(&Do_Abi_Split_3));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_Lookup_0(nint thisPtr, nint key, nint* __return_value__)
	{
		string text = null;
		*__return_value__ = 0;
		try
		{
			text = IReadOnlyDictionaryMethods<string, string>.Abi_Lookup_0(thisPtr, MarshalString.FromAbi(key));
			*__return_value__ = MarshalString.FromManaged(text);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_HasKey_2(nint thisPtr, nint key, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IReadOnlyDictionaryMethods<string, string>.Abi_HasKey_2(thisPtr, MarshalString.FromAbi(key));
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_Split_3(nint thisPtr, nint* first, nint* second)
	{
		*first = 0;
		*second = 0;
		nint first2 = 0;
		nint second2 = 0;
		try
		{
			IReadOnlyDictionaryMethods<string, string>.Abi_Split_3(thisPtr, out first2, out second2);
			*first = first2;
			*second = second2;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Size_1(nint thisPtr, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		try
		{
			num = IReadOnlyDictionaryMethods<string, string>.Abi_get_Size_1(thisPtr);
			*__return_value__ = num;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
