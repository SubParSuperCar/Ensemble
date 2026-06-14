using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Godot;
using Godot.Collections;

namespace Root.Mp.Gd;

// TODO
public partial class GdMp
{
	private static readonly ConcurrentDictionary<int, TokenBucketRateLimiter> Limiters = [];

	private static readonly TokenBucketRateLimiterOptions LimiterOptions = new()
	{
		TokenLimit = 100,
		QueueLimit = 10,
		TokensPerPeriod = 1,
		ReplenishmentPeriod = TimeSpan.FromSeconds(0.1),
		AutoReplenishment = true
	};

	private readonly ConcurrentQueue<Action> _rpcQueue = [];

	private void RunRpc(int senderId, int tokens, Action action) => _ = RunRpcAsync(senderId, tokens, action);

	private async Task RunRpcAsync(int senderId, int tokens, Action action)
	{
		var limiter = senderId == 1
			? null
			: Limiters.GetOrAdd(senderId, _ => new TokenBucketRateLimiter(LimiterOptions));

		using var lease = limiter is null
			? null
			: await limiter.AcquireAsync(tokens);

		if (lease is { IsAcquired: false })
			return;

		_rpcQueue.Enqueue(action);
	}

	public override void _Process(double delta)
	{
		while (_rpcQueue.TryDequeue(out var action))
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}

	private static void OnPeerDisconnectedRpc(long id)
	{
		if (Limiters.TryRemove((int)id, out var limiter))
			limiter.Dispose();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerAdded(string playerId, string name)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		RunRpc(senderId, 5, () =>
		{
			Players.Add(playerId, name);
			AddPeer(senderId, playerId);
		});
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncPlayerRemoved(string playerId)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		RunRpc(senderId, 5, () =>
		{
			Players.Remove(playerId);
			RemovePeer(senderId);
		});
	}

	[Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RpcSyncGameState(Array<string> players, Array<Dictionary> plots)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		RunRpc(senderId, 20, () =>
		{
			foreach (var playerId in players)
				Players.Add(playerId);

			foreach (var plot in plots)
				ApplyPlotState(plot);
		});
	}

	private static void ApplyPlotState(Dictionary plot)
	{
		var plotId = plot["id"].As<int>();
		var gdPlot = Plots.Get(plotId);

		if (gdPlot is null)
			return;

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
