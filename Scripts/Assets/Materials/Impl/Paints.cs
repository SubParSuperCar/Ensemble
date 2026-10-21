using Godot;

namespace EnsembleRoot.Scripts.Assets.Materials.Impl;

/// <summary>
///     Shares one painted material per asset material, color, and shading, so painted geometry still batches.
/// </summary>
public static class Paints
{
	private static readonly Dictionary<(AssetMaterial? Material, Color Color, BaseMaterial3D.ShadingModeEnum Shading),
		BaseMaterial3D> PaintsByKey = [];

	/// <summary>
	///     Paints with <paramref name="material" />'s look, or else a plain material in <paramref name="shading" /> (a
	///     look brings its own).
	/// </summary>
	public static BaseMaterial3D Get(AssetMaterial? material, Color color, BaseMaterial3D.ShadingModeEnum shading)
	{
		var key = (material, color, material?.Look?.ShadingMode ?? shading);

		if (!PaintsByKey.TryGetValue(key, out var paint))
			PaintsByKey.Add(key, paint = Create(material, color, shading));

		return paint;
	}

	private static BaseMaterial3D Create(AssetMaterial? material, Color color, BaseMaterial3D.ShadingModeEnum shading)
	{
		if (material?.Look is not { } look)
			return new StandardMaterial3D { AlbedoColor = color, ShadingMode = shading };

		var paint = (BaseMaterial3D)look.Duplicate();
		paint.AlbedoColor *= color;

		return paint;
	}
}
