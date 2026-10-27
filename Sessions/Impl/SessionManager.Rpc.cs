using System.Threading.RateLimiting;
using EnsembleRoot.Sessions.Actions;
using Serilog;
using PendingRpc = (System.Threading.Tasks.Task<bool> Lease, System.Action Action, System.Action? OnDropped);

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	// A drained bucket refills in 5 s, so token costs read as percentages of a peer's 5-second budget. Requests beyond
	// it wait in a queue of up to another bucket's worth, and only then get dropped.
	private static readonly TokenBucketRateLimiterOptions RateLimiterOptions = new()
	{
		TokenLimit = NetworkActionRegistry.MaxTokenCost,
		QueueLimit = 100,
		TokensPerPeriod = 2,
		ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
		AutoReplenishment = true
	};

	// Per sender, so a lease granted out of order can't run a request before an earlier one. Main thread only.
	private readonly Dictionary<int, Queue<PendingRpc>> _pendingRpcsByPeerId = [];
	private readonly Dictionary<int, TokenBucketRateLimiter> _rateLimitersByPeerId = [];

	/// <summary>How long a drained rate limiter takes to regain <paramref name="tokens" />, for pacing.</summary>
	public static TimeSpan GetRefillTime(int tokens) =>
		RateLimiterOptions.ReplenishmentPeriod * Math.Ceiling(tokens / (double)RateLimiterOptions.TokensPerPeriod);

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

	private static async Task<bool> AcquireAsync(int senderId, TokenBucketRateLimiter limiter, int tokens)
	{
		using var lease = await limiter.AcquireAsync(tokens).ConfigureAwait(false);

		if (!lease.IsAcquired)
			Log.Debug("Peer {PeerId} hit the RPC rate limit", senderId);

		return lease.IsAcquired;
	}

	private void RunPendingRpcs()
	{
		// An action may end the session, which clears the queues
		foreach (var peerId in _pendingRpcsByPeerId.Keys.ToArray())
			while (
				_pendingRpcsByPeerId.TryGetValue(peerId, out var queue) &&
				queue.TryPeek(out var pending) && pending.Lease.IsCompleted)
			{
				queue.Dequeue();

				if (queue.Count is 0)
					_pendingRpcsByPeerId.Remove(peerId);

				if (pending.Lease.Result)
					RunSafely(pending.Action);
				else if (pending.OnDropped is not null)
					RunSafely(pending.OnDropped);
			}
	}

	private void DisposeRateLimiter(int peerId)
	{
		if (_rateLimitersByPeerId.Remove(peerId, out var limiter))
			limiter.Dispose();
	}

	private void ClearRpcState()
	{
		foreach (var limiter in _rateLimitersByPeerId.Values)
			limiter.Dispose();

		_rateLimitersByPeerId.Clear();
		_pendingRpcsByPeerId.Clear();
	}

	/// <summary>
	///     Runs <paramref name="action" /> on the main thread once the sender's rate limiter allows it, or
	///     <paramref name="onDropped" /> instead if it drops the request. Requests from one sender run in order.
	/// </summary>
	private void EnqueueRpc(int senderId, int tokens, Action action, Action? onDropped = null)
	{
		if (_kickedPeerIds.Contains(senderId))
			return;

		if (!_rateLimitersByPeerId.TryGetValue(senderId, out var limiter))
			_rateLimitersByPeerId[senderId] = limiter = new TokenBucketRateLimiter(RateLimiterOptions);

		if (!_pendingRpcsByPeerId.TryGetValue(senderId, out var queue))
			_pendingRpcsByPeerId[senderId] = queue = [];

		queue.Enqueue((AcquireAsync(senderId, limiter, tokens), action, onDropped));
	}
}
