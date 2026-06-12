using Root.Core.Api.Player;

namespace Root.Core.Impl.Player;

public class Players : IPlayers
{
	private readonly Dictionary<Guid, IPlayer> _players = [];

	public IReadOnlyDictionary<Guid, IPlayer> All => _players;

	public IPlayer? Local { get; private set; }

	public event Action<IPlayer>? Added;
	public event Action<IPlayer>? Removed;
	public event Action<IPlayer?>? LocalChanged;

	public IPlayer Add(Guid? id = null, string? name = null)
	{
		id ??= Guid.NewGuid();

		if (_players.ContainsKey(id.Value))
			throw new InvalidOperationException($"Player with id {id} already exists");

		var player = new Player(id.Value, name);
		_players.Add(id.Value, player);
		Added?.Invoke(player);

		return player;
	}

	public void Remove(Guid id)
	{
		if (!_players.Remove(id, out var player))
			throw new KeyNotFoundException($"Player with id {id} not found");

		if (ReferenceEquals(player, Local))
			SetLocal();

		Removed?.Invoke(player);
	}

	public void SetLocal(Guid? id = null)
	{
		IPlayer? player = null;

		if (id is { } guid && !_players.TryGetValue(guid, out player))
			throw new KeyNotFoundException($"Player with id {id} not found");

		if (ReferenceEquals(player, Local))
			return;

		Local = player;
		LocalChanged?.Invoke(player);
	}

	public void Reset()
	{
		_players.Clear();
		Local = null;
	}
}
