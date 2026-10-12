using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct HMODULE : IEquatable<HMODULE>
{
	internal unsafe readonly void* Value;

	internal static HMODULE Null => default;

	internal unsafe bool IsNull => Value == null;

	internal unsafe HMODULE(void* value)
	{
		Value = value;
	}

	internal unsafe HMODULE(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HMODULE value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HMODULE(void* value)
	{
		return new HMODULE(value);
	}

	public unsafe static bool operator ==(HMODULE left, HMODULE right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HMODULE left, HMODULE right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HMODULE other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HMODULE other)
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

	public unsafe static implicit operator nint(HMODULE value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HMODULE(nint value)
	{
		return new HMODULE(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HMODULE(nuint value)
	{
		return new HMODULE(((UIntPtr)value).ToPointer());
	}

	public unsafe static implicit operator HINSTANCE(HMODULE value)
	{
		return new HINSTANCE(value.Value);
	}
}
