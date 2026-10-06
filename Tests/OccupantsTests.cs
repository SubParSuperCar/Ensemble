using EnsembleCoreRoot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class OccupantsTests
{
	[Fact]
	public void SetPlot_RelinquishingOwner_PassesOwnershipToOldestOccupant()
	{
		var (core, ids) = CreateCore(4);

		foreach (var id in ids)
			core.Plots.SetPlot(id, 0, true);

		core.Plots.SetPlot(ids[1]);
		core.Plots.SetPlot(ids[1], 0, true);
		core.Plots.SetPlot(ids[0], null, true);

		Assert.Equal(ids[2], core.Plots.All[0].Occupants.Owner?.Player.Id);
	}

	[Fact]
	public void SetPlot_RestoredOccupancy_ResolvesSameOwnerAsOrigin()
	{
		var (origin, ids) = CreateCore(4);
		var (restored, _) = CreateCore(4, ids);

		foreach (var id in ids[..3])
			origin.Plots.SetPlot(id, 0, true);

		origin.Plots.SetPlot(ids[1]);

		foreach (var occupant in origin.Plots.All[0].Occupants.All.Values)
			restored.Plots.SetPlot(occupant.Player.Id, 0);

		restored.Plots.All[0].Occupants.SetOwner(origin.Plots.All[0].Occupants.Owner?.Player.Id);

		foreach (var core in (Core[])[origin, restored])
		{
			core.Plots.SetPlot(ids[3], 0, true);
			core.Plots.SetPlot(ids[0], null, true);
		}

		Assert.Equal(
			origin.Plots.All[0].Occupants.Owner?.Player.Id,
			restored.Plots.All[0].Occupants.Owner?.Player.Id);
	}

	[Fact]
	public void Reset_RemovesOccupantsBeforePlots()
	{
		var (core, ids) = CreateCore(2);
		var plot = core.Plots.All[0];

		foreach (var id in ids)
			core.Plots.SetPlot(id, 0, true);

		var removedWhileRegistered = 0;
		plot.Occupants.Removed += _ => removedWhileRegistered += core.Plots.All.ContainsKey(0) ? 1 : 0;

		core.Reset();

		Assert.Equal(ids.Length, removedWhileRegistered);
		Assert.Empty(plot.Occupants.All);
		Assert.Null(plot.Occupants.Owner);
	}

	private static (Core Core, Guid[] Ids) CreateCore(int playerCount, Guid[]? ids = null)
	{
		var core = new Core();
		core.Plots.Add(0);

		ids ??= [.. Enumerable.Range(0, playerCount).Select(static _ => Guid.NewGuid())];

		foreach (var id in ids)
			core.Players.Add(id);

		return (core, ids);
	}
}
