using Godot;

namespace EnsembleRoot.Scripts.Plots.Impl;

/// <summary>Groups parts welded by abutting boxes (see <see cref="Obb.Abuts" />) into rigid groups.</summary>
public static class Welds
{
	/// <summary>
	///     Gets the groups of welded parts, each in ascending ID order and ordered by their lowest ID, so every peer
	///     derives the same groups from the same parts. A part's own boxes never weld to each other, and a part without
	///     boxes is a group of its own.
	/// </summary>
	public static IReadOnlyList<int[]> Group(IReadOnlyList<(int Id, Obb[] Boxes)> parts)
	{
		var grid = new ObbGrid();
		var boxes = new List<Obb>();
		var owners = new List<int>();

		for (var part = 0; part < parts.Count; part++)
			foreach (var box in parts[part].Boxes)
			{
				grid.Add(boxes.Count, box);
				boxes.Add(box);
				owners.Add(part);
			}

		var parents = Enumerable.Range(0, parts.Count).ToArray();
		var candidates = new List<int>();

		for (var key = 0; key < boxes.Count; key++)
		{
			var box = boxes[key];

			// Grown, so neighbors across a gap in the next cell are found
			grid.Query(box with { Extents = box.Extents + Vector3.One * Epsilon }, candidates);

			foreach (var other in candidates)
			{
				if (other <= key)
					continue;

				var root = Find(parents, owners[key]);
				var otherRoot = Find(parents, owners[other]);

				if (root != otherRoot && box.Abuts(boxes[other]))
					parents[otherRoot] = root;
			}
		}

		return parts
			.Select((part, index) => (part.Id, Root: Find(parents, index)))
			.GroupBy(static entry => entry.Root, static entry => entry.Id)
			.Select(static group => group.Order().ToArray())
			.OrderBy(static group => group[0])
			.ToList();
	}

	private static int Find(int[] parents, int index)
	{
		while (parents[index] != index)
			index = parents[index] = parents[parents[index]];

		return index;
	}
}
