namespace Root.Autoloading;

public interface IAutoload
{
	// We use our own custom method similar to _Ready that should (probably) be called after actual _Ready?
	// This lets us detect if an autoload fails while its code is initializing instead of adding try-catch boilerplate.
	void Initialize() { }
}
