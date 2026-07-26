using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Godot;
using Godot.Collections;
using Root.Core.Gd.Plot;
using Serilog;

namespace Root.SessionManager.Gd;

// TODO
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

	private readonly ConcurrentQueue<Action> _rpcQueue = [];

	public override void _Process(double delta)
	{
		while (_rpcQueue.TryDequeue(out var action))
			RunSafely(action);
	}

	private static void OnPeerDisconnectedRpc(long peerId)
	{
		if (RateLimitersByPeerId.TryRemove((int)peerId, out var limiter))
			limiter.Dispose();
	}

	[Rpc]
	private void RpcSyncGame(Array<Dictionary> players, Array<Dictionary> plots)
	{
		SyncPlayers(players);
		SyncPlots(plots);
	}

	private void SyncPlayers(Array<Dictionary> players)
	{
		foreach (var player in players)
			RunSafely(() =>
			{
				var peerId = player["peerId"].As<int>();
				var playerId = player["playerId"].AsString();

				var displayName = string.Empty;

				if (player.TryGetValue("name", out var name))
					displayName = name.AsString();

				AddPeer(peerId, playerId);
				GPlayers.Add(playerId, displayName);
			});
	}

	private static void SyncPlots(Array<Dictionary> plots)
	{
		foreach (var plot in plots)
			RunSafely(() =>
			{
				var plotId = plot["plotId"].As<int>();
				var gdPlot = GPlots.Get(plotId);

				if (gdPlot is null)
					return;

				if (plot.TryGetValue("occupantPlayerIds", out var occupantIds))
					RunSafely(() =>
					{
						foreach (var occupantId in occupantIds.AsGodotArray<string>())
							GPlots.SetPlot(occupantId, plotId);
					});

				if (plot.TryGetValue("instances", out var instances))
					foreach (var instance in instances.AsGodotArray<Dictionary>())
						RunSafely(() =>
						{
							var gdInstance = gdPlot.Instances.AddAt(
								instance["assetId"].As<int>(),
								instance["position"].As<Vector3>(),
								instance["rotation"].As<Quaternion>(),
								instance["instanceId"].As<int>());

							var properties = instance["properties"].AsGodotDictionary();
							gdInstance.Properties.UpdateAll(properties);
						});

				if (plot.TryGetValue("ownerPlayerId", out var ownerId))
					gdPlot.Occupants.SetOwner(ownerId.AsString());
			});
	}

	[Rpc(CallLocal = true)]
	private void RpcSyncPlayerAdded(int peerId, string playerId, string displayName) =>
		RunSafely(() =>
		{
			AddPeer(peerId, playerId);
			GPlayers.Add(playerId, displayName);
		});

	[Rpc(CallLocal = true)]
	private void RpcSyncPlayerRemoved(int peerId, string playerId) =>
		RunSafely(() =>
		{
			GPlayers.Remove(playerId);
			RemovePeer(peerId);
		});

	[Rpc(CallLocal = true)]
	private static void RpcSyncPlotChanged(string playerId, int plotId) =>
		RunSafely(() => GPlots.SetPlot(playerId, plotId));

	[Rpc(CallLocal = true)]
	private static void RpcSyncOwnerChanged(int plotId, string playerId) =>
		RunSafely(() =>
		{
			var plot = GPlots.Get(plotId);
			plot!.Occupants.SetOwner(playerId);
		});

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcSyncInstanceAdded(int assetId, Vector3 position, Quaternion rotation, int instanceId) =>
		RunSafely(() =>
		{
			if (IsRemoteSenderPlotOwner(out var occupant))
				occupant.Plot!.Instances.AddAt(assetId, position, rotation, instanceId);
		});

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcSyncInstanceRemoved(int instanceId) =>
		RunSafely(() =>
		{
			if (IsRemoteSenderPlotOwner(out var occupant))
				occupant.Plot!.Instances.Remove(instanceId);
		});

	private bool IsRemoteSenderPlotOwner([NotNullWhen(true)] out GdOccupant? occupant)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var playerId = PlayerIdsByPeerId[senderId];

		occupant = GPlots.GetOccupant(playerId);
		return occupant is not null && occupant == occupant.Plot?.Occupants.Owner;
	}

	private static void RunSafely(Action action)
	{
		try
		{
			action();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "");
		}
	}

	// ReSharper disable once UnusedMember.Local
	private void EnqueueRpc(int senderId, int tokens, Action action) => _ = EnqueueRpcAsync(senderId, tokens, action);

	private async Task EnqueueRpcAsync(int senderId, int tokens, Action action)
	{
		var limiter = senderId == 1
			? null
			: RateLimitersByPeerId.GetOrAdd(senderId, _ => new TokenBucketRateLimiter(RateLimiterOptions));

		using var lease = limiter is null
			? null
			: await limiter.AcquireAsync(tokens).ConfigureAwait(false);

		if (lease is { IsAcquired: false })
			return;

		_rpcQueue.Enqueue(action);
	}
}
