using EnsembleRoot.Scripts.Plots;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class RelayBudgetTests
{
	[Fact]
	public void Take_WithinBudget_RelaysEveryStream()
	{
		var remaining = 100d;

		Assert.Equal(1, RelayBudget.Take(ref remaining, 60));
		Assert.Equal(40d, remaining);
	}

	[Fact]
	public void Take_NearestFirst_ThinsOutFarOnes()
	{
		var remaining = 100d;

		Assert.Equal(1, RelayBudget.Take(ref remaining, 60));
		Assert.Equal(2, RelayBudget.Take(ref remaining, 60));
		Assert.Equal(4, RelayBudget.Take(ref remaining, 60));
	}

	// A far creation still moves, however little is left
	[Fact]
	public void Take_NothingLeft_StillRelaysAtTheMaximumDivisor()
	{
		var remaining = 0d;

		Assert.Equal(RelayBudget.MaxDivisor, RelayBudget.Take(ref remaining, 60));
		Assert.Equal(0d, remaining);
	}

	// A small creation's keepalives can fall on one phase of its streams, so every phase must get its turn
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void IsRelayed_EveryPhase_GetsItsTurn(int phase)
	{
		var isRelayed = Enumerable.Range(0, 1 << 12).Any(sequence =>
			sequence % RelayBudget.MaxDivisor == phase && RelayBudget.IsRelayed((ushort)sequence, RelayBudget.MaxDivisor));

		Assert.True(isRelayed);
	}

	[Fact]
	public void IsRelayed_FullRate_RelaysEverything() =>
		Assert.All(Enumerable.Range(0, 1 << 10),
			static sequence => Assert.True(RelayBudget.IsRelayed((ushort)sequence, 1)));
}
