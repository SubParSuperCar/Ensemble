namespace Root.Autoloading;

[Flags]
public enum AutoloadScope : byte
{
	/// <summary>
	///     This Autoload will never be instantiated. Effectively disabled.
	/// </summary>
	None = 0,

	/// <summary>
	///     This Autoload will be run on clients, where rendering is enabled.
	/// </summary>
	RegularClient = 1 << 0,

	/// <summary>
	///     This Autoload will be run on headless servers, where rendering is disabled.
	/// </summary>
	HeadlessServer = 1 << 1
}
