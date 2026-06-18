using Godot;
using Godot.Collections;
using Root.Gd.Globals;

namespace Root.Gd.Asset;

public partial class AssetManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, PackedScene> Scenes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int DefaultMaxInstanceCount { get; set; }

	public override void _EnterTree()
	{
		GdGlobals.AssetManager = this;
		ScanDirectory(Constants.AssetsDir);
	}

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
			else if (entry.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
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

		Assets.Add(
			id,
			name,
			converted,
			maxInstanceCount == 0 ? DefaultMaxInstanceCount : maxInstanceCount);
	}
}
