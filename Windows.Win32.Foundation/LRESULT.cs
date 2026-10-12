using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{Value}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct LRESULT : IEquatable<LRESULT>
{
	internal readonly nint Value;

	internal LRESULT(nint value)
	{
		Value = value;
	}

	public static implicit operator nint(LRESULT value)
	{
		return value.Value;
	}

	public static explicit operator LRESULT(nint value)
	{
		return new LRESULT(value);
	}

	public static bool operator ==(LRESULT left, LRESULT right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(LRESULT left, LRESULT right)
	{
		return !(left == right);
	}

	public bool Equals(LRESULT other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is LRESULT other)
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
