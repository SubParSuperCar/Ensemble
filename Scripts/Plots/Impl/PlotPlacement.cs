using Godot;

namespace EnsembleRoot.Scripts.Plots.Impl;

public enum PlacementState : byte
{
	Valid,
	OutOfBounds,
	Overlapping,
	AssetQuotaMet,
	PlotQuotaMet
}

/// <summary>Placement rules shared by previews and authoritative validation, in a plot's local space.</summary>
public static class PlotPlacement
{
	public static PlacementState Evaluate(PlotHandle plot, int assetId, Vector3 gridPosition, Quaternion rotation)
	{
		var box = GetBox(assetId, gridPosition, rotation);

		if (!IsWithin(box, GetBounds(plot)))
			return PlacementState.OutOfBounds;

		if (GetObstacles(plot).Overlaps(box))
			return PlacementState.Overlapping;

		var instances = GPlots.GetPlot(plot.Id)!.Instances.Source;
		var (count, maxCount) = instances.GetQuota(assetId);

		if (IsLimitReached(count, maxCount))
			return PlacementState.AssetQuotaMet;

		return IsLimitReached(instances.Count, instances.MaxCount)
			? PlacementState.PlotQuotaMet
			: PlacementState.Valid;
	}

	public static Obb GetBox(int assetId, Vector3 gridPosition, Quaternion rotation) =>
		Obb.From(
			GAssetManager.GetBoundary(assetId),
			new Transform3D(new Basis(rotation), gridPosition * PlotHandle.GridToWorldScale));

	public static ObbGrid GetObstacles(PlotHandle plot) => plot.InstanceBoxes;

	public static Aabb GetBounds(PlotHandle plot)
	{
		var min = plot.WorldToGrid(plot.BoundaryTransform.Origin) - plot.GridBoundarySize / 2;
		min.Y = MathF.Max(min.Y, 0);

		return new Aabb(min * PlotHandle.GridToWorldScale, plot.GridBoundarySize * PlotHandle.GridToWorldScale);
	}

	private static bool IsWithin(Obb box, Aabb bounds)
	{
		var min = box.Center - box.Envelope;
		var max = box.Center + box.Envelope;

		for (var axis = 0; axis < 3; axis++)
			if (!IsAtLeast(min[axis], bounds.Position[axis]) || !IsAtLeast(bounds.End[axis], max[axis]))
				return false;

		return true;
	}

	private static bool IsAtLeast(float value, float threshold) =>
		value >= threshold || Mathf.IsEqualApprox(value, threshold);
}
