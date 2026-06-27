using Root.Core.Api.Player;

namespace Root.Core.Impl.Player;

public class Players : IPlayers
{
	private readonly Dictionary<Guid, IPlayer> _playersById = [];

	public IReadOnlyDictionary<Guid, IPlayer> All => _playersById;
	public IPlayer? Local { get; private set; }

	public event Action<IPlayer>? Added;
	public event Action<IPlayer>? Removed;
	public event Action<IPlayer?>? LocalChanged;

	public IPlayer Add(Guid? id = null, string? name = null)
	{
		var playerId = id ?? Guid.NewGuid();

		if (_playersById.ContainsKey(playerId))
			throw new InvalidOperationException($"Player with id {playerId} already exists");

		var player = new Player(playerId, name);
		_playersById.Add(playerId, player);
		Added?.Invoke(player);

		return player;
	}

	public void Remove(Guid id)
	{
		if (!_playersById.Remove(id, out var player))
			throw new KeyNotFoundException($"Player with id {id} not found");

		if (ReferenceEquals(player, Local))
			SetLocal();

		Removed?.Invoke(player);
	}

	public void SetLocal(Guid? id = null)
	{
		IPlayer? player = null;

		if (id is { } playerId && !_playersById.TryGetValue(playerId, out player))
			throw new KeyNotFoundException($"Player with id {id} not found");

		if (ReferenceEquals(player, Local))
			return;

		Local = player;
		LocalChanged?.Invoke(player);
	}

	internal void Reset()
	{
		foreach (var player in _playersById.Values.ToArray())
			Remove(player.Id);
	}
}
