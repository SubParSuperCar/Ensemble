using System.Runtime.InteropServices;
using Godot;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace EnsembleRoot.Scripts.Plots.Placement;

/// <summary>A uniform spatial hash of boxes, so placement only tests the obstacles near a box.</summary>
public sealed class ObbGrid
{
	private const float CellSize = 2f;
	private const float MaxGriddedExtent = 16f;
	private const float MaxGriddedReach = 64 * 1024f;

	private readonly Dictionary<int, Obb> _boxes = [];
	private readonly List<int> _candidateIds = [];
	private readonly List<Obb> _candidates = [];
	private readonly Dictionary<Vector3I, List<int>> _cells = [];
	private readonly HashSet<int> _ungridded = [];
	private readonly HashSet<int> _visited = [];

	public void Add(int id, Obb box)
	{
		_boxes.Add(id, box);

		if (!TryGetCellRange(box, out var min, out var max))
		{
			_ungridded.Add(id);
			return;
		}

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
					(CollectionsMarshal.GetValueRefOrAddDefault(_cells, new Vector3I(x, y, z), out _) ??= []).Add(id);
	}

	public void Remove(int id)
	{
		if (!_boxes.Remove(id, out var box) || _ungridded.Remove(id) || !TryGetCellRange(box, out var min, out var max))
			return;

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
				{
					var cell = new Vector3I(x, y, z);

					if (_cells.TryGetValue(cell, out var ids) && ids.Remove(id) && ids.Count is 0)
						_cells.Remove(cell);
				}
	}

	public void Clear()
	{
		_boxes.Clear();
		_cells.Clear();
		_ungridded.Clear();
	}

	public bool Overlaps(Obb box)
	{
		Query(box, _candidates);

		foreach (var candidate in _candidates)
			if (box.Overlaps(candidate))
				return true;

		return false;
	}

	public void Query(Obb box, ICollection<Obb> results)
	{
		Query(box, _candidateIds);
		results.Clear();

		foreach (var id in _candidateIds)
			results.Add(_boxes[id]);
	}

	public void Query(Obb box, ICollection<int> results)
	{
		results.Clear();
		_visited.Clear();

		if (!TryGetCellRange(box, out var min, out var max))
		{
			foreach (var id in _boxes.Keys)
				results.Add(id);

			return;
		}

		foreach (var id in _ungridded)
			results.Add(id);

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
				{
					if (!_cells.TryGetValue(new Vector3I(x, y, z), out var ids))
						continue;

					foreach (var id in ids)
						if (_visited.Add(id))
							results.Add(id);
				}
	}

	// Huge, far, or non-finite boxes skip the cells, so the loops stay bounded
	private static bool TryGetCellRange(Obb box, out Vector3I min, out Vector3I max)
	{
		var envelope = box.Envelope;
		var isGridded = IsWithin(envelope, MaxGriddedExtent) && IsWithin(box.Center.Abs() + envelope, MaxGriddedReach);

		min = isGridded ? ToCell(box.Center - envelope) : default;
		max = isGridded ? ToCell(box.Center + envelope) : default;

		return isGridded;
	}

	private static bool IsWithin(Vector3 vector, float limit) =>
		vector.X <= limit && vector.Y <= limit && vector.Z <= limit;

	private static Vector3I ToCell(Vector3 point) => (Vector3I)(point / CellSize).Floor();
}
