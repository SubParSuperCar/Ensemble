namespace Root.Autoloading;

[Flags]
public enum AutoloadScope : byte
{
	// ReSharper disable once UnusedMember.Global
	None = 0,
	Client = 1 << 0,
	Server = 1 << 1
}
