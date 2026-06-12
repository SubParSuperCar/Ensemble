using Godot;

namespace Root.Scripts.Asset;

public partial class AssetHandle : Node3D
{
	[Export(PropertyHint.Range, "0,0,1,or_greater")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	[Export] public Godot.Collections.Dictionary<StringName, Variant> Properties { get; set; } = null!;

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int MaxInstanceCount { get; set; }
}
