using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.RateLimiting;
using Godot;
using Godot.Collections;

namespace Root.Mp.Gd;

// TODO
public partial class GdMp
{
	// TODO: Remove Limiter on Player Removed
	private static readonly ConcurrentDictionary<int, TokenBucketRateLimiter> Limiters = [];

	private static readonly TokenBucketRateLimiterOptions LimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 10,
		TokensPerPeriod = 1,
		ReplenishmentPeriod = TimeSpan.FromSeconds(0.1),
		AutoReplenishment = true
	};

	private static readonly Lock Mutex = new();

	private static void RunRpc(int senderId, int tokens, Action action) =>
		_ = Task.Run(async () =>
		{
			var limiter = senderId == 1
				? null
				: Limiters.GetOrAdd(senderId, _ => new TokenBucketRateLimiter(LimiterOptions));

			using var lease = limiter is null ? null : await limiter.AcquireAsync(tokens).ConfigureAwait(true);

			try
			{
				if (lease is { IsAcquired: false })
					throw new InvalidOperationException(string.Create(
						CultureInfo.InvariantCulture,
						$"Lease not acquired for sender with id {senderId}"));

				lock (Mutex)
					action();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		});

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerAdded(string playerId, string name)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		RunRpc(senderId, 5, () => Players.Add(playerId, name));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerRemoved(string playerId)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		RunRpc(senderId, 5, () => Players.Remove(playerId));
	}

	/*[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncSetPlot(string playerId, int plotId)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		RunRpc(senderId, 10, () => Plots.SetPlot(playerId, plotId));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncIsSpawned(int plotId, bool isSpawned)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var plot = Plots.Get(plotId);

		RunRpc(
			senderId,
			10,
			() =>
			{
				if (isSpawned)
					plot!.Spawn();
				else
					plot!.Despawn();
			});
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncInstanceAdded(int plotId, int assetId, int instanceId, Vector3 position, Quaternion rotation)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var instances = Plots.Get(plotId)!.Instances;

		RunRpc(senderId, 1, () => instances.AddAt(assetId, position, rotation, instanceId));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncInstanceRemoved(int plotId, int instanceId)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var instances = Plots.Get(plotId)!.Instances;

		RunRpc(senderId, 1, () => instances.Remove(instanceId));
	}*/

	[Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncGameState(Array<string> players, Array<Dictionary> plots)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		RunRpc(
			senderId,
			20,
			() =>
			{
				foreach (var id in players)
					Players.Add(id);

				foreach (var plot in plots)
				{
					var plotId = plot["id"].As<int>();
					var gdPlot = Plots.Get(plotId);

					if (gdPlot is null)
						continue;

					if (plot.TryGetValue("occupantIds", out var occupantIds))
					{
						foreach (var playerId in occupantIds.AsGodotArray<string>())
							Plots.SetPlot(playerId, plotId);
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
			});
	}

	/*[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestSetPlot(int plotId)
	{
		if (!IsServer)
			return;

		var senderId = Multiplayer.GetRemoteSenderId();

		if (!_peers.TryGetValue(senderId, out var playerId))
			return;

		Rpc(MethodName.RpcSyncSetPlot, playerId, plotId);
	}*/

	private void SendGameState(int peerId, string localPlayerId)
	{
		var players = new Array<string>();

		foreach (var id in from player in Players.GetAll()
						   where !string.Equals(player.Id, localPlayerId, StringComparison.OrdinalIgnoreCase)
						   select player.Id)
			players.Add(id);

		var plots = GetPlotStates();
		RpcId(peerId, MethodName.RpcSyncGameState, players, plots);
	}

	private static Array<Dictionary> GetPlotStates()
	{
		var states = new Array<Dictionary>();

		foreach (var plot in Plots.GetAll())
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
	}
}
