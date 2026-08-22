using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Serilog;

namespace Root.SessionManager;

// RPC infra. Rate limited. Should be mostly fine for now for being resilient against exploiters
// (even though the host must willingly provide PW/connect into to friends, but still).
public partial class SessionManager
{
	private static readonly ConcurrentDictionary<int, TokenBucketRateLimiter> RateLimitersByPeerId = [];

	private static readonly TokenBucketRateLimiterOptions RateLimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 10,
		TokensPerPeriod = 1,
		ReplenishmentPeriod = TimeSpan.FromSeconds(0.1),
		AutoReplenishment = true
	};

	private readonly ConcurrentQueue<Action> _pendingRpcs = [];

	public override void _Process(double delta)
	{
		while (_pendingRpcs.TryDequeue(out var action))
			RunSafely(action);
	}

	private static void RunSafely(Action action)
	{
		try
		{
			action();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Unhandled exception in RPC action.");
		}
	}

	// Tokens are essentially the price of the operation: bigger/more expensive ops should use more tokens.
	// The host should probably never be rate limited, as seen below.
	private void EnqueueRpc(int senderId, int tokens, Action action) => _ = EnqueueRpcAsync(senderId, tokens, action);

	private async Task EnqueueRpcAsync(int senderId, int tokens, Action action)
	{
		var limiter = senderId is 1
			? null
			: RateLimitersByPeerId.GetOrAdd(senderId, _ => new TokenBucketRateLimiter(RateLimiterOptions));

		using var lease = limiter is null
			? null
			: await limiter.AcquireAsync(tokens).ConfigureAwait(false);

		// We might want to log when a peer hits the rate limit so the user is notified of potential spam.
		if (lease is not { IsAcquired: false })
			_pendingRpcs.Enqueue(action);
	}

	private static void OnPeerDisconnectedDisposeRateLimiter(long peerId)
	{
		// Get rid of the rate limiter object when a peer disconnects.
		if (RateLimitersByPeerId.TryRemove((int)peerId, out var limiter))
			limiter.Dispose();
	}
}
