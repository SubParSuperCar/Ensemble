using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Player;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Core.Gd.Player;

[GlobalClass]
public partial class GdPlayer : RefCounted
{
	private static readonly ConditionalWeakTable<IPlayer, GdPlayer> Cache = [];
	private IPlayer _player = null!;

	public string Id => _player.Id.ToString();
	public string Name => _player.Name;

	public double UtcCreatedAtUnix => new DateTimeOffset(_player.UtcCreatedAt).ToUnixTimeSeconds();

	public static GdPlayer From(IPlayer player)
		=> Cache.GetValue(player, static value => new GdPlayer { _player = value });

	public Dictionary ToDict() => new()
	{
		["id"] = Id,
		["name"] = Name,
		["utcCreatedAtUnix"] = UtcCreatedAtUnix
	};

	public override string ToString() => _player.ToString()!;
}
