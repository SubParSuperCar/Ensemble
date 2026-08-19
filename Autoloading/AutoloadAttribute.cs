// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Autoloading;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoloadAttribute : Attribute
{
	public AutoloadScopeFlag Scope { get; init; } = AutoloadScopeFlag.Client | AutoloadScopeFlag.Server;
	public sbyte Order { get; init; }
	public AutoloadFailurePolicyEnum FailurePolicy { get; init; } = AutoloadFailurePolicyEnum.AskUser;
}
