namespace EnsembleCoreRoot.Globals;

public static class Guids
{
	// UUIDv4, not v7, for temporal privacy: v7 embeds an approximate creation time
	public static Guid Create() => Guid.NewGuid();
}
