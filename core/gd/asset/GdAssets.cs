using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;

namespace Root.Core.Gd.Asset;

public partial class GdAssets : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdAsset asset);

	[Signal]
	public delegate void RemovedEventHandler(GdAsset asset);

	private static readonly ConditionalWeakTable<IAssets, GdAssets> Wrappers = [];
	private IAssets _source = null!;

	public int Count => _source.All.Count;
	public bool IsLocked => _source.IsLocked;

	public static GdAssets From(IAssets assets) =>
		Wrappers.GetValue(assets,
			static source =>
			{
				var wrapper = new GdAssets { _source = source };

				source.Added += asset => wrapper.EmitSignal(SignalName.Added, GdAsset.From(asset));
				source.Removed += asset => wrapper.EmitSignal(SignalName.Removed, GdAsset.From(asset));

				return wrapper;
			});

	public GdAsset? Get(int id) => _source.All.TryGetValue(id, out var asset) ? GdAsset.From(asset) : null;

	public Array<GdAsset> GetAll()
	{
		var result = new Array<GdAsset>();

		foreach (var asset in _source.All.Values)
			result.Add(GdAsset.From(asset));

		return result;
	}

	public GdAsset Add(int id) => Add(id, string.Empty);
	public GdAsset Add(int id, string name) => Add(id, name, null, Default);
	public GdAsset Add(int id, string name, Dictionary properties) => Add(id, name, properties, Default);

	public GdAsset Add(int id, string name, Dictionary? properties, int maxInstanceCount) =>
		GdAsset.From(_source.Add(
			id,
			name == string.Empty ? null : name,
			properties is null ? null : Converter.FromGodotProperties(properties),
			maxInstanceCount is Default ? null : maxInstanceCount));

	public void Lock() => _source.Lock();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var asset in _source.All.Values)
			result.Add(GdAsset.From(asset).ToDict());

		return result;
	}
}
