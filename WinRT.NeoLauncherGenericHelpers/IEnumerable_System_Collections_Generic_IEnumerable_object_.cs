using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections.Generic;

namespace WinRT.NeoLauncherGenericHelpers;

internal static class IEnumerable_System_Collections_Generic_IEnumerable_object_
{
	private static readonly bool _initialized = Init();

	internal static bool Initialized => _initialized;

	private unsafe static bool Init()
	{
		return IEnumerableMethods<IEnumerable<object>, nint>.InitCcw((delegate* unmanaged[Stdcall]<nint, nint*, int>)(&Do_Abi_First_0));
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private unsafe static int Do_Abi_First_0(nint thisPtr, nint* __return_value__)
	{
		*__return_value__ = 0;
		try
		{
			*__return_value__ = MarshalInterface<IEnumerator<IEnumerable<object>>>.FromManaged(IEnumerableMethods<IEnumerable<object>>.Abi_First_0(thisPtr));
		}
		catch (Exception ex)
		{
			ExceptionHelpers.SetErrorInfo(ex);
			return ExceptionHelpers.GetHRForException(ex);
		}
		return 0;
	}
}
