namespace EnsembleRoot.Autoloading;

/// <summary>
///     The order in which this Autoload is instantiated, from -128 (<see cref="sbyte.MinValue" />, first) to 127
///     (<see cref="sbyte.MaxValue" />, last).
/// </summary>
/// <remarks>
///     Ties are broken by the ordinal order of fully qualified type names ("A" before "Z" before "a" before "z").
/// </remarks>
public static class AutoloadOrder
{
	public const sbyte First = sbyte.MinValue;
	public const sbyte Early = -0x40;
	public const sbyte Standard = 0;
	public const sbyte Late = 0x40;
	public const sbyte Last = sbyte.MaxValue;
}
