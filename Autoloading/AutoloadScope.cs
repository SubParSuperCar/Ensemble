namespace Root.Autoloading;

[Flags]
public enum AutoloadScope : byte
{
	None = 0,
	Client = 1 << 0,
	Server = 1 << 1
}
