using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.Security;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct SECURITY_ATTRIBUTES
{
	internal uint nLength;

	internal unsafe void* lpSecurityDescriptor;

	internal BOOL bInheritHandle;
}
