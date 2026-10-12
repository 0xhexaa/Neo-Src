using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct HWND : IEquatable<HWND>
{
	internal unsafe readonly void* Value;

	internal static HWND Null => default;

	internal unsafe bool IsNull => Value == null;

	internal unsafe HWND(void* value)
	{
		Value = value;
	}

	internal unsafe HWND(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HWND value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HWND(void* value)
	{
		return new HWND(value);
	}

	public unsafe static bool operator ==(HWND left, HWND right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HWND left, HWND right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HWND other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HWND other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		return (int)Value;
	}

	public unsafe override string ToString()
	{
		return $"0x{(nuint)Value:x}";
	}

	public unsafe static implicit operator nint(HWND value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HWND(nint value)
	{
		return new HWND(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HWND(nuint value)
	{
		return new HWND(((UIntPtr)value).ToPointer());
	}

	public unsafe static implicit operator HANDLE(HWND value)
	{
		return new HANDLE(value.Value);
	}
}
