using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IInstance" />
public partial class GdInstance : RefCounted
{
	private static readonly ConditionalWeakTable<IInstance, GdInstance> Wrappers = [];

	/// <inheritdoc cref="IInstance" />
	public IInstance Source { get; private init; } = null!;

	public int Id => Source.Id;

	/// <inheritdoc cref="GdAsset" />
	public GdAsset Asset => GdAsset.From(Source.Asset);

	/// <inheritdoc cref="GdProperties" />
	public GdProperties Properties => field ??= GdProperties.From(Source.Properties);

	public Vector3 Position => Source.Position.ToGodot();
	public Quaternion Rotation => Source.Rotation.ToGodot();

	public static GdInstance From(IInstance instance) =>
		Wrappers.GetValue(instance, static source => new GdInstance { Source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["assetId"] = Asset.Id,
			["position"] = Position,
			["rotation"] = Rotation,
			["properties"] = Properties.GetAll()
		};

	public override string ToString() => Source.ToString()!;
}
