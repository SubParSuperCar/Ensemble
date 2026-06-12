using Godot;
using Godot.Collections;
using Root.Globals;

namespace Root.Scripts.Asset;

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

		string element;

		while ((element = dir.GetNext()) != string.Empty)
		{
			var fullPath = path.PathJoin(element);

			if (dir.CurrentIsDir())
				Scan(fullPath);
			else if (element.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
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

		if (node is not AssetHandle asset)
		{
			node.Free();
			return;
		}

		var assetId = asset.AssetId;
		var assetName = asset.AssetName;
		var properties = asset.Properties;
		var maxInstanceCount = asset.MaxInstanceCount;

		node.Free();

		if (!Scenes.TryAdd(assetId, packed))
			return;

		var gdProperties = new Dictionary();

		foreach (var (key, value) in properties)
			gdProperties.Add(key.ToString(), value);

		Assets.Add(
			assetId,
			assetName,
			gdProperties,
			maxInstanceCount == 0 ? DefaultMaxInstanceCount : maxInstanceCount);
	}
}
