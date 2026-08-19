using System.Runtime.CompilerServices;
using CoreRoot.Api.Players;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Systems.GdCore.Players;

public partial class GdPlayer : RefCounted
{
	private static readonly ConditionalWeakTable<IPlayer, GdPlayer> Wrappers = [];
	private IPlayer _source = null!;

	public string Id => _source.Id.ToString();
	public string Name => _source.Name;

	public double UtcCreatedAtUnix => _source.UtcCreatedAt.ToUnixTimeSeconds();

	public static GdPlayer From(IPlayer player) =>
		Wrappers.GetValue(player, static source => new GdPlayer { _source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["name"] = Name,
			["utcCreatedAtUnix"] = UtcCreatedAtUnix
		};

	public override string ToString() => _source.ToString()!;
}
