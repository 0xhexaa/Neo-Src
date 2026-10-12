using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct LPARAM : IEquatable<LPARAM>
{
	internal readonly nint Value;

	internal LPARAM(nint value)
	{
		Value = value;
	}

	public static implicit operator nint(LPARAM value)
	{
		return value.Value;
	}

	public static implicit operator LPARAM(nint value)
	{
		return new LPARAM(value);
	}

	public static bool operator ==(LPARAM left, LPARAM right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(LPARAM left, LPARAM right)
	{
		return !(left == right);
	}

	public bool Equals(LPARAM other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is LPARAM other)
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
}
