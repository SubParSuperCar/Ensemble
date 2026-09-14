// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

/// <summary>
///     An attribute given to game components that should be automatically instantiated when the game loads.
///     For initialization errors to be caught, this attribute should be paired with <see cref="IAutoload" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoloadAttribute : Attribute
{
	/// <inheritdoc cref="AutoloadScope" />
	public AutoloadScope Scope { get; init; } = AutoloadScope.RegularClient | AutoloadScope.HeadlessServer;

	/// <inheritdoc cref="AutoloadOrder" />
	public sbyte Order { get; init; } = AutoloadOrder.Standard;

	/// <inheritdoc cref="AutoloadFailurePolicy" />
	public AutoloadFailurePolicy FailurePolicy { get; init; }
}
