namespace EnsembleRoot.Autoloading;

/// <summary>
///     The run contexts in which this Autoload is instantiated.
/// </summary>
[Flags]
public enum AutoloadScope : byte
{
	/// <summary>
	///     Never instantiated, which disables the Autoload.
	/// </summary>
	None = 0,

	/// <summary>
	///     Instantiated on regular clients, which render.
	/// </summary>
	RegularClient = 1 << 0,

	/// <summary>
	///     Instantiated on headless servers, which don't render.
	/// </summary>
	HeadlessServer = 1 << 1
}
