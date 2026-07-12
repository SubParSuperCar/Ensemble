using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Godot;
using Godot.Collections;
using Root.Core.Gd.Plot;

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

	// We have to use this queue to ensure RPCs run on the right context if rate limiters yield
	private readonly ConcurrentQueue<Action> _rpcQueue = [];

	public override void _Process(double delta)
	{
		while (_rpcQueue.TryDequeue(out var action))
		{
			try
			{
				action();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}
	}

	private static void OnPeerDisconnectedRpc(long peerId)
	{
		if (RateLimitersByPeerId.TryRemove((int)peerId, out var limiter))
			limiter.Dispose();
	}

	// All RPCs are W.I.P. and not complete
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerAdded(string playerId, string name)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		EnqueueRpc(senderId, 5, () =>
		{
			GPlayers.Add(playerId, name);
			AddPeer(senderId, playerId);
		});
	}

	//

	[Rpc]
	private void RpcSyncGame(Array<Dictionary> players, Array<Dictionary> plots)
	{
		SyncPlayers(players);
		SyncPlots(plots);
	}

	private void SyncPlayers(Array<Dictionary> players)
	{
		foreach (var player in players)
		{
			try
			{
				var peerId = player["peerId"].As<int>();
				var playerId = player["playerId"].AsString();

				var displayName = string.Empty;

				if (player.TryGetValue("name", out var name))
					displayName = name.AsString();

				AddPeer(peerId, playerId);
				GPlayers.Add(playerId, displayName);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}

	private static void SyncPlots(Array<Dictionary> plots)
	{
		foreach (var plot in plots)
		{
			try
			{
				var plotId = plot["plotId"].As<int>();
				var gdPlot = GPlots.Get(plotId);

				if (gdPlot is null)
					continue;

				if (plot.TryGetValue("occupantPlayerIds", out var occupantIds))
					try
					{
						foreach (var occupantId in occupantIds.AsGodotArray<string>())
							GPlots.SetPlot(occupantId, plotId);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}

				if (plot.TryGetValue("instances", out var instances))
					foreach (var instance in instances.AsGodotArray<Dictionary>())
						try
						{
							var gdInstance = gdPlot.Instances.AddAt(
								instance["assetId"].As<int>(),
								instance["position"].As<Vector3>(),
								instance["rotation"].As<Quaternion>(),
								instance["instanceId"].As<int>());

							var properties = instance["properties"].AsGodotDictionary();
							gdInstance.Properties.UpdateAll(properties);
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
						}

				if (plot.TryGetValue("ownerPlayerId", out var ownerId))
					gdPlot.Occupants.SetOwner(ownerId.AsString());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}

	[Rpc(CallLocal = true)]
	private void RpcSyncPlayerAdded(int peerId, string playerId, string displayName)
	{
		try
		{
			AddPeer(peerId, playerId);
			GPlayers.Add(playerId, displayName);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	[Rpc(CallLocal = true)]
	private void RpcSyncPlayerRemoved(int peerId, string playerId)
	{
		try
		{
			GPlayers.Remove(playerId);
			RemovePeer(peerId);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	[Rpc(CallLocal = true)]
	private static void RpcSyncPlotChanged(string playerId, int plotId)
	{
		try
		{
			GPlots.SetPlot(playerId, plotId);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	[Rpc(CallLocal = true)]
	private static void RpcSyncOwnerChanged(int plotId, string playerId)
	{
		try
		{
			var plot = GPlots.Get(plotId);
			plot!.Occupants.SetOwner(playerId);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcSyncInstanceAdded(int assetId, Vector3 position, Quaternion rotation, int instanceId)
	{
		try
		{
			if (IsRemoteSenderPlotOwner(out var occupant))
				occupant.Plot!.Instances.AddAt(assetId, position, rotation, instanceId);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcSyncInstanceRemoved(int instanceId)
	{
		try
		{
			if (IsRemoteSenderPlotOwner(out var occupant))
				occupant.Plot!.Instances.Remove(instanceId);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	private bool IsRemoteSenderPlotOwner(out GdOccupant occupant)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var playerId = PlayerIdsByPeerId[senderId];

		occupant = GPlots.GetOccupant(playerId);
		return occupant == occupant.Plot?.Occupants.Owner;
	}

	//

	/*[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerRemoved(string playerId)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		EnqueueRpc(senderId, 5, () =>
		{
			GPlayers.Remove(playerId);
			RemovePeer(senderId);
		});
	}

	[Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncGameState(Array<string> players, Array<Dictionary> plots)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		EnqueueRpc(senderId, 20, () =>
		{
			foreach (var playerId in players)
				GPlayers.Add(playerId);

			foreach (var plot in plots)
				ApplyPlotState(plot);
		});
	}

	private static void ApplyPlotState(Dictionary plot)
	{
		var plotId = plot["id"].As<int>();
		var gdPlot = GPlots.Get(plotId);

		if (gdPlot is null)
			return;

		if (plot.TryGetValue("occupantIds", out var occupantIds))
		{
			foreach (var playerId in occupantIds.AsGodotArray<string>())
				GPlots.SetPlot(playerId, plotId);
		}

		if (plot.TryGetValue("ownerId", out var ownerId))
			gdPlot.Occupants.SetOwner(ownerId.AsString());

		if (plot.TryGetValue("instances", out var instances))
		{
			foreach (var instance in instances.AsGodotArray<Dictionary>())
				gdPlot.Instances.AddAt(
					instance["assetId"].As<int>(),
					instance["position"].As<Vector3>(),
					instance["rotation"].As<Quaternion>(),
					instance["instanceId"].As<int>());
		}

		if (plot.TryGetValue("isSpawned", out var isSpawned) && isSpawned.AsBool())
			gdPlot.Spawn();
	}

	private void SendGameState(int peerId, string localPlayerId)
	{
		var players = new Array<string>();

		foreach (var player in GPlayers.GetAll())
		{
			if (!string.Equals(player.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
				players.Add(player.Id);
		}

		var plots = GetPlotStates();
		RpcId(peerId, MethodName.RpcSyncGameState, players, plots);
	}

	private static Array<Dictionary> GetPlotStates()
	{
		var states = new Array<Dictionary>();

		foreach (var plot in GPlots.GetAll())
		{
			var state = plot.ToDict();

			if (plot.Occupants.Owner?.Player is { } owner)
				state["ownerId"] = owner.Id;

			var occupants = new Array<string>();

			foreach (var occupant in plot.Occupants.GetAll())
				occupants.Add(occupant.Player.Id);

			if (occupants.Count > 0)
				state["occupantIds"] = occupants;

			var instances = plot.Instances.GetAllDicts();

			if (instances.Count > 0)
				state["instances"] = instances;

			states.Add(state);
		}

		return states;
	}*/

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
