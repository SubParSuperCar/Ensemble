using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IAssets" />
public partial class GdAssets : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdAsset asset);

	[Signal]
	public delegate void RemovedEventHandler(GdAsset asset);

	private static readonly ConditionalWeakTable<IAssets, GdAssets> Wrappers = [];

	/// <inheritdoc cref="IAssets" />
	public IAssets Source { get; private init; } = null!;

	public int Count => Source.All.Count;
	public bool IsLocked => Source.IsLocked;

	public static GdAssets From(IAssets assets) =>
		Wrappers.GetValue(assets,
			static source =>
			{
				var wrapper = new GdAssets { Source = source };

				source.Added += asset => wrapper.EmitSignal(SignalName.Added, GdAsset.From(asset));
				source.Removed += asset => wrapper.EmitSignal(SignalName.Removed, GdAsset.From(asset));

				return wrapper;
			});

	public GdAsset? GetAsset(int id) => Source.All.TryGetValue(id, out var asset) ? GdAsset.From(asset) : null;

	public Array<GdAsset> GetAll() => [.. Source.All.Values.Select(GdAsset.From)];

	// Explicit overloads instead of default arguments, which GDScript callers can't omit
	public GdAsset Add(int id) => Add(id, string.Empty);
	public GdAsset Add(int id, string name) => Add(id, name, null, Default);
	public GdAsset Add(int id, string name, Dictionary properties) => Add(id, name, properties, Default);

	public GdAsset Add(int id, string name, Dictionary? properties, int maxInstanceCount) =>
		GdAsset.From(Source.Add(
			id,
			name == string.Empty ? null : name,
			properties is null ? null : Converter.FromGodotProperties(properties),
			maxInstanceCount is Default ? null : maxInstanceCount));

	public void Lock() => Source.Lock();

	public Array<Dictionary> GetAllDicts() =>
		[.. Source.All.Values.Select(static asset => GdAsset.From(asset).ToDict())];
}
