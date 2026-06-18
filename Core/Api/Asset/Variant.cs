using System.Globalization;
using System.Runtime.InteropServices;

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

	[FieldOffset(8)] private readonly long _bits;
	[FieldOffset(8)] private readonly double _real;

	[FieldOffset(16)] private readonly string? _text;

	public static readonly Variant Null;

	public Variant(bool value) : this()
	{
		Type = VariantType.Bool;
		_bits = value ? 1 : 0;
	}

	public Variant(int value) : this((long)value) { }

	public Variant(long value) : this()
	{
		Type = VariantType.NumInt;
		_bits = value;
	}

	public Variant(float value) : this((double)value) { }

	public Variant(double value) : this()
	{
		Type = VariantType.NumDouble;
		_real = value;
	}

	public Variant(string? value) : this()
	{
		if (value is null)
		{
			Type = VariantType.Null;
			return;
		}

		Type = VariantType.Str;
		_text = value;
	}

	public bool IsNull => Type == VariantType.Null;

	public static implicit operator Variant(bool value) => new(value);
	public static implicit operator Variant(int value) => new(value);
	public static implicit operator Variant(long value) => new(value);
	public static implicit operator Variant(float value) => new(value);
	public static implicit operator Variant(double value) => new(value);
	public static implicit operator Variant(string? value) => new(value);

	public static explicit operator bool(Variant variant)
		=> variant.Type == VariantType.Bool ? variant._bits != 0 : throw new InvalidCastException();

	public static explicit operator long(Variant variant) => variant.Type switch
	{
		VariantType.NumInt => variant._bits,
		VariantType.NumDouble => (long)variant._real,
		_ => throw new InvalidCastException()
	};

	public static explicit operator double(Variant variant) => variant.Type switch
	{
		VariantType.NumDouble => variant._real,
		VariantType.NumInt => variant._bits,
		_ => throw new InvalidCastException()
	};

	public static explicit operator string(Variant variant)
		=> variant.Type == VariantType.Str ? variant._text! : throw new InvalidCastException();

	public static bool operator ==(Variant left, Variant right) => left.Equals(right);
	public static bool operator !=(Variant left, Variant right) => !left.Equals(right);

	public bool Equals(Variant other)
	{
		if (Type != other.Type)
			return false;

		return Type switch
		{
			VariantType.Null => true,
			VariantType.Bool => _bits == other._bits,
			VariantType.NumInt => _bits == other._bits,
			VariantType.NumDouble => _real.Equals(other._real),
			VariantType.Str => string.Equals(_text, other._text, StringComparison.Ordinal),
			_ => false
		};
	}

	public override bool Equals(object? obj) => obj is Variant other && Equals(other);

	public override int GetHashCode() => Type switch
	{
		VariantType.Null => Type.GetHashCode(),
		VariantType.Bool => HashCode.Combine(Type, _bits),
		VariantType.NumInt => HashCode.Combine(Type, _bits),
		VariantType.NumDouble => HashCode.Combine(Type, _real),
		VariantType.Str => HashCode.Combine(Type, _text),
		_ => Type.GetHashCode()
	};

	public override string ToString() => Type switch
	{
		VariantType.Null => "null",
		VariantType.Bool => (_bits != 0).ToString(),
		VariantType.NumInt => _bits.ToString(CultureInfo.InvariantCulture),
		VariantType.NumDouble => _real.ToString(CultureInfo.InvariantCulture),
		VariantType.Str => _text ?? "null",
		_ => "unknown"
	};
}
