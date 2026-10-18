using Godot;

namespace EnsembleRoot.Scripts.Assets.Impl;

/// <summary>Shares one material per material ID and color, so painted geometry batches like unpainted geometry.</summary>
public static class AssetMaterials
{
	private static readonly Dictionary<(int MaterialId, Color Color), StandardMaterial3D> MaterialsByKey = [];

	// TODO: A material registry; every material ID is plain for now
	public static StandardMaterial3D Get(int materialId, Color color)
	{
		if (!MaterialsByKey.TryGetValue((materialId, color), out var material))
			MaterialsByKey.Add((materialId, color), material = new StandardMaterial3D { AlbedoColor = color });

		return material;
	}
}
