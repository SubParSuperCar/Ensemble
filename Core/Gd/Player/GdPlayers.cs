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
		static value =>
		{
			var wrapper = new GdPlayers { _players = value };

			value.Added += player => wrapper.EmitSignal(SignalName.Added, GdPlayer.From(player));
			value.Removed += player => wrapper.EmitSignal(SignalName.Removed, GdPlayer.From(player));

			value.LocalChanged += player
				=> wrapper.EmitSignal(SignalName.LocalChanged, (player is null ? null : GdPlayer.From(player))!);

			return wrapper;
		});

	public GdPlayer? Get(string id)
		=> Guid.TryParse(id, out var guid) && _players.All.TryGetValue(guid, out var player)
			? GdPlayer.From(player)
			: null;

	public Array<GdPlayer> GetAll()
	{
		var result = new Array<GdPlayer>();

		foreach (var player in _players.All.Values)
			result.Add(GdPlayer.From(player));

		return result;
	}

	public GdPlayer Add() => Add(string.Empty, string.Empty);
	public GdPlayer Add(string id) => Add(id, string.Empty);

	public GdPlayer Add(string id, string name) => GdPlayer.From(_players.Add(
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

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var player in _players.All.Values)
			result.Add(GdPlayer.From(player).ToDict());

		return result;
	}
}
