using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

internal static class IList_byte
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IListMethods<byte, byte>.InitCcw((delegate* unmanaged[Stdcall]<nint, uint, byte*, int>)(&Do_Abi_GetAt_0), (delegate* unmanaged[Stdcall]<nint, uint*, int>)(&Do_Abi_get_Size_1), (delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_GetView_2), (delegate* unmanaged[Stdcall]<nint, byte, uint*, byte*, int>)(&Do_Abi_IndexOf_3), (delegate* unmanaged[Stdcall]<nint, uint, byte, int>)(&Do_Abi_SetAt_4), (delegate* unmanaged[Stdcall]<nint, uint, byte, int>)(&Do_Abi_InsertAt_5), (delegate* unmanaged[Stdcall]<nint, uint, int>)(&Do_Abi_RemoveAt_6), (delegate* unmanaged[Stdcall]<nint, byte, int>)(&Do_Abi_Append_7), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_RemoveAtEnd_8), (delegate* unmanaged[Stdcall]<nint, int>)(&Do_Abi_Clear_9), (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint*, int>)(&Do_Abi_GetMany_10), (delegate* unmanaged[Stdcall]<nint, int, nint, int>)(&Do_Abi_ReplaceAll_11));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetAt_0(nint thisPtr, uint index, byte* __return_value__)
	{
		byte b = 0;
		*__return_value__ = 0;
		try
		{
			b = IListMethods<byte>.Abi_GetAt_0(thisPtr, index);
			*__return_value__ = b;
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetView_2(nint thisPtr, nint* __return_value__)
	{
		IReadOnlyList<byte> readOnlyList = null;
		*__return_value__ = 0;
		try
		{
			readOnlyList = IListMethods<byte>.Abi_GetView_2(thisPtr);
			*__return_value__ = MarshalInterface<IReadOnlyList<byte>>.FromManaged(readOnlyList);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_IndexOf_3(nint thisPtr, byte value, uint* index, byte* __return_value__)
	{
		bool flag = false;
		*index = 0u;
		*__return_value__ = 0;
		uint index2 = 0u;
		try
		{
			flag = IListMethods<byte>.Abi_IndexOf_3(thisPtr, value, out index2);
			*index = index2;
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
	private static int Do_Abi_SetAt_4(nint thisPtr, uint index, byte value)
	{
		try
		{
			IListMethods<byte>.Abi_SetAt_4(thisPtr, index, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_InsertAt_5(nint thisPtr, uint index, byte value)
	{
		try
		{
			IListMethods<byte>.Abi_InsertAt_5(thisPtr, index, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAt_6(nint thisPtr, uint index)
	{
		try
		{
			IListMethods<byte>.Abi_RemoveAt_6(thisPtr, index);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Append_7(nint thisPtr, byte value)
	{
		try
		{
			IListMethods<byte>.Abi_Append_7(thisPtr, value);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_RemoveAtEnd_8(nint thisPtr)
	{
		try
		{
			IListMethods<byte>.Abi_RemoveAtEnd_8(thisPtr);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static int Do_Abi_Clear_9(nint thisPtr)
	{
		try
		{
			IListMethods<byte>.Abi_Clear_9(thisPtr);
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_GetMany_10(nint thisPtr, uint startIndex, int __itemsSize, nint items, uint* __return_value__)
	{
		uint num = 0u;
		*__return_value__ = 0u;
		byte[] items2 = MarshalBlittable<byte>.FromAbiArray((__itemsSize, items));
		try
		{
			num = IListMethods<byte>.Abi_GetMany_10(thisPtr, startIndex, ref items2);
			MarshalBlittable<byte>.CopyManagedArray(items2, items);
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
	private static int Do_Abi_ReplaceAll_11(nint thisPtr, int __itemsSize, nint items)
	{
		try
		{
			IListMethods<byte>.Abi_ReplaceAll_11(thisPtr, MarshalBlittable<byte>.FromAbiArray((__itemsSize, items)));
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
			num = IListMethods<byte>.Abi_get_Size_1(thisPtr);
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
