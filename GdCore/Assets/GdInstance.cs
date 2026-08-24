using System.Runtime.CompilerServices;
using CoreRoot.Api.Assets;
using Godot;
using Godot.Collections;
using Root.GdCore.Utils;

namespace Root.GdCore.Assets;

public partial class GdInstance : RefCounted
{
	private static readonly ConditionalWeakTable<IInstance, GdInstance> Wrappers = [];
	private IInstance _source = null!;

	public int Id => _source.Id;

	public GdAsset Asset => GdAsset.From(_source.Asset);
	public GdProperties Properties => field ??= GdProperties.From(_source.Properties);

	public Vector3 Position => _source.Position.ToGodot();
	public Quaternion Rotation => _source.Rotation.ToGodot();

	public static GdInstance From(IInstance instance) =>
		Wrappers.GetValue(instance, static source => new GdInstance { _source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["assetId"] = Asset.Id,
			["position"] = Position,
			["rotation"] = Rotation,
			["properties"] = Properties.GetAll()
		};

	public override string ToString() => _source.ToString()!;
}
