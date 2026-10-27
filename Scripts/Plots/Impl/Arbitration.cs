namespace EnsembleRoot.Scripts.Plots;

/// <summary>Groups plots whose creations are close, so each group's creations can share one simulator.</summary>
public static class Arbitration
{
	/// <summary>
	///     Gets each plot's leader: the plot with the most assemblies among those linked to it, directly or through
	///     others, or the lowest ID among equals. A plot without links leads itself, and links to unlisted plots are
	///     ignored.
	/// </summary>
	public static IReadOnlyDictionary<int, int> GetLeaders(
		IReadOnlyList<(int Id, int AssemblyCount)> plots, IEnumerable<(int, int)> links)
	{
		var indicesById = plots.Select(static (plot, index) => (plot.Id, index)).ToDictionary();
		var parents = Enumerable.Range(0, plots.Count).ToArray();

		foreach (var (id, otherId) in links)
		{
			if (!indicesById.TryGetValue(id, out var index) || !indicesById.TryGetValue(otherId, out var otherIndex))
				continue;

			var root = Find(parents, index);
			var otherRoot = Find(parents, otherIndex);

			// Each group's root is its leader, so the better root stays one
			if (root == otherRoot)
				continue;

			if (Leads(plots[otherRoot], plots[root]))
				parents[root] = otherRoot;
			else
				parents[otherRoot] = root;
		}

		return plots.Select((plot, index) => (plot.Id, plots[Find(parents, index)].Id)).ToDictionary();
	}

	private static bool Leads((int Id, int AssemblyCount) plot, (int Id, int AssemblyCount) other) =>
		plot.AssemblyCount != other.AssemblyCount ? plot.AssemblyCount > other.AssemblyCount : plot.Id < other.Id;

	private static int Find(int[] parents, int index)
	{
		while (parents[index] != index)
			index = parents[index] = parents[parents[index]];

		return index;
	}
}
