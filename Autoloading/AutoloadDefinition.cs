using Godot;

namespace EnsembleRoot.Autoloading;

public readonly record struct AutoloadDefinition(
	Type Type,
	AutoloadScope Scope,
	sbyte Order,
	AutoloadFailurePolicy FailurePolicy,
	Func<Node> Factory);
