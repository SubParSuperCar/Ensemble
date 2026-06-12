namespace Root.Core.Api.Player;

public interface IPlayers
{
	IReadOnlyDictionary<Guid, IPlayer> All { get; }
	IPlayer? Local { get; }

	IPlayer Add(Guid? id = null, string? name = null);
	void Remove(Guid id);

	event Action<IPlayer> Added;
	event Action<IPlayer> Removed;

	void SetLocal(Guid? id = null);
	event Action<IPlayer?> LocalChanged;

	void Reset();
}
