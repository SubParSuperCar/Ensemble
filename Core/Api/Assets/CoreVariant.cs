using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace CoreRoot.Api.Assets;

public enum CoreVariantType : byte
{
	Null,
	Boolean,
	Int64,
	Double,
	String
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct CoreVariant : IEquatable<CoreVariant>
{
	public static readonly CoreVariant Null;

	[FieldOffset(8)] private readonly long _integer;
	[FieldOffset(8)] private readonly double _float;

	[FieldOffset(16)] private readonly string? _string;

	public CoreVariant(bool value) : this()
	{
		Type = CoreVariantType.Boolean;
		_integer = value ? 1 : 0;
	}

	public CoreVariant(int value) : this((long)value) { }

	public CoreVariant(long value) : this()
	{
		Type = CoreVariantType.Int64;
		_integer = value;
	}

	public CoreVariant(float value) : this((double)value) { }

	public CoreVariant(double value) : this()
	{
		Type = CoreVariantType.Double;
		_float = value;
	}

	public CoreVariant(string? value) : this()
	{
		if (value is null)
		{
			Type = CoreVariantType.Null;
			return;
		}

		Type = CoreVariantType.String;
		_string = value;
	}

	[field: FieldOffset(0)] public CoreVariantType Type { get; }

	public object? Value =>
		Type switch
		{
			CoreVariantType.Null => null,
			CoreVariantType.Boolean => (bool)this,
			CoreVariantType.Int64 => (long)this,
			CoreVariantType.Double => (double)this,
			CoreVariantType.String => (string)this,
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
		variant.Type is CoreVariantType.Boolean ? variant._integer is not 0 : throw new InvalidCastException();

	public static explicit operator long(CoreVariant variant) =>
		variant.Type switch
		{
			CoreVariantType.Int64 => variant._integer,
			CoreVariantType.Double => (long)variant._float,
			_ => throw new InvalidCastException()
		};

	public static explicit operator double(CoreVariant variant) =>
		variant.Type switch
		{
			CoreVariantType.Double => variant._float,
			CoreVariantType.Int64 => variant._integer,
			_ => throw new InvalidCastException()
		};

	public static explicit operator string(CoreVariant variant) =>
		variant.Type is CoreVariantType.String ? variant._string! : throw new InvalidCastException();

	public static bool operator ==(CoreVariant left, CoreVariant right) => left.Equals(right);
	public static bool operator !=(CoreVariant left, CoreVariant right) => !left.Equals(right);

	public bool Equals(CoreVariant other)
	{
		if (Type != other.Type)
			return false;

		return Type switch
		{
			CoreVariantType.Null => true,
			CoreVariantType.Boolean or CoreVariantType.Int64 => _integer == other._integer,
			CoreVariantType.Double => _float.Equals(other._float),
			CoreVariantType.String => string.Equals(_string, other._string, StringComparison.Ordinal),
			_ => false
		};
	}

	public override bool Equals(object? obj) => obj is CoreVariant other && Equals(other);

	public override int GetHashCode() =>
		Type switch
		{
			CoreVariantType.Boolean or CoreVariantType.Int64 => HashCode.Combine(Type, _integer),
			CoreVariantType.Double => HashCode.Combine(Type, _float),
			CoreVariantType.String => HashCode.Combine(Type, _string),
			_ => Type.GetHashCode()
		};

	public override string ToString() =>
		Type switch
		{
			CoreVariantType.Null => "null",
			CoreVariantType.Boolean => (_integer is not 0).ToString(),
			CoreVariantType.Int64 => _integer.ToString(CultureInfo.InvariantCulture),
			CoreVariantType.Double => _float.ToString(CultureInfo.InvariantCulture),
			CoreVariantType.String => _string ?? "null",
			_ => "unknown"
		};
}
