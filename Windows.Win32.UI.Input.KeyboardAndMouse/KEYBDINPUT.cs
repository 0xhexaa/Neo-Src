using System.CodeDom.Compiler;

namespace Windows.Win32.UI.Input.KeyboardAndMouse;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct KEYBDINPUT
{
	internal VIRTUAL_KEY wVk;

	internal ushort wScan;

	internal KEYBD_EVENT_FLAGS dwFlags;

	internal uint time;

	internal nuint dwExtraInfo;
}
