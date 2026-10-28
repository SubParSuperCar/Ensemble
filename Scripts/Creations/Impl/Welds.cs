using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Plots.Placement;
using Godot;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>Groups parts welded by abutting boxes (see <see cref="Obb.Abuts" />) into rigid groups.</summary>
public static class Welds
{
	/// <summary>
	///     Gets the groups of welded parts, as part indices in ascending order, ordered by their lowest index, so every
	///     peer derives the same groups from the same parts in the same order. Parts of one owner (an instance's
	///     pieces) never weld to each other, though others may join them into one group, and a part without boxes is a
	///     group of its own.
	/// </summary>
	public static IReadOnlyList<int[]> Group(IReadOnlyList<(int Owner, Obb[] Boxes)> parts)
	{
		var grid = new ObbGrid();
		var boxes = new List<Obb>();
		var partsByBox = new List<int>();

		for (var part = 0; part < parts.Count; part++)
			foreach (var box in parts[part].Boxes)
			{
				grid.Add(boxes.Count, box);
				boxes.Add(box);
				partsByBox.Add(part);
			}

		var parents = Enumerable.Range(0, parts.Count).ToArray();
		var candidates = new List<int>();

		for (var key = 0; key < boxes.Count; key++)
		{
			var box = boxes[key];
			var owner = parts[partsByBox[key]].Owner;
			var root = UnionFind.Find(parents, partsByBox[key]);

			// Grown, so neighbors across a gap in the next cell are found
			grid.Query(box with { Extents = box.Extents + Vector3.One * Epsilon }, candidates);

			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var other in candidates)
			{
				if (other <= key || parts[partsByBox[other]].Owner == owner)
					continue;

				var otherRoot = UnionFind.Find(parents, partsByBox[other]);

				if (root != otherRoot && box.Abuts(boxes[other]))
					parents[otherRoot] = root;
			}
		}

		// Grouping keeps the parts' order, within each group and across groups by their first part
		var groups = Enumerable.Range(0, parts.Count).GroupBy(part => UnionFind.Find(parents, part));
		return [.. groups.Select(static group => group.ToArray())];
	}
}
