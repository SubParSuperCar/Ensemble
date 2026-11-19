using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.Assets.Materials;

/// <summary>
///     Shares one painted material per asset material, color, and shading, so painted geometry still batches.
/// </summary>
public static class Paints
{
	// Past this many paints, those no geometry uses are swept out, so painting endless colors can't fill memory
	private const int MinSweepCount = 1024;

	private static readonly Dictionary<(AssetMaterial? Material, Color Color, BaseMaterial3D.ShadingModeEnum Shading),
		BaseMaterial3D> PaintsByKey = [];

	private static int _sweepCount = MinSweepCount;

	/// <summary>
	///     Paints with <paramref name="material" />'s look, or else a plain material in <paramref name="shading" /> (a
	///     look brings its own), transparent only if <paramref name="color" /> is.
	/// </summary>
	public static BaseMaterial3D Get(AssetMaterial? material, Color color, BaseMaterial3D.ShadingModeEnum shading)
	{
		var key = (material, color, material?.Look?.ShadingMode ?? shading);

		if (PaintsByKey.TryGetValue(key, out var paint))
			return paint;

		if (PaintsByKey.Count >= _sweepCount)
			Sweep();

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

	// A paint only this cache references is on no geometry. The next sweep waits until the cache has doubled, so
	// sweeping stays cheap however many paints are in use.
	private static void Sweep()
	{
		var count = PaintsByKey.Count;

		foreach (var (key, paint) in PaintsByKey)
			if (paint.GetReferenceCount() <= 1)
			{
				PaintsByKey.Remove(key);
				paint.Dispose();
			}

		_sweepCount = Math.Max(MinSweepCount, 2 * PaintsByKey.Count);
		Log.Verbose("Swept {Count} unused paint(s), keeping {Kept}", count - PaintsByKey.Count, PaintsByKey.Count);
	}
}
