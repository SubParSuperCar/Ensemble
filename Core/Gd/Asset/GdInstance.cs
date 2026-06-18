using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;

namespace Root.Core.Gd.Asset;

[GlobalClass]
public partial class GdInstance : RefCounted
{
	private static readonly ConditionalWeakTable<IInstance, GdInstance> Cache = [];
	private IInstance _instance = null!;

	public int Id => _instance.Id;

	public GdAsset Asset => GdAsset.From(_instance.Asset);
	public GdProperties Properties => field ??= GdProperties.From(_instance.Properties);

	public Vector3 Position => _instance.Position.ToGodot();
	public Quaternion Rotation => _instance.Rotation.ToGodot();

	public static GdInstance From(IInstance instance)
		=> Cache.GetValue(instance, static value => new GdInstance { _instance = value });

	public Dictionary ToDict() => new()
	{
		["id"] = Id,
		["assetId"] = Asset.Id,
		["position"] = Position,
		["rotation"] = Rotation,
		["properties"] = Properties.GetAll()
	};

	public override string ToString() => _instance.ToString()!;
}
