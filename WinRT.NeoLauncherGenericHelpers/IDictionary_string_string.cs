using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

internal static class IDictionary_string_string
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IDictionaryMethods<string, nint, string, nint>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(&Do_Abi_Lookup_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint, byte*, int>)(&Do_Abi_HasKey_2), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_GetView_3), (delegate* unmanaged[Stdcall]<nint, nint, nint, byte*, int>)(&Do_Abi_Insert_4), (delegate* unmanaged[Stdcall]<nint, nint, int>)(&Do_Abi_Remove_5), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_Clear_6));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_Lookup_0(nint thisPtr, nint key, nint* __return_value__)
	{
		string text = null;
		*__return_value__ = 0;
		try
		{
			text = IDictionaryMethods<string, string>.Abi_Lookup_0(thisPtr, MarshalString.FromAbi(key));
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
			flag = IDictionaryMethods<string, string>.Abi_HasKey_2(thisPtr, MarshalString.FromAbi(key));
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
	private unsafe static int Do_Abi_GetView_3(nint thisPtr, nint* __return_value__)
	{
		IReadOnlyDictionary<string, string> readOnlyDictionary = null;
		*__return_value__ = 0;
		try
		{
			readOnlyDictionary = IDictionaryMethods<string, string>.Abi_GetView_3(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyDictionary<string, string>>.FromManaged(readOnlyDictionary);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_Insert_4(nint thisPtr, nint key, nint value, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IDictionaryMethods<string, string>.Abi_Insert_4(thisPtr, MarshalString.FromAbi(key), MarshalString.FromAbi(value));
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
	private static int Do_Abi_Remove_5(nint thisPtr, nint key)
	{
		try
		{
			IDictionaryMethods<string, string>.Abi_Remove_5(thisPtr, MarshalString.FromAbi(key));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Clear_6(nint thisPtr)
	{
		try
		{
			IDictionaryMethods<string, string>.Abi_Clear_6(thisPtr);
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
			num = IDictionaryMethods<string, string>.Abi_get_Size_1(thisPtr);
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
