using Godot;

namespace EnsembleRoot.Scripts.Assets.Impl;

/// <summary>Shares one painted material per asset material and color, so painted geometry still batches.</summary>
public static class Paints
{
	private static readonly Dictionary<(AssetMaterial? Material, Color Color), BaseMaterial3D> PaintsByKey = [];

	public static BaseMaterial3D Get(AssetMaterial? material, Color color)
	{
		var key = (material, color);

		if (!PaintsByKey.TryGetValue(key, out var paint))
			PaintsByKey.Add(key, paint = Create(material, color));

		return paint;
	}

	private static BaseMaterial3D Create(AssetMaterial? material, Color color)
	{
		if (material?.Look is not { } look)
			return new StandardMaterial3D { AlbedoColor = color };

		var paint = (BaseMaterial3D)look.Duplicate();
		paint.AlbedoColor *= color;

		return paint;
	}
}
