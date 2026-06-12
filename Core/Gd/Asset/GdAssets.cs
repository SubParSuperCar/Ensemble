using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;

namespace Root.Core.Gd.Asset;

[GlobalClass]
public partial class GdAssets : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdAsset asset);

	private static readonly ConditionalWeakTable<IAssets, GdAssets> Cache = [];
	private IAssets _assets = null!;

	public int Count => _assets.All.Count;
	public bool IsLocked => _assets.IsLocked;

	public static GdAssets From(IAssets assets) => Cache.GetValue(assets,
		static a =>
		{
			var gdAssets = new GdAssets { _assets = a };
			a.Added += asset => gdAssets.EmitSignal(SignalName.Added, GdAsset.From(asset));

			return gdAssets;
		});

	public GdAsset? Get(int id)
		=> _assets.All.TryGetValue(id, out var asset) ? GdAsset.From(asset) : null;

	public Array<GdAsset> GetAll()
	{
		var assets = new Array<GdAsset>();

		foreach (var asset in _assets.All.Values)
			assets.Add(GdAsset.From(asset));

		return assets;
	}

	public GdAsset Add(int id) => Add(id, string.Empty, null, 0);
	public GdAsset Add(int id, string name) => Add(id, name, null, 0);
	public GdAsset Add(int id, string name, Dictionary properties) => Add(id, name, properties, 0);

	public GdAsset Add(int id, string name, Dictionary? properties, int maxInstanceCount)
		=> GdAsset.From(_assets.Add(
			id,
			name == string.Empty ? null : name,
			properties is null ? null : GdConvert.FromGodotProperties(properties),
			maxInstanceCount == 0 ? null : maxInstanceCount));

	public void Lock() => _assets.Lock();
	public void Reset() => _assets.Reset();

	public Array<Dictionary> GetAllDicts()
	{
		var dicts = new Array<Dictionary>();

		foreach (var asset in _assets.All.Values)
			dicts.Add(GdAsset.From(asset).ToDict());

		return dicts;
	}
}
