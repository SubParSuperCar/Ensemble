using Godot;

namespace Root.Autoloading;

public readonly record struct AutoloadDefinition(
	Type Type,
	AutoloadScopeFlag Scope,
	sbyte Order,
	AutoloadFailurePolicyEnum FailurePolicy,
	Func<Node> Factory);
