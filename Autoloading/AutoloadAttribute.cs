// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoloadAttribute : Attribute
{
	// These defaults should match those in the source generator.
	public AutoloadScope Scope { get; init; } =
		AutoloadScope.Client |
		AutoloadScope.Server; // This is essentially the equivalent of "all," though not explicitly.

	public sbyte Order { get; init; } // A signed byte is probably enough.

	public AutoloadFailurePolicy FailurePolicy { get; init; } =
		AutoloadFailurePolicy.AskUser; // Asking the user is probably a reasonable default.
}
