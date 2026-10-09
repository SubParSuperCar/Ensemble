// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace EnsembleRoot.Autoloading;

/// <summary>
///     Marks a node to be instantiated automatically when the game loads.
///     Implement <see cref="IAutoload" /> to have initialization errors caught.
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
