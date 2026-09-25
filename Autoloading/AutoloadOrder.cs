namespace EnsembleRoot.Autoloading;

/// <summary>
///     The order in which this Autoload is instantiated,
///     where lower values are earlier and higher values are later.
///     Ranges from -128 (<see cref="sbyte.MinValue" />) to 127 (<see cref="sbyte.MaxValue" />).
///     If more than one Autoload has the same order,
///     their order of instantiation is resolved by the ordinal ordering of their fully qualified type names,
///     so "A" runs before "Z", which runs before "a", which runs before "z".
/// </summary>
public static class AutoloadOrder
{
	public const sbyte First = sbyte.MinValue;
	public const sbyte Early = unchecked((sbyte)0xC0);
	public const sbyte Standard = 0;
	public const sbyte Late = 0x40;
	public const sbyte Last = sbyte.MaxValue;
}
