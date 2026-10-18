using System.Diagnostics;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Sessions;

/// <summary>
///     Simulates a poor connection on this peer, for testing without outside players: incoming live state (creations
///     and characters) is delayed by <see cref="LatencyMs" /> plus up to <see cref="JitterMs" />, kept in order like
///     ENet's ordered channels, and a <see cref="LossRate" /> share of it is dropped. Reliable traffic is untouched,
///     since dropping it above ENet would break its guarantees.
/// </summary>
public static class NetworkConditions
{
	private static long _lastDueTimestamp;

	public static int LatencyMs { get; private set; }
	public static int JitterMs { get; private set; }
	public static double LossRate { get; private set; }

	public static bool IsSimulated => LatencyMs > 0 || JitterMs > 0 || LossRate > 0;

	public static void Set(int latencyMs, int jitterMs, double lossRate)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(latencyMs);
		ArgumentOutOfRangeException.ThrowIfNegative(jitterMs);
		ArgumentOutOfRangeException.ThrowIfNegative(lossRate);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(lossRate, 1);

		LatencyMs = latencyMs;
		JitterMs = jitterMs;
		LossRate = lossRate;
	}

	/// <summary>Runs <paramref name="deliver" /> now, later, or never, unless <paramref name="node" /> is freed first.</summary>
	public static void Receive(Node node, Action deliver)
	{
		if (!IsSimulated)
		{
			deliver();
			return;
		}

		if (Random.Shared.NextDouble() < LossRate)
			return;

		var now = Stopwatch.GetTimestamp();
		var delay = TimeSpan.FromMilliseconds(LatencyMs + Random.Shared.Next(JitterMs + 1));
		var due = Math.Max(now + (long)(delay.TotalSeconds * Stopwatch.Frequency), _lastDueTimestamp);

		_lastDueTimestamp = due;

		node.GetTree().CreateTimer(Stopwatch.GetElapsedTime(now, due).TotalSeconds, true, true).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(node))
				deliver();
		};
	}
}
