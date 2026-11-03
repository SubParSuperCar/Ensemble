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
}
