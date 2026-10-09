using System.Runtime.InteropServices;
using Godot;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace EnsembleRoot.Scripts.Plots.Impl;

/// <summary>A uniform spatial hash of boxes, so placement only tests the obstacles near a box.</summary>
public sealed class ObbGrid
{
	private const float CellSize = 2f;

	private readonly Dictionary<int, Obb> _boxes = [];
	private readonly List<Obb> _candidates = [];
	private readonly Dictionary<Vector3I, List<int>> _cells = [];
	private readonly HashSet<int> _visited = [];

	public void Add(int id, Obb box)
	{
		_boxes.Add(id, box);
		var (min, max) = GetCellRange(box);

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
					(CollectionsMarshal.GetValueRefOrAddDefault(_cells, new Vector3I(x, y, z), out _) ??= []).Add(id);
	}

	public void Remove(int id)
	{
		if (!_boxes.Remove(id, out var box))
			return;

		var (min, max) = GetCellRange(box);

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
				{
					var cell = new Vector3I(x, y, z);

					if (_cells.TryGetValue(cell, out var ids) && ids.Remove(id) && ids.Count is 0)
						_cells.Remove(cell);
				}
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
		results.Clear();
		_visited.Clear();

		var (min, max) = GetCellRange(box);

		for (var x = min.X; x <= max.X; x++)
			for (var y = min.Y; y <= max.Y; y++)
				for (var z = min.Z; z <= max.Z; z++)
				{
					if (!_cells.TryGetValue(new Vector3I(x, y, z), out var ids))
						continue;

					foreach (var id in ids)
						if (_visited.Add(id))
							results.Add(_boxes[id]);
				}
	}

	private static (Vector3I Min, Vector3I Max) GetCellRange(Obb box)
	{
		var envelope = box.Envelope;
		return (ToCell(box.Center - envelope), ToCell(box.Center + envelope));
	}

	private static Vector3I ToCell(Vector3 point) => (Vector3I)(point / CellSize).Floor();
}
