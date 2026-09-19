namespace Root.Autoloading;

/// <summary>
///     The run contexts in which this Autoload is instantiated.
/// </summary>
[Flags]
public enum AutoloadScope : byte
{
	/// <summary>
	///     This Autoload is never run, effectively disabling it.
	/// </summary>
	None = 0,

	/// <summary>
	///     This Autoload is run on regular clients, where rendering is enabled.
	/// </summary>
	RegularClient = 1 << 0,

	/// <summary>
	///     This Autoload is run on headless servers, where rendering is disabled.
	/// </summary>
	HeadlessServer = 1 << 1
}
