using Godot;
using Godot.Collections;
using Root.Globals;

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
		Scan(Constants.AssetsDir);
	}

	private void Scan(string path)
	{
		using var dir = DirAccess.Open(path);

		if (dir is null)
			return;

		dir.ListDirBegin();

		string entry;

		while ((entry = dir.GetNext()) != string.Empty)
		{
			var fullPath = path.PathJoin(entry);

			if (dir.CurrentIsDir())
				Scan(fullPath);
			else if (entry.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
				TryAdd(fullPath);
		}

		dir.ListDirEnd();
	}

	private void TryAdd(string path)
	{
		var packed = GD.Load<PackedScene>(path);

		if (packed is null)
			return;

		var node = packed.Instantiate();

		if (node is not AssetHandle handle)
		{
			node.Free();
			return;
		}

		var assetId = handle.AssetId;
		var assetName = handle.AssetName;
		var properties = handle.Properties;
		var maxInstanceCount = handle.MaxInstanceCount;

		node.Free();

		if (!Scenes.TryAdd(assetId, packed))
			return;

		var converted = new Dictionary();

		foreach (var (key, value) in properties)
			converted.Add(key.ToString(), value);

		Assets.Add(
			assetId,
			assetName,
			converted,
			maxInstanceCount == 0 ? DefaultMaxInstanceCount : maxInstanceCount);
	}
}
