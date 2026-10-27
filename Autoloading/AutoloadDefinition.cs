using Godot;

namespace EnsembleRoot.Autoloading;

public readonly record struct AutoloadDefinition(Type Type, AutoloadAttribute Attribute, Func<Node> Factory);
