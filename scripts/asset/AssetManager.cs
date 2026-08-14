using System.Globalization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Root.Common.Globals;
using Root.Core.Gd.Asset;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Asset;

[GlobalClass]
public partial class AssetManager : Node
{
	public Godot.Collections.Dictionary<int, PackedScene> Scenes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxInstanceCount { get; set; }

	[GeneratedRegex(@"\.t?scn$", RegexOptions.Compiled, (int)TimeSpan.MillisecondsPerSecond)]
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
			Log.Warning("{Class} is already locked; skipping asset scan", nameof(GAssets));
			return;
		}

		Log.Debug("Scanning and registering assets from {Directory}", CommonConstants.BuildAssetsDir);

		ScanDirectory(CommonConstants.BuildAssetsDir);
		GAssets.Lock();

		Log.Debug("Finished scanning and registering assets");
	}

	public PackedScene GetPacked(GdAsset asset) => GetPacked(asset.Id);

	public PackedScene GetPacked(int assetId) =>
		Scenes.TryGetValue(assetId, out var packed)
			? packed
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Packed scene with asset id {assetId} not found"));

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
			Log.Warning("Duplicate asset id {AssetId} at {Path}; skipping registration", id, path);
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
