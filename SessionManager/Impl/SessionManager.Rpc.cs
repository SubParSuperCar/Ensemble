using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Serilog;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	private const int ServerPeerId = 1;

	private static readonly ConcurrentDictionary<int, TokenBucketRateLimiter> RateLimitersByPeerId = [];

	private static readonly TokenBucketRateLimiterOptions RateLimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 10,
		TokensPerPeriod = 1,
		ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
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
			Log.Error(exception, "Unhandled exception in RPC action");
		}
	}

	private static void DisposeRateLimiter(long peerId)
	{
		if (RateLimitersByPeerId.TryRemove((int)peerId, out var limiter))
			limiter.Dispose();
	}

	private void ClearRpcState()
	{
		foreach (var peerId in RateLimitersByPeerId.Keys)
			DisposeRateLimiter(peerId);

		_pendingRpcs.Clear();
	}

	private void EnqueueRpc(int senderId, int tokens, Action action) => _ = EnqueueRpcAsync(senderId, tokens, action);

	private async Task EnqueueRpcAsync(int senderId, int tokens, Action action)
	{
		if (senderId is not ServerPeerId)
		{
			if (tokens > RateLimiterOptions.TokenLimit)
			{
				Log.Warning(
					"Peer {PeerId} requested an RPC costing {Tokens} token(s), exceeding the limit of {TokenLimit}",
					senderId,
					tokens,
					RateLimiterOptions.TokenLimit);

				return;
			}

			var limiter = RateLimitersByPeerId.GetOrAdd(
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
		}

		_pendingRpcs.Enqueue(action);
	}
}
