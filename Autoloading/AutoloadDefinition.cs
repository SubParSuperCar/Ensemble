using Godot;

namespace Root.Autoloading;

// Store both the Type and Factory for debugging and instantiation.
public readonly record struct AutoloadDefinition(
	Type Type,
	AutoloadScope Scope,
	sbyte Order,
	AutoloadFailurePolicy FailurePolicy,
	Func<Node> Factory);
