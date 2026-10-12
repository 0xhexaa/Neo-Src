using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct HANDLE : IEquatable<HANDLE>
{
	internal unsafe readonly void* Value;

	internal static HANDLE Null => default;

	internal unsafe bool IsNull => Value == null;

	internal unsafe HANDLE(void* value)
	{
		Value = value;
	}

	internal unsafe HANDLE(nint value)
		: this((void*)value)
	{
	}

	public unsafe static implicit operator void*(HANDLE value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HANDLE(void* value)
	{
		return new HANDLE(value);
	}

	public unsafe static bool operator ==(HANDLE left, HANDLE right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HANDLE left, HANDLE right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(HANDLE other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is HANDLE other)
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

	public unsafe static implicit operator nint(HANDLE value)
	{
		return new IntPtr(value.Value);
	}

	public unsafe static explicit operator HANDLE(nint value)
	{
		return new HANDLE(((IntPtr)value).ToPointer());
	}

	public unsafe static explicit operator HANDLE(nuint value)
	{
		return new HANDLE(((UIntPtr)value).ToPointer());
	}
}
