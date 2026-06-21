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
	private static readonly ConditionalWeakTable<IAsset, GdAsset> Wrappers = [];
	private IAsset _source = null!;

	public int Id => _source.Id;
	public string Name => _source.Name;

	public int MaxInstanceCount => _source.MaxInstanceCount;

	public Dictionary Properties => Convert.ToGodotProperties(_source.Properties);

	public static GdAsset From(IAsset asset)
		=> Wrappers.GetValue(asset, static source => new GdAsset { _source = source });

	public Dictionary ToDict() => new()
	{
		["id"] = Id,
		["name"] = Name,
		["maxInstanceCount"] = MaxInstanceCount,
		["properties"] = Properties
	};

	public override string ToString() => _source.ToString()!;
}
