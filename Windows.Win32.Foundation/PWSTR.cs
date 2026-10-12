using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Windows.Win32.Foundation;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.269+368685089b.RR")]
internal readonly struct PWSTR : IEquatable<PWSTR>
{
	internal unsafe readonly char* Value;

	internal unsafe int Length => new PCWSTR(Value).Length;

	private string DebuggerDisplay => ToString();

	internal unsafe PWSTR(char* value)
	{
		Value = value;
	}

	internal unsafe PWSTR(nint value)
		: this((char*)value)
	{
	}

	public unsafe static implicit operator char*(PWSTR value)
	{
		return value.Value;
	}

	public unsafe static implicit operator PWSTR(char* value)
	{
		return new PWSTR(value);
	}

	public unsafe static bool operator ==(PWSTR left, PWSTR right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(PWSTR left, PWSTR right)
	{
		return !(left == right);
	}

	public unsafe bool Equals(PWSTR other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is PWSTR other)
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
		return new PCWSTR(Value).ToString();
	}

	public unsafe static implicit operator PCWSTR(PWSTR value)
	{
		return new PCWSTR(value.Value);
	}

	internal unsafe Span<char> AsSpan()
	{
		if (Value != null)
		{
			return new Span<char>(Value, Length);
		}
		return default;
	}
}
