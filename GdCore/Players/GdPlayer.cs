using System.Runtime.CompilerServices;
using CoreRoot.Api.Players;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.GdCore.Players;

public partial class GdPlayer : RefCounted
{
	private static readonly ConditionalWeakTable<IPlayer, GdPlayer> Wrappers = [];
	private IPlayer _source = null!;

	public string Id => _source.Id.ToString();
	public string Name => _source.Name;

	// Doubles for unix timestamps can be consumed by GDScript. We should support both C# (obviously) and GDScript.
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
