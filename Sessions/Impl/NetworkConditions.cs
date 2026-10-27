using System.Diagnostics;
using Godot;

namespace EnsembleRoot.Sessions;

/// <summary>
///     Simulates a poor connection on this peer, for testing without outside players: incoming live state (creations
///     and characters) is delayed by a latency plus up to a jitter, kept in order like ENet's ordered channels, and a
///     loss rate's share of it is dropped. Reliable traffic is untouched, since dropping it above ENet would break its
///     guarantees.
/// </summary>
public static class NetworkConditions
{
	private static int _latencyMs;
	private static int _jitterMs;
	private static double _lossRate;
	private static long _lastDueTimestamp;

	private static bool IsSimulated => _latencyMs > 0 || _jitterMs > 0 || _lossRate > 0;

	public static void Set(int latencyMs, int jitterMs, double lossRate)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(latencyMs);
		ArgumentOutOfRangeException.ThrowIfNegative(jitterMs);
		ArgumentOutOfRangeException.ThrowIfNegative(lossRate);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(lossRate, 1d);

		_latencyMs = latencyMs;
		_jitterMs = jitterMs;
		_lossRate = lossRate;
	}

	/// <summary>
	///     Runs <paramref name="deliver" /> now, later, or never, unless <paramref name="node" /> is freed first.
	/// </summary>
	public static void Receive(Node node, Action deliver)
	{
		if (!IsSimulated)
		{
			deliver();
			return;
		}

		if (Random.Shared.NextDouble() < _lossRate)
			return;

		var now = Stopwatch.GetTimestamp();
		var delayMs = _latencyMs + Random.Shared.Next(_jitterMs + 1);
		var due = Math.Max(now + delayMs * Stopwatch.Frequency / 1000, _lastDueTimestamp);

		_lastDueTimestamp = due;

		node.GetTree().CreateTimer(Stopwatch.GetElapsedTime(now, due).TotalSeconds, true, true).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(node))
				deliver();
		};
	}
}
