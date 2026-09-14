namespace Root.Autoloading;

/// <summary>
///     The run contexts that this Autoload should be instantiated in.
/// </summary>
[Flags]
public enum AutoloadScope : byte
{
	/// <summary>
	///     This Autoload may never be run. Effectively disabled.
	/// </summary>
	None = 0,

	/// <summary>
	///     This Autoload may be run on clients, where rendering is enabled.
	/// </summary>
	RegularClient = 1 << 0,

	/// <summary>
	///     This Autoload may be run on headless servers, where rendering is disabled.
	/// </summary>
	HeadlessServer = 1 << 1
}
