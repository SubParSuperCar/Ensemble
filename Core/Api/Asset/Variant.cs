using System.Globalization;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global

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

	[FieldOffset(1)] private readonly bool _boolValue;

	[FieldOffset(8)] private readonly long _intValue;
	[FieldOffset(8)] private readonly double _doubleValue;

	[FieldOffset(16)] private readonly string? _stringValue;

	public static readonly Variant Null;

	// ReSharper disable once UnusedMember.Global
	public bool IsNull => Type == VariantType.Null;

	public Variant(bool value) : this()
	{
		Type = VariantType.Bool;
		_boolValue = value;
	}

	public Variant(int value) : this((long)value) { }

	public Variant(long value) : this()
	{
		Type = VariantType.NumInt;
		_intValue = value;
	}

	public Variant(float value) : this((double)value) { }

	public Variant(double value) : this()
	{
		Type = VariantType.NumDouble;
		_doubleValue = value;
	}

	public Variant(string? value) : this()
	{
		if (value is null)
			Type = VariantType.Null;
		else
		{
			Type = VariantType.Str;
			_stringValue = value;
		}
	}

	public static implicit operator Variant(bool value) => new(value);
	public static implicit operator Variant(int value) => new(value);
	public static implicit operator Variant(long value) => new(value);
	public static implicit operator Variant(float value) => new(value);
	public static implicit operator Variant(double value) => new(value);
	public static implicit operator Variant(string? value) => new(value);

	public static explicit operator bool(Variant variant)
		=> variant.Type == VariantType.Bool ? variant._boolValue : throw new InvalidCastException();

	// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
	public static explicit operator long(Variant variant) => variant.Type switch
	{
		VariantType.NumInt => variant._intValue,
		VariantType.NumDouble => (long)variant._doubleValue,
		_ => throw new InvalidCastException()
	};

	// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
	public static explicit operator double(Variant variant) => variant.Type switch
	{
		VariantType.NumDouble => variant._doubleValue,
		VariantType.NumInt => variant._intValue,
		_ => throw new InvalidCastException()
	};

	public static explicit operator string(Variant variant)
		=> variant.Type == VariantType.Str ? variant._stringValue! : throw new InvalidCastException();

	public bool Equals(Variant other) =>
		Type == other.Type && Type switch
		{
			VariantType.Null => true,
			VariantType.Bool => _boolValue == other._boolValue,
			VariantType.NumInt => _intValue == other._intValue,
			VariantType.NumDouble => _doubleValue.Equals(other._doubleValue),
			VariantType.Str => string.Equals(_stringValue, other._stringValue, StringComparison.Ordinal),
			_ => false
		};

	public override bool Equals(object? obj) => obj is Variant other && Equals(other);

	public override int GetHashCode() =>
		Type switch
		{
			VariantType.Null => Type.GetHashCode(),
			VariantType.Bool => HashCode.Combine(Type, _boolValue),
			VariantType.NumInt => HashCode.Combine(Type, _intValue),
			VariantType.NumDouble => HashCode.Combine(Type, _doubleValue),
			VariantType.Str => HashCode.Combine(Type, _stringValue),
			_ => Type.GetHashCode()
		};

	public override string ToString() =>
		Type switch
		{
			VariantType.Null => "null",
			VariantType.Bool => _boolValue.ToString(),
			VariantType.NumInt => _intValue.ToString(CultureInfo.InvariantCulture),
			VariantType.NumDouble => _doubleValue.ToString(CultureInfo.InvariantCulture),
			VariantType.Str => _stringValue ?? "null",
			_ => "unknown"
		};

	public static bool operator ==(Variant left, Variant right) => left.Equals(right);
	public static bool operator !=(Variant left, Variant right) => !left.Equals(right);
}
