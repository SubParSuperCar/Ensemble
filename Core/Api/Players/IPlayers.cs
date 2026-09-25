namespace EnsembleCoreRoot.Api.Players;

/// <summary>
///     The registry of <see cref="IPlayer" /> objects, including which one, if any, is the local player.
/// </summary>
public interface IPlayers
{
	IReadOnlyDictionary<Guid, IPlayer> All { get; }
	IPlayer? Local { get; }

	event Action<IPlayer> Added;
	event Action<IPlayer> Removed;
	event Action<IPlayer?> LocalChanged;

	IPlayer Add(Guid? id = null, string? name = null);
	void Remove(Guid id);

	void SetLocal(Guid? id = null);
}
