using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IAsset" />
public partial class GdAsset : RefCounted
{
	private static readonly ConditionalWeakTable<IAsset, GdAsset> Wrappers = [];

	/// <inheritdoc cref="IAsset" />
	public IAsset Source { get; private init; } = null!;

	public int Id => Source.Id;
	public string Name => Source.Name;

	public int MaxInstanceCount => Source.MaxInstanceCount;

	public Dictionary Properties => Converter.ToGodotProperties(Source.Properties);

	public static GdAsset From(IAsset asset) =>
		Wrappers.GetValue(asset, static source => new GdAsset { Source = source });

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["name"] = Name,
			["maxInstanceCount"] = MaxInstanceCount,
			["properties"] = Properties
		};

	public override string ToString() => Source.ToString()!;
}
