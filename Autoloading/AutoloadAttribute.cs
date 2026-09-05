// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoloadAttribute : Attribute
{
	public AutoloadScope Scope { get; init; } = AutoloadScope.Client | AutoloadScope.Server;
	public sbyte Order { get; init; }
	public AutoloadFailurePolicy FailurePolicy { get; init; } = AutoloadFailurePolicy.AskUser;
}
