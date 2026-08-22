namespace Root.Autoloading;

[Flags]
public enum AutoloadScope : byte // We should always use byte when possible for optimization.
{
	// ReSharper disable once UnusedMember.Global
	None = 0, // The autoload runs on no contexts. Essentially disables the autoload. Good for testing.
	Client = 1 << 0, // The autoload runs on the client. Good for ensuring UI or Discord RPC only runs when a normal user is playing. These aren't run on headless servers.
	Server = 1 << 1 // The autoload runs on the server. Good for things like Core and baseline components that should usually run always.
}
