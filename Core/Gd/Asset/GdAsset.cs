using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Convert = Root.Core.Gd.Util.Convert;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Core.Gd.Asset;

[GlobalClass]
public partial class GdAsset : RefCounted
{
	private static readonly ConditionalWeakTable<IAsset, GdAsset> Cache = [];
	private IAsset _asset = null!;

	public int Id => _asset.Id;
	public string Name => _asset.Name;

	public int MaxInstanceCount => _asset.MaxInstanceCount;

	public Dictionary Properties => Convert.ToGodotProperties(_asset.Properties);

	public static GdAsset From(IAsset asset)
		=> Cache.GetValue(asset, static value => new GdAsset { _asset = value });

	public Dictionary ToDict() => new()
	{
		["id"] = Id,
		["name"] = Name,
		["maxInstanceCount"] = MaxInstanceCount,
		["properties"] = Properties
	};

	public override string ToString() => _asset.ToString()!;
}
