using EnsembleRoot.Scripts.Creations.Impl;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ArbitrationTests
{
	[Fact]
	public void GetLeaders_GroupsChains_UnderTheirLargestPlot()
	{
		// 0 - 1 - 2 in a chain led by 1, the largest; 3 - 4 led by 4; 5 alone
		(int, int)[] plots = [(0, 3), (1, 9), (2, 1), (3, 2), (4, 5), (5, 99)];
		(int, int)[] links = [(0, 1), (1, 2), (3, 4)];

		var expected = new Dictionary<int, int> { [0] = 1, [1] = 1, [2] = 1, [3] = 4, [4] = 4, [5] = 5 };

		Assert.Equal(expected, Arbitration.GetLeaders(plots, links));
		Assert.Equal(expected, Arbitration.GetLeaders([.. plots.Reverse()], [.. links.Reverse()]));
	}

	[Fact]
	public void GetLeaders_BreaksTiesByLowestId_AndIgnoresUnlistedPlots()
	{
		// 7 isn't spawned, so it neither leads nor bridges 2 to 4
		(int, int)[] plots = [(4, 6), (2, 6), (9, 6)];
		(int, int)[] links = [(9, 4), (2, 7), (7, 4)];

		var expected = new Dictionary<int, int> { [2] = 2, [4] = 4, [9] = 4 };

		Assert.Equal(expected, Arbitration.GetLeaders(plots, links));
	}

	[Fact]
	public void GetLeaders_MatchesBruteForce_OnRandomLinks()
	{
		var random = new Random(13);

		for (var round = 0; round < 200; round++)
		{
			var plots = Enumerable.Range(0, random.Next(1, 12))
				.Select(id => (Id: id * 3, AssemblyCount: random.Next(1, 5)))
				.ToArray();

			var links = Enumerable.Range(0, random.Next(0, 16))
				.Select(_ => (plots[random.Next(plots.Length)].Id, plots[random.Next(plots.Length)].Id))
				.ToArray();

			Assert.Equal(Brute(plots, links), Arbitration.GetLeaders(plots, links));
		}
	}

	[Fact]
	public void Spheres_OverlapOnlyWhenAPairTouches()
	{
		var spheres = Spheres((Vector3.Zero, 1), (new Vector3(10, 0, 0), 1));

		Assert.True(spheres.Overlaps(Spheres((new Vector3(11.5f, 0, 0), 1))));
		Assert.False(spheres.Overlaps(Spheres((new Vector3(5, 0, 0), 1))));
		Assert.False(spheres.Overlaps(Spheres((new Vector3(12.5f, 0, 0), 1))));
		Assert.False(spheres.Overlaps(Spheres()));
	}

	[Fact]
	public void Spheres_MatchBruteForce_OnRandomSets()
	{
		var random = new Random(17);

		for (var round = 0; round < 500; round++)
		{
			var sets = new[] { RandomSpheres(random), RandomSpheres(random) };
			var expected = sets[0].Any(a => sets[1].Any(b => a.Center.DistanceTo(b.Center) < a.Radius + b.Radius));

			var spheres = Spheres(sets[0]);
			var otherSpheres = Spheres(sets[1]);

			Assert.Equal(expected, spheres.Overlaps(otherSpheres));
			Assert.Equal(expected, otherSpheres.Overlaps(spheres));
		}
	}

	private static (Vector3 Center, float Radius)[] RandomSpheres(Random random) =>
	[
		.. Enumerable.Range(0, random.Next(0, 24)).Select(_ => (
			new Vector3(random.NextSingle() * 40, random.NextSingle() * 8, random.NextSingle() * 8),
			random.NextSingle() * 2))
	];

	private static ArbitrationSpheres Spheres(params (Vector3 Center, float Radius)[] spheres)
	{
		var result = new ArbitrationSpheres();

		foreach (var (center, radius) in spheres)
			result.Add(center, radius);

		return result;
	}

	// Floods each plot's group, then picks its best plot directly
	private static Dictionary<int, int> Brute((int Id, int AssemblyCount)[] plots, (int, int)[] links)
	{
		var leaders = new Dictionary<int, int>();

		foreach (var plot in plots)
		{
			var group = new HashSet<int> { plot.Id };
			var isGrowing = true;

			while (isGrowing)
			{
				isGrowing = false;

				foreach (var (id, otherId) in links)
					if (group.Contains(id) != group.Contains(otherId))
						isGrowing = group.Add(id) | group.Add(otherId);
			}

			leaders[plot.Id] = plots
				.Where(other => group.Contains(other.Id))
				.OrderByDescending(static other => other.AssemblyCount)
				.ThenBy(static other => other.Id)
				.First()
				.Id;
		}

		return leaders;
	}
}
