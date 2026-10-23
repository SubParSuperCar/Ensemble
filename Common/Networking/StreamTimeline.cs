namespace EnsembleRoot.Common.Networking;

/// <summary>
///     Maps a stream's sequence numbers, one per <c>interval</c> of the sender's clock, to local time. It follows the
///     earliest arrivals, as later ones were only delayed.
/// </summary>
public sealed class StreamTimeline(double interval)
{
	// How fast the timeline adapts to states arriving later than before (they move it earlier at once)
	private const double DriftRate = 1 / 16d;

	// The local time of sequence 0 on the sender's clock, or null until the first state
	private double? _origin;
	private long _sequence;

	/// <summary>Gets the local time a state stands for, from its sequence and its arrival's local time.</summary>
	public double GetTime(ushort sequence, double clock)
	{
		_sequence = _origin is null ? sequence : _sequence + (short)(sequence - (ushort)_sequence);

		var senderTime = _sequence * interval;
		var origin = clock - senderTime;

		_origin = _origin is { } current && origin > current ? current + (origin - current) * DriftRate : origin;
		return senderTime + _origin.Value;
	}

	/// <summary>Starts over with the next state, e.g., when another sender's clock takes over.</summary>
	public void Reset() => _origin = null;
}
