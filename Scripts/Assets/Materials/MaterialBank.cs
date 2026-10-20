using EnsembleRoot.GdCore.Assets;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Materials;

/// <summary>The game's asset materials, indexed by the material ID property; None (-1) means no material.</summary>
[GlobalClass]
public partial class MaterialBank : Node
{
	public const string PropertyKey = "_materialId";

	// A C# copy, so lookups during spawning don't marshal
	private AssetMaterial[] _materials = [];

	public static MaterialBank? Instance { get; private set; }

	[Export] public Array<AssetMaterial> Materials { get; set; } = [];

	public override void _EnterTree()
	{
		_materials = [.. Materials];
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public static AssetMaterial? Get(int materialId) =>
		Instance?._materials is { } materials && materialId >= 0 && materialId < materials.Length
			? materials[materialId]
			: null;

	public static AssetMaterial? Get(GdProperties properties) =>
		properties.GetValue(PropertyKey) is { VariantType: Variant.Type.Int } materialId
			? Get(materialId.AsInt32())
			: null;
}
