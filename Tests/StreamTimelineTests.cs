using EnsembleRoot.Common.Networking;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class StreamTimelineTests
{
	private const double Interval = 1 / 16d;
	private const double Precision = 1e-9;

	[Fact]
	public void GetTime_SteadyArrivals_MapToTheirArrivals()
	{
		var timeline = new StreamTimeline(Interval);

		for (var i = 0; i < 8; i++)
			Assert.Equal(10 + i * Interval, timeline.GetTime((ushort)(100 + i), 10 + i * Interval), Precision);
	}

	// A delayed state belongs where its sequence puts it, so the timeline only drifts toward it
	[Fact]
	public void GetTime_LateArrival_KeepsItsPlace()
	{
		var timeline = new StreamTimeline(Interval);
		timeline.GetTime(0, 10);

		var time = timeline.GetTime(1, 10 + Interval + 1 / 4d);

		Assert.True(time > 10 + Interval);
		Assert.True(time < 10 + Interval + 1 / 16d);
	}

	[Fact]
	public void GetTime_EarlyArrival_MovesTheTimelineAtOnce()
	{
		var timeline = new StreamTimeline(Interval);
		timeline.GetTime(0, 10);

		Assert.Equal(10 + Interval - 1 / 8d, timeline.GetTime(1, 10 + Interval - 1 / 8d), Precision);

		// Back on the old schedule, it's late by the early one's lead, so it only drifts a sixteenth of the way back
		Assert.Equal(10 + 2 * Interval - 1 / 8d + 1 / 8d / 16, timeline.GetTime(2, 10 + 2 * Interval), Precision);
	}

	[Fact]
	public void GetTime_SequenceWraps_KeepsCounting()
	{
		var timeline = new StreamTimeline(Interval);

		var before = timeline.GetTime(ushort.MaxValue, 10);
		var after = timeline.GetTime(0, 10 + Interval);

		Assert.Equal(before + Interval, after, Precision);
	}

	// It's late by its two intervals, so it lands behind the newer state, drifting the timeline a sixteenth of that
	[Fact]
	public void GetTime_OutOfOrder_MapsBehind()
	{
		var timeline = new StreamTimeline(Interval);
		timeline.GetTime(5, 10);

		Assert.Equal(10 - 2 * Interval + 2 * Interval / 16, timeline.GetTime(3, 10), Precision);
	}

	[Fact]
	public void Reset_StartsOverAtTheNextArrival()
	{
		var timeline = new StreamTimeline(Interval);
		timeline.GetTime(0, 10);
		timeline.Reset();

		Assert.Equal(20, timeline.GetTime(40_000, 20), Precision);
	}
}
