using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Player;

namespace Root.Core.Gd.Player;

public partial class GdPlayers : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdPlayer player);

	[Signal]
	public delegate void LocalChangedEventHandler(GdPlayer? player);

	[Signal]
	public delegate void RemovedEventHandler(GdPlayer player);

	private static readonly ConditionalWeakTable<IPlayers, GdPlayers> Wrappers = [];
	private IPlayers _source = null!;

	public int Count => _source.All.Count;
	public GdPlayer? Local => _source.Local is { } local ? GdPlayer.From(local) : null;

	public static GdPlayers From(IPlayers players) =>
		Wrappers.GetValue(players,
			static source =>
			{
				var wrapper = new GdPlayers { _source = source };

				source.Added += player => wrapper.EmitSignal(SignalName.Added, GdPlayer.From(player));
				source.Removed += player => wrapper.EmitSignal(SignalName.Removed, GdPlayer.From(player));

				source.LocalChanged += player
					=> wrapper.EmitSignal(SignalName.LocalChanged, (player is null ? null : GdPlayer.From(player))!);

				return wrapper;
			});

	public GdPlayer? GetPlayer(string id) =>
		Guid.TryParse(id, out var guid) && _source.All.TryGetValue(guid, out var player)
			? GdPlayer.From(player)
			: null;

	public Array<GdPlayer> GetAll()
	{
		var result = new Array<GdPlayer>();

		foreach (var player in _source.All.Values)
			result.Add(GdPlayer.From(player));

		return result;
	}

	public GdPlayer Add() => Add(string.Empty);
	public GdPlayer Add(string id) => Add(id, string.Empty);

	public GdPlayer Add(string id, string name) =>
		GdPlayer.From(_source.Add(
			id == string.Empty ? null : Guid.Parse(id),
			name == string.Empty ? null : name));

	public void Remove(string id)
	{
		if (Guid.TryParse(id, out var guid))
			_source.Remove(guid);
	}

	public void SetLocal(string id) => SetLocal(id, string.Empty);

	public void SetLocal(string id, string name)
	{
		if (!Guid.TryParse(id, out var guid))
			return;

		if (!_source.All.ContainsKey(guid))
			_source.Add(guid, name == string.Empty ? null : name);

		_source.SetLocal(guid);
	}

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var player in _source.All.Values)
			result.Add(GdPlayer.From(player).ToDict());

		return result;
	}
}
