using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Threading;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct STARTUPINFOW
{
	internal uint cb;

	internal PWSTR lpReserved;

	internal PWSTR lpDesktop;

	internal PWSTR lpTitle;

	internal uint dwX;

	internal uint dwY;

	internal uint dwXSize;

	internal uint dwYSize;

	internal uint dwXCountChars;

	internal uint dwYCountChars;

	internal uint dwFillAttribute;

	internal STARTUPINFOW_FLAGS dwFlags;

	internal ushort wShowWindow;

	internal ushort cbReserved2;

	internal unsafe byte* lpReserved2;

	internal HANDLE hStdInput;

	internal HANDLE hStdOutput;

	internal HANDLE hStdError;
}
