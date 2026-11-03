using Godot;

namespace EnsembleRoot.Scripts.Assets.Materials;

/// <summary>
///     Shares one painted material per asset material, color, and shading, so painted geometry still batches.
/// </summary>
public static class Paints
{
	private static readonly Dictionary<(AssetMaterial? Material, Color Color, BaseMaterial3D.ShadingModeEnum Shading),
		BaseMaterial3D> PaintsByKey = [];

	/// <summary>
	///     Paints with <paramref name="material" />'s look, or else a plain material in <paramref name="shading" /> (a
	///     look brings its own), transparent only if <paramref name="color" /> is.
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
		var paint = material?.Look is { } look
			? (BaseMaterial3D)look.Duplicate()
			: new StandardMaterial3D { ShadingMode = shading };

		paint.AlbedoColor *= color;

		if (color.A < 1)
			paint.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

		return paint;
	}
}
