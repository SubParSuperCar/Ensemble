using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace CoreRoot.Api.Assets;

public enum CoreVariantType : byte
{
	Null,
	Bool,
	NumInt,
	NumDouble,
	Str
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct CoreVariant : IEquatable<CoreVariant>
{
	[field: FieldOffset(0)] public CoreVariantType Type { get; }

	[FieldOffset(8)] private readonly long _integer;
	[FieldOffset(8)] private readonly double _float;

	[FieldOffset(16)] private readonly string? _string;

	public static readonly CoreVariant Null;

	public CoreVariant(bool value) : this()
	{
		Type = CoreVariantType.Bool;
		_integer = value ? 1 : 0;
	}

	public CoreVariant(int value) : this((long)value) { }

	public CoreVariant(long value) : this()
	{
		Type = CoreVariantType.NumInt;
		_integer = value;
	}

	public CoreVariant(float value) : this((double)value) { }

	public CoreVariant(double value) : this()
	{
		Type = CoreVariantType.NumDouble;
		_float = value;
	}

	public CoreVariant(string? value) : this()
	{
		if (value is null)
		{
			Type = CoreVariantType.Null;
			return;
		}

		Type = CoreVariantType.Str;
		_string = value;
	}

	public object? Value =>
		Type switch
		{
			CoreVariantType.Null => null,
			CoreVariantType.Bool => (bool)this,
			CoreVariantType.NumInt => (long)this,
			CoreVariantType.NumDouble => (double)this,
			CoreVariantType.Str => (string)this,
			_ => throw new UnreachableException()
		};

	public bool IsNull => Type is CoreVariantType.Null;

	public static implicit operator CoreVariant(bool value) => new(value);
	public static implicit operator CoreVariant(int value) => new(value);
	public static implicit operator CoreVariant(long value) => new(value);
	public static implicit operator CoreVariant(float value) => new(value);
	public static implicit operator CoreVariant(double value) => new(value);
	public static implicit operator CoreVariant(string? value) => new(value);

	public static explicit operator bool(CoreVariant variant) =>
		variant.Type is CoreVariantType.Bool ? variant._integer is not 0 : throw new InvalidCastException();

	public static explicit operator long(CoreVariant variant) =>
		variant.Type switch
		{
			CoreVariantType.NumInt => variant._integer,
			CoreVariantType.NumDouble => (long)variant._float,
			_ => throw new InvalidCastException()
		};

	public static explicit operator double(CoreVariant variant) =>
		variant.Type switch
		{
			CoreVariantType.NumDouble => variant._float,
			CoreVariantType.NumInt => variant._integer,
			_ => throw new InvalidCastException()
		};

	public static explicit operator string(CoreVariant variant) =>
		variant.Type is CoreVariantType.Str ? variant._string! : throw new InvalidCastException();

	public static bool operator ==(CoreVariant left, CoreVariant right) => left.Equals(right);
	public static bool operator !=(CoreVariant left, CoreVariant right) => !left.Equals(right);

	public bool Equals(CoreVariant other)
	{
		if (Type != other.Type)
			return false;

		return Type switch
		{
			CoreVariantType.Null => true,
			CoreVariantType.Bool or CoreVariantType.NumInt => _integer == other._integer,
			CoreVariantType.NumDouble => _float.Equals(other._float),
			CoreVariantType.Str => string.Equals(_string, other._string, StringComparison.Ordinal),
			_ => false
		};
	}

	public override bool Equals(object? obj) => obj is CoreVariant other && Equals(other);

	public override int GetHashCode() =>
		Type switch
		{
			CoreVariantType.Null => Type.GetHashCode(),
			CoreVariantType.Bool or CoreVariantType.NumInt => HashCode.Combine(Type, _integer),
			CoreVariantType.NumDouble => HashCode.Combine(Type, _float),
			CoreVariantType.Str => HashCode.Combine(Type, _string),
			_ => Type.GetHashCode()
		};

	public override string ToString() =>
		Type switch
		{
			CoreVariantType.Null => "null",
			CoreVariantType.Bool => (_integer is not 0).ToString(),
			CoreVariantType.NumInt => _integer.ToString(CultureInfo.InvariantCulture),
			CoreVariantType.NumDouble => _float.ToString(CultureInfo.InvariantCulture),
			CoreVariantType.Str => _string ?? "null",
			_ => "unknown"
		};
}
