// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

/// <summary>
///     Marks a game component to be instantiated automatically when the game loads.
///     For initialization errors to be caught, pair this attribute with <see cref="IAutoload" />.
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
