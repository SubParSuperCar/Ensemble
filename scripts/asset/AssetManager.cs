using System.Globalization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Root.Core.Gd.Asset;
using Root.Scripts.Globals;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Asset;

public partial class AssetManager : Node
{
	public Godot.Collections.Dictionary<int, PackedScene> Scenes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxInstanceCount { get; set; }

	[GeneratedRegex(@"\.t?scn$", RegexOptions.Compiled, (int)TimeSpan.MillisecondsPerSecond)]
	private static partial Regex SceneFileRegex { get; }

	public override void _EnterTree()
	{
		GAssetManager = this;

		if (GAssets.IsLocked)
			return;

		ScanDirectory(ScriptConstants.BuildAssetsDir);
		GAssets.Lock();
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GAssetManager, this))
			GAssetManager = null!;
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
		using var directory = DirAccess.Open(path);

		if (directory is null)
			return;

		directory.ListDirBegin();

		for (var entry = directory.GetNext(); entry != string.Empty; entry = directory.GetNext())
		{
			var entryPath = path.PathJoin(entry);

			if (directory.CurrentIsDir())
				ScanDirectory(entryPath);
			else if (SceneFileRegex.IsMatch(entryPath))
				RegisterScene(entryPath);
		}

		directory.ListDirEnd();
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
			return;

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
