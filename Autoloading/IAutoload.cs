namespace EnsembleRoot.Autoloading;

public interface IAutoload
{
	/// <summary>
	///     Called by the Autoload system once the node is in the tree (after <see cref="Godot.Node._Ready" />).
	///     If it throws, the Autoload fails and its <see cref="AutoloadFailurePolicy" /> is applied.
	/// </summary>
	void Initialize() { }
}
