namespace Root.Autoloading;

public interface IAutoload
{
	/// <summary>
	///     Called and observed for errors by the Autoload system.
	///     Effectively identical to <see cref="Godot.Node._Ready" /> in function.
	///     If an error occurs in this method,
	///     the Autoload will be declared a failure and the <see cref="AutoloadFailurePolicy" />
	///     will be initiated.
	/// </summary>
	void Initialize() { }
}
