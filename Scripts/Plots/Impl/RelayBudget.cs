namespace EnsembleRoot.Scripts.Plots;

/// <summary>
///     Shares what the host may send a peer among the creations streaming to it, nearest first: each takes its full
///     rate if that fits, or else every second or fourth of its streams, so far creations thin out before near ones do.
/// </summary>
public static class RelayBudget
{
	public const int MaxDivisor = 4;

	// As a power of two, the streams before a thinned creation's relays shift by one
	private const int PhaseShiftBits = 6;

	/// <summary>
	///     Whether a thinned stream is relayed. The phase drifts, as a fixed one could forever miss the streams where a
	///     small creation's keepalives happen to fall, leaving a proxy stale.
	/// </summary>
	public static bool IsRelayed(ushort sequence, int divisor) =>
		(sequence + (sequence >> PhaseShiftBits)) % divisor is 0;

	public static int Take(ref double remaining, double bytesPerSecond)
	{
		var divisor = 1;

		while (divisor < MaxDivisor && bytesPerSecond / divisor > remaining)
			divisor *= 2;

		remaining = Math.Max(remaining - bytesPerSecond / divisor, 0);
		return divisor;
	}
}
