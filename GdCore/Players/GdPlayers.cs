using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Players;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Players;

/// <inheritdoc cref="IPlayers" />
public partial class GdPlayers : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdPlayer player);

	[Signal]
	public delegate void LocalChangedEventHandler(GdPlayer? player);

	[Signal]
	public delegate void RemovedEventHandler(GdPlayer player);

	private static readonly ConditionalWeakTable<IPlayers, GdPlayers> Wrappers = [];

	/// <inheritdoc cref="IPlayers" />
	public IPlayers Source { get; private init; } = null!;

	public int Count => Source.All.Count;
	public GdPlayer? Local => Source.Local is { } local ? GdPlayer.From(local) : null;

	public static GdPlayers From(IPlayers players) =>
		Wrappers.GetValue(players,
			static source =>
			{
				var wrapper = new GdPlayers { Source = source };

				source.Added += player => wrapper.EmitSignal(SignalName.Added, GdPlayer.From(player));
				source.Removed += player => wrapper.EmitSignal(SignalName.Removed, GdPlayer.From(player));

				source.LocalChanged += player =>
					wrapper.EmitSignal(SignalName.LocalChanged, (player is null ? null : GdPlayer.From(player))!);

				return wrapper;
			});

	public GdPlayer? GetPlayer(string id) =>
		Guid.TryParse(id, out var guid) && Source.All.TryGetValue(guid, out var player)
			? GdPlayer.From(player)
			: null;

	public Array<GdPlayer> GetAll()
	{
		var result = new Array<GdPlayer>();

		foreach (var player in Source.All.Values)
			result.Add(GdPlayer.From(player));

		return result;
	}

	public GdPlayer Add() => Add(string.Empty);
	public GdPlayer Add(string id) => Add(id, string.Empty);

	public GdPlayer Add(string id, string name) =>
		GdPlayer.From(Source.Add(
			id == string.Empty ? null : Guid.Parse(id),
			name == string.Empty ? null : name));

	public void Remove(string id)
	{
		if (Guid.TryParse(id, out var guid))
			Source.Remove(guid);
	}

	public void SetLocal(string id) => SetLocal(id, string.Empty);

	public void SetLocal(string id, string name)
	{
		if (!Guid.TryParse(id, out var guid))
			return;

		if (!Source.All.ContainsKey(guid))
			Source.Add(guid, name == string.Empty ? null : name);

		Source.SetLocal(guid);
	}

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var player in Source.All.Values)
			result.Add(GdPlayer.From(player).ToDict());

		return result;
	}
}
