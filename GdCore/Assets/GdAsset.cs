using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IAsset" />
public partial class GdAsset : RefCounted
{
	private static readonly ConditionalWeakTable<IAsset, GdAsset> Wrappers = [];

	private IAsset _source = null!;

	public int Id => _source.Id;
	public string Name => _source.Name;

	public int MaxInstanceCount => _source.MaxInstanceCount;

	public Dictionary Properties => Converter.ToGodotProperties(_source.Properties);

	public static GdAsset From(IAsset asset) =>
		Wrappers.GetValue(asset, static source => new GdAsset { _source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["name"] = Name,
			["maxInstanceCount"] = MaxInstanceCount,
			["properties"] = Properties
		};

	public override string ToString() => _source.ToString()!;
}
