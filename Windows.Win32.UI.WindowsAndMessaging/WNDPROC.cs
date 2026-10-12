using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.WindowsAndMessaging;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal delegate LRESULT WNDPROC(HWND param0, uint param1, WPARAM param2, LPARAM param3);
