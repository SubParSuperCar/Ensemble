using Godot;

namespace Root.Scripts.Asset;

public partial class AssetHandle : Node3D
{
	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	[Export] public Godot.Collections.Dictionary<StringName, Variant> Properties { get; set; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxInstanceCount { get; set; }

	// ReSharper disable once MemberCanBePrivate.Global
	// Only store non-exported members that are necessary for referencing the associated Core object or are not included in it
	public int InstanceId { get; set; }
}
