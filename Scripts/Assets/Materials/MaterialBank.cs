using EnsembleRoot.GdCore.Assets;
using Godot;
using Godot.Collections;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Materials;

/// <summary>The game's asset materials, by their IDs, which the material ID property names; None (-1) means none.</summary>
[GlobalClass]
public partial class MaterialBank : Node
{
	public const string PropertyKey = "_materialId";

	// A C# copy, so lookups during spawning don't marshal
	private readonly System.Collections.Generic.Dictionary<int, AssetMaterial> _materialsById = [];

	public static MaterialBank? Instance { get; private set; }

	/// <summary>The materials by ID, in any order.</summary>
	[Export]
	public Array<AssetMaterial> Materials { get; set; } = [];

	/// <summary>The materials in ID order.</summary>
	public static IReadOnlyList<AssetMaterial> All { get; private set; } = [];

	public override void _EnterTree()
	{
		foreach (var material in Materials)
			if (material.MaterialId < 0 || !_materialsById.TryAdd(material.MaterialId, material))
				Log.Warning(
					"Skipped material {MaterialName} with invalid or duplicate ID {MaterialId}",
					material.MaterialName, material.MaterialId);

		All = [.. _materialsById.Values.OrderBy(static material => material.MaterialId)];
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (!ReferenceEquals(Instance, this))
			return;

		All = [];
		Instance = null;
	}

	public static AssetMaterial? Get(int materialId) => Instance?._materialsById.GetValueOrDefault(materialId);

	public static AssetMaterial? Get(GdProperties properties) =>
		properties.GetValue(PropertyKey) is { VariantType: Variant.Type.Int } materialId
			? Get(materialId.AsInt32())
			: null;
}
