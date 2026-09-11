// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

/// <summary>
///     An attribute given to game components that will be automatically instantiated when the game loads.
///     For initialization errors to be caught, this attribute should be paired with <see cref="IAutoload" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoloadAttribute : Attribute
{
	/// <summary>
	///     The run context(s) that this Autoload will be instantiated in.
	/// </summary>
	public AutoloadScope Scope { get; init; } = AutoloadScope.RegularClient | AutoloadScope.HeadlessServer;

	/// <summary>
	///     The order that this Autoload will be instantiated in,
	///     where lower values are earlier and higher values are later.
	///     Ranges from -128 (<see cref="sbyte.MinValue" />) to 127 (<see cref="sbyte.MaxValue" />).
	///     If more than one Autoload object has the same order,
	///     their order of instantiation will be resolved with ordinal alphabetical ordering.
	///     So, "A" runs before "Z" which runs before "a" which runs before "z."
	/// </summary>
	public sbyte Order { get; init; }

	/// <summary>
	///     The policy to follow if a failure occurs in this Autoload, and it cannot be instantiated.
	/// </summary>
	public AutoloadFailurePolicy FailurePolicy { get; init; }
}
