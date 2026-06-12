using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Player;

namespace Root.Core.Gd.Player;

[GlobalClass]
public partial class GdPlayers : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdPlayer player);

	[Signal]
	public delegate void LocalChangedEventHandler(GdPlayer? player);

	[Signal]
	public delegate void RemovedEventHandler(GdPlayer player);

	private static readonly ConditionalWeakTable<IPlayers, GdPlayers> Cache = [];
	private IPlayers _players = null!;

	public int Count => _players.All.Count;
	public GdPlayer? Local => _players.Local is { } local ? GdPlayer.From(local) : null;

	public static GdPlayers From(IPlayers players) => Cache.GetValue(players,
		static p =>
		{
			var gdPlayers = new GdPlayers { _players = p };

			p.Added += player => gdPlayers.EmitSignal(SignalName.Added, GdPlayer.From(player));
			p.Removed += player => gdPlayers.EmitSignal(SignalName.Removed, GdPlayer.From(player));

			p.LocalChanged += player
				=> gdPlayers.EmitSignal(SignalName.LocalChanged, (player is null ? null : GdPlayer.From(player))!);

			return gdPlayers;
		});

	public GdPlayer? Get(string id)
		=> Guid.TryParse(id, out var guid) && _players.All.TryGetValue(guid, out var player)
			? GdPlayer.From(player)
			: null;

	public Array<GdPlayer> GetAll()
	{
		var players = new Array<GdPlayer>();

		foreach (var player in _players.All.Values)
			players.Add(GdPlayer.From(player));

		return players;
	}

	public GdPlayer Add() => Add(string.Empty, string.Empty);
	public GdPlayer Add(string id) => Add(id, string.Empty);

	public GdPlayer Add(string id, string name) =>
		GdPlayer.From(_players.Add(
			id == string.Empty ? null : Guid.Parse(id),
			name == string.Empty ? null : name));

	public void Remove(string id)
	{
		if (Guid.TryParse(id, out var guid))
			_players.Remove(guid);
	}

	public void SetLocal(string id) => SetLocal(id, string.Empty);

	public void SetLocal(string id, string name)
	{
		if (!Guid.TryParse(id, out var guid))
			return;

		if (!_players.All.ContainsKey(guid))
			_players.Add(guid, name == string.Empty ? null : name);

		_players.SetLocal(guid);
	}

	public void Reset() => _players.Reset();

	public Array<Dictionary> GetAllDicts()
	{
		var dicts = new Array<Dictionary>();

		foreach (var player in _players.All.Values)
			dicts.Add(GdPlayer.From(player).ToDict());

		return dicts;
	}
}
