namespace EnsembleRoot.Common.Networking;

/// <summary>
///     A token bucket for what's only worth having now, like streamed states: past the rate, messages are dropped
///     rather than queued. It holds half a second's worth.
/// </summary>
public sealed class TokenGate(double perSecond, TimeProvider timeProvider)
{
	private long _refillTimestamp = timeProvider.GetTimestamp();
	private double _tokens = perSecond / 2;

	public bool TryTake(double cost = 1d)
	{
		var now = timeProvider.GetTimestamp();
		var elapsed = timeProvider.GetElapsedTime(_refillTimestamp, now).TotalSeconds;

		_refillTimestamp = now;
		_tokens = Math.Min(perSecond / 2, _tokens + elapsed * perSecond);

		if (_tokens < cost)
			return false;

		_tokens -= cost;
		return true;
	}
}
