using System.Globalization;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace Root.Core.Api.Asset;

public enum VariantType : byte
{
	Null,
	Bool,
	NumInt,
	NumDouble,
	Str
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct Variant : IEquatable<Variant>
{
	[field: FieldOffset(0)] public VariantType Type { get; }

	[FieldOffset(8)] private readonly long _integer;
	[FieldOffset(8)] private readonly double _float;

	[FieldOffset(16)] private readonly string? _string;

	public static readonly Variant Null;

	public Variant(bool value) : this()
	{
		Type = VariantType.Bool;
		_integer = value ? 1 : 0;
	}

	public Variant(int value) : this((long)value) { }

	public Variant(long value) : this()
	{
		Type = VariantType.NumInt;
		_integer = value;
	}

	public Variant(float value) : this((double)value) { }

	public Variant(double value) : this()
	{
		Type = VariantType.NumDouble;
		_float = value;
	}

	public Variant(string? value) : this()
	{
		if (value is null)
		{
			Type = VariantType.Null;
			return;
		}

		Type = VariantType.Str;
		_string = value;
	}

	// ReSharper disable once UnusedMember.Global
	public bool IsNull => Type == VariantType.Null;

	public static implicit operator Variant(bool value) => new(value);
	public static implicit operator Variant(int value) => new(value);
	public static implicit operator Variant(long value) => new(value);
	public static implicit operator Variant(float value) => new(value);
	public static implicit operator Variant(double value) => new(value);
	public static implicit operator Variant(string? value) => new(value);

	public static explicit operator bool(Variant variant)
		=> variant.Type == VariantType.Bool ? variant._integer != 0 : throw new InvalidCastException();

	public static explicit operator long(Variant variant) => variant.Type switch
	{
		VariantType.NumInt => variant._integer,
		VariantType.NumDouble => (long)variant._float,
		_ => throw new InvalidCastException()
	};

	public static explicit operator double(Variant variant) => variant.Type switch
	{
		VariantType.NumDouble => variant._float,
		VariantType.NumInt => variant._integer,
		_ => throw new InvalidCastException()
	};

	public static explicit operator string(Variant variant)
		=> variant.Type == VariantType.Str ? variant._string! : throw new InvalidCastException();

	public static bool operator ==(Variant left, Variant right) => left.Equals(right);
	public static bool operator !=(Variant left, Variant right) => !left.Equals(right);

	public bool Equals(Variant other)
	{
		if (Type != other.Type)
			return false;

		return Type switch
		{
			VariantType.Null => true,
			VariantType.Bool or VariantType.NumInt => _integer == other._integer,
			VariantType.NumDouble => _float.Equals(other._float),
			VariantType.Str => string.Equals(_string, other._string, StringComparison.Ordinal),
			_ => false
		};
	}

	public override bool Equals(object? obj) => obj is Variant other && Equals(other);

	public override int GetHashCode() => Type switch
	{
		VariantType.Null => Type.GetHashCode(),
		VariantType.Bool or VariantType.NumInt => HashCode.Combine(Type, _integer),
		VariantType.NumDouble => HashCode.Combine(Type, _float),
		VariantType.Str => HashCode.Combine(Type, _string),
		_ => Type.GetHashCode()
	};

	public override string ToString() => Type switch
	{
		VariantType.Null => "null",
		VariantType.Bool => (_integer != 0).ToString(),
		VariantType.NumInt => _integer.ToString(CultureInfo.InvariantCulture),
		VariantType.NumDouble => _float.ToString(CultureInfo.InvariantCulture),
		VariantType.Str => _string ?? "null",
		_ => "unknown"
	};
}
