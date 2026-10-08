using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Players;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.GdCore.Players;

/// <inheritdoc cref="IPlayer" />
public partial class GdPlayer : RefCounted
{
	private static readonly ConditionalWeakTable<IPlayer, GdPlayer> Wrappers = [];

	/// <inheritdoc cref="IPlayer" />
	public IPlayer Source { get; private init; } = null!;

	public string Id => Source.Id.ToString();
	public string Name => Source.Name;

	public double UtcCreatedAtUnix => Source.UtcCreatedAt.ToUnixTimeMilliseconds() / 1000d;

	public static GdPlayer From(IPlayer player) =>
		Wrappers.GetValue(player, static source => new GdPlayer { Source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["name"] = Name,
			["utcCreatedAtUnix"] = UtcCreatedAtUnix
		};

	public override string ToString() => Source.ToString()!;
}
