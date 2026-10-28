namespace EnsembleRoot.Common.Utils;

/// <summary>Disjoint sets as an array of parent indices, where each set's root is its own parent.</summary>
public static class UnionFind
{
	public static int Find(int[] parents, int index)
	{
		while (parents[index] != index)
			index = parents[index] = parents[parents[index]];

		return index;
	}
}
