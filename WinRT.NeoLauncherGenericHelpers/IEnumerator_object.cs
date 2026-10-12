using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

internal static class IEnumerator_object
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IEnumeratorMethods<object, nint>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_get_Current_0), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(&Do_Abi_get_HasCurrent_1), (delegate* unmanaged[Stdcall]<nint, byte*, int>)(&Do_Abi_MoveNext_2), (delegate* unmanaged[Stdcall]<nint, int, nint, uint*, int>)(&Do_Abi_GetMany_3));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_MoveNext_2(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<object>.Abi_MoveNext_2(thisPtr);
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
	private unsafe static int Do_Abi_GetMany_3(nint thisPtr, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		object[] items2 = MarshalInspectable<object>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IEnumeratorMethods<object>.Abi_GetMany_3(thisPtr, ref items2);
			Marshaler<object>.CopyManagedArray(items2, items);
			*__return_value__ = num;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_Current_0(nint thisPtr, nint* __return_value__)
	{
		object obj = null;
		*__return_value__ = 0;
		try
		{
			obj = IEnumeratorMethods<object>.Abi_get_Current_0(thisPtr);
			*__return_value__ = MarshalInspectable<object>.FromManaged(obj);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_get_HasCurrent_1(nint thisPtr, byte* __return_value__)
	{
		bool flag = false;
		*__return_value__ = 0;
		try
		{
			flag = IEnumeratorMethods<object>.Abi_get_HasCurrent_1(thisPtr);
			*__return_value__ = (flag ? ((byte)1) : ((byte)0));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
