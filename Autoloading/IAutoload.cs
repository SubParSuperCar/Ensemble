namespace Root.Autoloading;

public interface IAutoload
{
	/// <summary>
	///     Called and observed for errors by the Autoload system.
	///     Functionally equivalent to <see cref="Godot.Node._Ready" />.
	///     If this method throws, the Autoload is declared a failure
	///     and its <see cref="AutoloadFailurePolicy" /> is applied.
	/// </summary>
	void Initialize() { }
}
