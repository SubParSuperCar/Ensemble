using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Serilog;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	// A drained bucket refills in 5 s, so token costs read as percentages of a peer's 5-second budget. Requests beyond
	// it wait in a queue of up to another bucket's worth, and only then get dropped.
	private static readonly TokenBucketRateLimiterOptions RateLimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 100,
		TokensPerPeriod = 2,
		ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
		AutoReplenishment = true
	};

	private readonly ConcurrentQueue<(int Generation, Action Action)> _pendingRpcs = [];
	private readonly ConcurrentDictionary<int, TokenBucketRateLimiter> _rateLimitersByPeerId = [];

	// Bumped per session, so leases granted on the thread pool after a session ends can't run in the next one.
	// Main thread only: requests read it before their first await.
	private int _rpcGeneration;

	public override void _Process(double delta)
	{
		while (_pendingRpcs.TryDequeue(out var pending))
			if (pending.Generation == _rpcGeneration)
				RunSafely(pending.Action);

		UpdatePings(delta);
	}

	private static void RunSafely(Action action)
	{
		try
		{
			action();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Unhandled exception in RPC action");
		}
	}

	private void DisposeRateLimiter(int peerId)
	{
		if (_rateLimitersByPeerId.TryRemove(peerId, out var limiter))
			limiter.Dispose();
	}

	private void ClearRpcState()
	{
		foreach (var peerId in _rateLimitersByPeerId.Keys)
			DisposeRateLimiter(peerId);

		_pendingRpcs.Clear();
		_rpcGeneration++;
	}

	/// <summary>
	///     Runs <paramref name="action" /> on the main thread once the sender's rate limiter allows it, or
	///     <paramref name="onDropped" /> instead if it drops the request.
	/// </summary>
	private void EnqueueRpc(int senderId, int tokens, Action action, Action? onDropped = null) =>
		_ = EnqueueRpcAsync(senderId, tokens, action, onDropped);

	private async Task EnqueueRpcAsync(int senderId, int tokens, Action action, Action? onDropped)
	{
		var generation = _rpcGeneration;

		if (tokens > RateLimiterOptions.TokenLimit)
		{
			Drop();
			return;
		}

		var limiter = _rateLimitersByPeerId.GetOrAdd(
			senderId, static _ => new TokenBucketRateLimiter(RateLimiterOptions));

		try
		{
			using var lease = await limiter.AcquireAsync(tokens).ConfigureAwait(false);

			if (!lease.IsAcquired)
			{
				Log.Debug("Peer {PeerId} hit the RPC rate limit", senderId);
				Drop();

				return;
			}
		}
		catch (ObjectDisposedException)
		{
			return;
		}

		_pendingRpcs.Enqueue((generation, action));
		return;

		void Drop()
		{
			if (onDropped is not null)
				_pendingRpcs.Enqueue((generation, onDropped));
		}
	}
}
