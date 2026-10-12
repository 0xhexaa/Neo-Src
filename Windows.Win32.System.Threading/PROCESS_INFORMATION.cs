using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Threading;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct PROCESS_INFORMATION
{
	internal HANDLE hProcess;

	internal HANDLE hThread;

	internal uint dwProcessId;

	internal uint dwThreadId;
}
