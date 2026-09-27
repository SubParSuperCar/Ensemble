using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Serilog;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	private const int ServerPeerId = 1;

	private static readonly TokenBucketRateLimiterOptions RateLimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 10,
		TokensPerPeriod = 1,
		ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
		AutoReplenishment = true
	};

	private readonly ConcurrentQueue<Action> _pendingRpcs = [];
	private readonly ConcurrentDictionary<int, TokenBucketRateLimiter> _rateLimitersByPeerId = [];

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
	}

	private void EnqueueRpc(int senderId, int tokens, Action action) => _ = EnqueueRpcAsync(senderId, tokens, action);

	private async Task EnqueueRpcAsync(int senderId, int tokens, Action action)
	{
		if (tokens > RateLimiterOptions.TokenLimit)
			return;

		var limiter = _rateLimitersByPeerId.GetOrAdd(
			senderId,
			static _ => new TokenBucketRateLimiter(RateLimiterOptions));

		try
		{
			using var lease = await limiter.AcquireAsync(tokens).ConfigureAwait(false);

			if (!lease.IsAcquired)
			{
				Log.Debug("Peer {PeerId} hit the RPC rate limit", senderId);
				return;
			}
		}
		catch (ObjectDisposedException)
		{
			return;
		}

		_pendingRpcs.Enqueue(action);
	}
}
