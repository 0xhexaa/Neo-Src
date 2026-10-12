using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal struct FARPROC
{
	internal nint Value;

	internal static FARPROC Null => default;

	internal bool IsNull => Value == 0;

	internal FARPROC(nint value)
	{
		Value = value;
	}

	public static implicit operator nint(FARPROC value)
	{
		return value.Value;
	}

	public static explicit operator FARPROC(nint value)
	{
		return new FARPROC(value);
	}

	public static bool operator ==(FARPROC left, FARPROC right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(FARPROC left, FARPROC right)
	{
		return !(left == right);
	}

	public bool Equals(FARPROC other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is FARPROC other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IntPtr)Value).GetHashCode();
	}

	public override string ToString()
	{
		return $"0x{Value:x}";
	}

	internal TDelegate CreateDelegate<TDelegate>() where TDelegate : Delegate
	{
		return Marshal.GetDelegateForFunctionPointer<TDelegate>(Value);
	}
}
