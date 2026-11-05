using EnsembleRoot.Common.Networking;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class TokenGateTests
{
	[Fact]
	public void TryTake_PastTheBurst_Drops()
	{
		var gate = new TokenGate(4, new ManualTime());

		Assert.True(gate.TryTake(2));
		Assert.False(gate.TryTake());
	}

	[Fact]
	public void TryTake_AfterTime_RefillsAtTheRateUpToTheBurst()
	{
		var time = new ManualTime();
		var gate = new TokenGate(4, time);

		Assert.True(gate.TryTake(2));

		time.Seconds = 1 / 4d;
		Assert.True(gate.TryTake());
		Assert.False(gate.TryTake());

		time.Seconds = 100d;
		Assert.True(gate.TryTake(2));
		Assert.False(gate.TryTake());
	}

	private sealed class ManualTime : TimeProvider
	{
		public double Seconds { get; set; }

		public override long TimestampFrequency => 1_000_000;
		public override long GetTimestamp() => (long)(Seconds * TimestampFrequency);
	}
}
