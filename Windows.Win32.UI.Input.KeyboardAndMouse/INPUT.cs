using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32.UI.Input.KeyboardAndMouse;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct INPUT
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
	internal struct _Anonymous_e__Union
	{
		[FieldOffset(0)]
		internal MOUSEINPUT mi;

		[FieldOffset(0)]
		internal KEYBDINPUT ki;

		[FieldOffset(0)]
		internal HARDWAREINPUT hi;
	}

	internal INPUT_TYPE type;

	internal _Anonymous_e__Union Anonymous;
}
