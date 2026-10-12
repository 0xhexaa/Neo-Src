using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal static class PInvoke
{
	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL CloseHandle(HANDLE hObject);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL CreateProcess([Optional] string lpApplicationName, ref Span<char> lpCommandLine, [Optional] SECURITY_ATTRIBUTES? lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] void* lpEnvironment, [Optional] string lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
	{
		//The blocks IL_0076, IL_00a3, IL_00a7, IL_00aa, IL_00b3, IL_00b7, IL_00ba are reachable both inside and outside the pinned region starting at IL_0071. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (lpCommandLine != null && ((ReadOnlySpan<char>)lpCommandLine).LastIndexOf('\0') == -1)
		{
			throw new ArgumentException("Required null terminator missing.", "lpCommandLine");
		}
		fixed (PROCESS_INFORMATION* lpProcessInformation2 = &lpProcessInformation)
		{
			fixed (STARTUPINFOW* lpStartupInfo2 = &lpStartupInfo)
			{
				fixed (char* ptr = lpCurrentDirectory)
				{
					fixed (char* ptr2 = lpCommandLine)
					{
						char* uIntPtr;
						SECURITY_ATTRIBUTES valueOrDefault;
						SECURITY_ATTRIBUTES valueOrDefault2;
						PCWSTR lpApplicationName2;
						PWSTR lpCommandLine2;
						UIntPtr lpProcessAttributes2;
						UIntPtr lpThreadAttributes2;
						BOOL result;
						char* ptr4;
						PWSTR pWSTR;
						if (lpApplicationName != null)
						{
							fixed (char* ptr3 = &lpApplicationName.GetPinnableReference())
							{
								uIntPtr = (ptr4 = ptr3);
								pWSTR = ptr2;
								valueOrDefault = lpProcessAttributes.GetValueOrDefault();
								valueOrDefault2 = lpThreadAttributes.GetValueOrDefault();
								lpApplicationName2 = ptr4;
								lpCommandLine2 = pWSTR;
								lpProcessAttributes2 = (UIntPtr)(lpProcessAttributes.HasValue ? (&valueOrDefault) : null);
								lpThreadAttributes2 = (UIntPtr)(lpThreadAttributes.HasValue ? (&valueOrDefault2) : null);
								result = CreateProcess(lpApplicationName2, lpCommandLine2, (SECURITY_ATTRIBUTES*)lpProcessAttributes2, (SECURITY_ATTRIBUTES*)lpThreadAttributes2, bInheritHandles, dwCreationFlags, lpEnvironment, ptr, lpStartupInfo2, lpProcessInformation2);
								lpCommandLine = lpCommandLine.Slice(0, pWSTR.Length);
								return result;
							}
						}
						uIntPtr = (ptr4 = null);
						pWSTR = ptr2;
						valueOrDefault = lpProcessAttributes.GetValueOrDefault();
						valueOrDefault2 = lpThreadAttributes.GetValueOrDefault();
						lpApplicationName2 = ptr4;
						lpCommandLine2 = pWSTR;
						lpProcessAttributes2 = (UIntPtr)(lpProcessAttributes.HasValue ? (&valueOrDefault) : null);
						lpThreadAttributes2 = (UIntPtr)(lpThreadAttributes.HasValue ? (&valueOrDefault2) : null);
						result = CreateProcess(lpApplicationName2, lpCommandLine2, (SECURITY_ATTRIBUTES*)lpProcessAttributes2, (SECURITY_ATTRIBUTES*)lpThreadAttributes2, bInheritHandles, dwCreationFlags, lpEnvironment, ptr, lpStartupInfo2, lpProcessInformation2);
						lpCommandLine = lpCommandLine.Slice(0, pWSTR.Length);
						return result;
					}
				}
			}
		}
	}

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static BOOL CreateProcess([Optional] string lpApplicationName, [Optional] SECURITY_ATTRIBUTES? lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] void* lpEnvironment, [Optional] string lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
	{
		//The blocks IL_0034, IL_005e, IL_0062, IL_0065, IL_006e, IL_0072, IL_0075 are reachable both inside and outside the pinned region starting at IL_002f. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		fixed (PROCESS_INFORMATION* lpProcessInformation2 = &lpProcessInformation)
		{
			fixed (STARTUPINFOW* lpStartupInfo2 = &lpStartupInfo)
			{
				fixed (char* ptr = lpCurrentDirectory)
				{
					nuint num;
					SECURITY_ATTRIBUTES valueOrDefault;
					SECURITY_ATTRIBUTES valueOrDefault2;
					PCWSTR lpApplicationName2;
					PWSTR lpCommandLine;
					UIntPtr lpProcessAttributes2;
					UIntPtr lpThreadAttributes2;
					if (lpApplicationName == null)
					{
						num = 0u;
						valueOrDefault = lpProcessAttributes.GetValueOrDefault();
						valueOrDefault2 = lpThreadAttributes.GetValueOrDefault();
						lpApplicationName2 = (char*)num;
						lpCommandLine = default;
						lpProcessAttributes2 = (UIntPtr)(lpProcessAttributes.HasValue ? (&valueOrDefault) : null);
						lpThreadAttributes2 = (UIntPtr)(lpThreadAttributes.HasValue ? (&valueOrDefault2) : null);
						return CreateProcess(lpApplicationName2, lpCommandLine, (SECURITY_ATTRIBUTES*)lpProcessAttributes2, (SECURITY_ATTRIBUTES*)lpThreadAttributes2, bInheritHandles, dwCreationFlags, lpEnvironment, ptr, lpStartupInfo2, lpProcessInformation2);
					}
					fixed (char* ptr2 = &lpApplicationName.GetPinnableReference())
					{
						num = (nuint)ptr2;
						valueOrDefault = lpProcessAttributes.GetValueOrDefault();
						valueOrDefault2 = lpThreadAttributes.GetValueOrDefault();
						lpApplicationName2 = (char*)num;
						lpCommandLine = default;
						lpProcessAttributes2 = (UIntPtr)(lpProcessAttributes.HasValue ? (&valueOrDefault) : null);
						lpThreadAttributes2 = (UIntPtr)(lpThreadAttributes.HasValue ? (&valueOrDefault2) : null);
						return CreateProcess(lpApplicationName2, lpCommandLine, (SECURITY_ATTRIBUTES*)lpProcessAttributes2, (SECURITY_ATTRIBUTES*)lpThreadAttributes2, bInheritHandles, dwCreationFlags, lpEnvironment, ptr, lpStartupInfo2, lpProcessInformation2);
					}
				}
			}
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "CreateProcessW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern BOOL CreateProcess([Optional] PCWSTR lpApplicationName, [Optional] PWSTR lpCommandLine, [Optional] SECURITY_ATTRIBUTES* lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES* lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] void* lpEnvironment, [Optional] PCWSTR lpCurrentDirectory, STARTUPINFOW* lpStartupInfo, PROCESS_INFORMATION* lpProcessInformation);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static SafeFileHandle CreateRemoteThread(SafeHandle hProcess, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, nuint dwStackSize, LPTHREAD_START_ROUTINE lpStartAddress, [Optional] void* lpParameter, uint dwCreationFlags, out uint lpThreadId)
	{
		bool success = false;
		try
		{
			fixed (uint* lpThreadId2 = &lpThreadId)
			{
				if (hProcess != null)
				{
					hProcess.DangerousAddRef(ref success);
					HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
					SECURITY_ATTRIBUTES valueOrDefault = lpThreadAttributes.GetValueOrDefault();
					SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
					HANDLE hANDLE = CreateRemoteThread(hProcess2, lpThreadAttributes.HasValue ? (&valueOrDefault) : null, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, lpThreadId2);
					Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
					return safeFileHandle;
				}
				throw new ArgumentNullException("hProcess");
			}
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static SafeFileHandle CreateRemoteThread(SafeHandle hProcess, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, nuint dwStackSize, LPTHREAD_START_ROUTINE lpStartAddress, [Optional] void* lpParameter, uint dwCreationFlags)
	{
		bool success = false;
		try
		{
			if (hProcess != null)
			{
				hProcess.DangerousAddRef(ref success);
				HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
				SECURITY_ATTRIBUTES valueOrDefault = lpThreadAttributes.GetValueOrDefault();
				SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
				HANDLE hANDLE = CreateRemoteThread(hProcess2, lpThreadAttributes.HasValue ? (&valueOrDefault) : null, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, null);
				Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
				return safeFileHandle;
			}
			throw new ArgumentNullException("hProcess");
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern HANDLE CreateRemoteThread(HANDLE hProcess, [Optional] SECURITY_ATTRIBUTES* lpThreadAttributes, nuint dwStackSize, [MarshalAs(UnmanagedType.FunctionPtr)] LPTHREAD_START_ROUTINE lpStartAddress, [Optional] void* lpParameter, uint dwCreationFlags, [Optional] uint* lpThreadId);

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern BOOL FreeLibrary(HMODULE hLibModule);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static FreeLibrarySafeHandle GetModuleHandle([Optional] string lpModuleName)
	{
		fixed (char* ptr = lpModuleName)
		{
			FreeLibrarySafeHandle freeLibrarySafeHandle = new FreeLibrarySafeHandle(0, ownsHandle: false);
			HMODULE moduleHandle = GetModuleHandle(ptr);
			Marshal.InitHandle(freeLibrarySafeHandle, (nint)moduleHandle);
			return freeLibrarySafeHandle;
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "GetModuleHandleW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HMODULE GetModuleHandle([Optional] PCWSTR lpModuleName);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static FARPROC GetProcAddress(SafeHandle hModule, string lpProcName)
	{
		bool success = false;
		try
		{
			fixed (byte* value = ((lpProcName != null) ? Encoding.Default.GetBytes(lpProcName) : null))
			{
				if (hModule != null)
				{
					hModule.DangerousAddRef(ref success);
					HMODULE hModule2 = (HMODULE)hModule.DangerousGetHandle();
					return GetProcAddress(hModule2, new PCSTR(value));
				}
				throw new ArgumentNullException("hModule");
			}
		}
		finally
		{
			if (success)
			{
				hModule.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern FARPROC GetProcAddress(HMODULE hModule, PCSTR lpProcName);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static FreeLibrarySafeHandle LoadLibrary(string lpLibFileName)
	{
		fixed (char* ptr = lpLibFileName)
		{
			FreeLibrarySafeHandle freeLibrarySafeHandle = new FreeLibrarySafeHandle(0);
			HMODULE hMODULE = LoadLibrary(ptr);
			Marshal.InitHandle(freeLibrarySafeHandle, (nint)hMODULE);
			return freeLibrarySafeHandle;
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "LoadLibraryW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HMODULE LoadLibrary(PCWSTR lpLibFileName);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static SafeFileHandle OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
		HANDLE hANDLE = OpenProcess(dwDesiredAccess, bInheritHandle, dwProcessId);
		Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
		return safeFileHandle;
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HANDLE OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static SafeFileHandle OpenThread_SafeHandle(THREAD_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwThreadId)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
		HANDLE hANDLE = OpenThread(dwDesiredAccess, bInheritHandle, dwThreadId);
		Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
		return safeFileHandle;
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HANDLE OpenThread(THREAD_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwThreadId);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static uint ResumeThread(SafeHandle hThread)
	{
		bool success = false;
		try
		{
			if (hThread != null)
			{
				hThread.DangerousAddRef(ref success);
				HANDLE hThread2 = (HANDLE)hThread.DangerousGetHandle();
				return ResumeThread(hThread2);
			}
			throw new ArgumentNullException("hThread");
		}
		finally
		{
			if (success)
			{
				hThread.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern uint ResumeThread(HANDLE hThread);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static uint SuspendThread(SafeHandle hThread)
	{
		bool success = false;
		try
		{
			if (hThread != null)
			{
				hThread.DangerousAddRef(ref success);
				HANDLE hThread2 = (HANDLE)hThread.DangerousGetHandle();
				return SuspendThread(hThread2);
			}
			throw new ArgumentNullException("hThread");
		}
		finally
		{
			if (success)
			{
				hThread.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern uint SuspendThread(HANDLE hThread);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static BOOL TerminateProcess(SafeHandle hProcess, uint uExitCode)
	{
		bool success = false;
		try
		{
			if (hProcess != null)
			{
				hProcess.DangerousAddRef(ref success);
				HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
				return TerminateProcess(hProcess2, uExitCode);
			}
			throw new ArgumentNullException("hProcess");
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern BOOL TerminateProcess(HANDLE hProcess, uint uExitCode);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static void* VirtualAllocEx(SafeHandle hProcess, [Optional] void* lpAddress, nuint dwSize, VIRTUAL_ALLOCATION_TYPE flAllocationType, PAGE_PROTECTION_FLAGS flProtect)
	{
		bool success = false;
		try
		{
			if (hProcess != null)
			{
				hProcess.DangerousAddRef(ref success);
				HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
				return VirtualAllocEx(hProcess2, lpAddress, dwSize, flAllocationType, flProtect);
			}
			throw new ArgumentNullException("hProcess");
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern void* VirtualAllocEx(HANDLE hProcess, [Optional] void* lpAddress, nuint dwSize, VIRTUAL_ALLOCATION_TYPE flAllocationType, PAGE_PROTECTION_FLAGS flProtect);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL VirtualFreeEx(SafeHandle hProcess, void* lpAddress, nuint dwSize, VIRTUAL_FREE_TYPE dwFreeType)
	{
		bool success = false;
		try
		{
			if (hProcess != null)
			{
				hProcess.DangerousAddRef(ref success);
				HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
				return VirtualFreeEx(hProcess2, lpAddress, dwSize, dwFreeType);
			}
			throw new ArgumentNullException("hProcess");
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern BOOL VirtualFreeEx(HANDLE hProcess, void* lpAddress, nuint dwSize, VIRTUAL_FREE_TYPE dwFreeType);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static WAIT_EVENT WaitForSingleObject(SafeHandle hHandle, uint dwMilliseconds)
	{
		bool success = false;
		try
		{
			if (hHandle != null)
			{
				hHandle.DangerousAddRef(ref success);
				HANDLE hHandle2 = (HANDLE)hHandle.DangerousGetHandle();
				return WaitForSingleObject(hHandle2, dwMilliseconds);
			}
			throw new ArgumentNullException("hHandle");
		}
		finally
		{
			if (success)
			{
				hHandle.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern WAIT_EVENT WaitForSingleObject(HANDLE hHandle, uint dwMilliseconds);

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static BOOL WriteProcessMemory(SafeHandle hProcess, void* lpBaseAddress, ReadOnlySpan<byte> lpBuffer, out nuint lpNumberOfBytesWritten)
	{
		bool success = false;
		try
		{
			fixed (nuint* lpNumberOfBytesWritten2 = &lpNumberOfBytesWritten)
			{
				fixed (byte* lpBuffer2 = lpBuffer)
				{
					if (hProcess != null)
					{
						hProcess.DangerousAddRef(ref success);
						HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
						return WriteProcessMemory(hProcess2, lpBaseAddress, lpBuffer2, (nuint)lpBuffer.Length, lpNumberOfBytesWritten2);
					}
					throw new ArgumentNullException("hProcess");
				}
			}
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static BOOL WriteProcessMemory(SafeHandle hProcess, void* lpBaseAddress, ReadOnlySpan<byte> lpBuffer)
	{
		bool success = false;
		try
		{
			fixed (byte* lpBuffer2 = lpBuffer)
			{
				if (hProcess != null)
				{
					hProcess.DangerousAddRef(ref success);
					HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
					return WriteProcessMemory(hProcess2, lpBaseAddress, lpBuffer2, (nuint)lpBuffer.Length, null);
				}
				throw new ArgumentNullException("hProcess");
			}
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern BOOL WriteProcessMemory(HANDLE hProcess, void* lpBaseAddress, void* lpBuffer, nuint nSize, [Optional] nuint* lpNumberOfBytesWritten);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL AllowSetForegroundWindow(uint dwProcessId);

	[DllImport("USER32.dll", EntryPoint = "CallWindowProcW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern LRESULT CallWindowProc([MarshalAs(UnmanagedType.FunctionPtr)] WNDPROC lpPrevWndFunc, HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL IsIconic(HWND hWnd);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static MESSAGEBOX_RESULT MessageBox([Optional] HWND hWnd, [Optional] string lpText, [Optional] string lpCaption, MESSAGEBOX_STYLE uType)
	{
		//The blocks IL_0021 are reachable both inside and outside the pinned region starting at IL_001e. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		fixed (char* ptr = lpCaption)
		{
			char* uIntPtr;
			char* ptr3;
			if (lpText != null)
			{
				fixed (char* ptr2 = &lpText.GetPinnableReference())
				{
					uIntPtr = (ptr3 = ptr2);
					return MessageBox(hWnd, ptr3, ptr, uType);
				}
			}
			uIntPtr = (ptr3 = null);
			return MessageBox(hWnd, ptr3, ptr, uType);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern MESSAGEBOX_RESULT MessageBox([Optional] HWND hWnd, [Optional] PCWSTR lpText, [Optional] PCWSTR lpCaption, MESSAGEBOX_STYLE uType);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL ReleaseCapture();

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static uint SendInput(ReadOnlySpan<INPUT> pInputs, int cbSize)
	{
		fixed (INPUT* pInputs2 = pInputs)
		{
			return SendInput((uint)pInputs.Length, pInputs2, cbSize);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern uint SendInput(uint cInputs, INPUT* pInputs, int cbSize);

	[DllImport("USER32.dll", EntryPoint = "SendMessageW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern LRESULT SendMessage(HWND hWnd, uint Msg, [Optional] WPARAM wParam, [Optional] LPARAM lParam);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL SetForegroundWindow(HWND hWnd);

	[DllImport("USER32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL ShowWindow(HWND hWnd, SHOW_WINDOW_CMD nCmdShow);
}
