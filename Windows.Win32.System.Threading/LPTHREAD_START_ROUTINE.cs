using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32.System.Threading;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal unsafe delegate uint LPTHREAD_START_ROUTINE(void* lpThreadParameter);
