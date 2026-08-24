using System.Globalization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Serilog;

namespace Root.Scripts.Assets;

[GlobalClass]
public partial class AssetManager : Node
{
	public Godot.Collections.Dictionary<int, PackedScene> Scenes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxInstanceCount { get; set; }

	[GeneratedRegex(@"\.t?scn$", RegexOptions.Compiled, 100)]
	private static partial Regex SceneFileRegex { get; }

	public override void _EnterTree() => GAssetManager = this;

	public override void _ExitTree()
	{
		if (ReferenceEquals(GAssetManager, this))
			GAssetManager = null!;
	}

	public override void _Ready()
	{
		if (GAssets.IsLocked)
		{
			Log.Warning("{Class} is locked.", nameof(GAssets));
			return;
		}

		Log.Debug("Scanning and registering assets from: {Directory}", BuildAssetsDir);

		ScanDirectory(BuildAssetsDir);
		GAssets.Lock();

		Log.Debug("Registered {Count} asset(s).", Scenes.Count);
	}

	public PackedScene? GetPackedOrNull(int assetId) => Scenes.TryGetValue(assetId, out var packed) ? packed : null;

	public PackedScene GetPacked(int assetId) =>
		GetPackedOrNull(assetId) ?? throw new InvalidOperationException(string.Create(
			CultureInfo.InvariantCulture,
			$"Packed scene with asset id {assetId} not found."));

	private void ScanDirectory(string path)
	{
		foreach (var entry in ResourceLoader.ListDirectory(path))
		{
			var entryPath = path.PathJoin(entry);

			if (entry.EndsWith('/'))
				ScanDirectory(entryPath);
			else if (SceneFileRegex.IsMatch(entryPath))
				RegisterScene(entryPath);
		}
	}

	private void RegisterScene(string path)
	{
		var scene = GD.Load<PackedScene>(path);
		if (scene is null)
			return;

		var instance = scene.Instantiate();
		if (instance is not AssetHandle handle)
		{
			instance.Free();
			return;
		}

		var id = handle.AssetId;
		var name = handle.AssetName;
		var properties = handle.Properties;
		var maxInstanceCount = handle.MaxInstanceCount;

		instance.Free();

		if (!Scenes.TryAdd(id, scene))
		{
			Log.Warning("Duplicate asset id {AssetId} at: {Path}", id, path);
			return;
		}

		var converted = new Dictionary();
		foreach (var (key, value) in properties)
			converted.Add(key.ToString(), value);

		GAssets.Add(
			id,
			name,
			converted,
			maxInstanceCount is Default ? DefaultMaxInstanceCount : maxInstanceCount);
	}
}
