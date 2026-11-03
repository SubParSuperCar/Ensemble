using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotManager
{
	// Bytes per second per peer: one creation at its full stream budget, and half of another
	private const double RelayBudgetPerPeer = 192 * 1024d;
	private const double RelayBudgetInterval = 1d;
	private readonly List<(PlotHandle Handle, double Rate, float Distance)> _relayOrder = [];

	private readonly List<(PlotHandle Handle, double Rate)> _streamingHandles = [];

	private double _sinceRelayBudget;

	private void UpdateRelayBudgets(double delta)
	{
		if (!GSessionManager.IsServer || !PlotHandle.IsStreaming)
			return;

		_sinceRelayBudget += delta;

		if (_sinceRelayBudget < RelayBudgetInterval)
			return;

		_streamingHandles.Clear();

		foreach (var handle in _handlesById.Values)
			_streamingHandles.Add((handle, handle.TakeRelayRate(_sinceRelayBudget)));

		_sinceRelayBudget = 0;

		foreach (var peer in GSessionManager.Peers.Values)
			if (peer.Id != GSessionManager.LocalPeerId && GPlayerManager.GetHandleOrNull(peer.PlayerId) is { } player)
				ShareRelayBudget(peer.Id, player.Body.GlobalPosition);
	}

	private void ShareRelayBudget(int peerId, Vector3 position)
	{
		_relayOrder.Clear();

		foreach (var (handle, rate) in _streamingHandles)
			if (handle.Creation is { } creation && handle.SimulatorPeerId != peerId)
				_relayOrder.Add((handle, rate, creation.GetDistanceTo(position)));
			else
				handle.SetRelayDivisor(peerId, 1);

		_relayOrder.Sort(static (left, right) => left.Distance.CompareTo(right.Distance));

		var remaining = RelayBudgetPerPeer;

		foreach (var (handle, rate, _) in _relayOrder)
			handle.SetRelayDivisor(peerId, RelayBudget.Take(ref remaining, rate));
	}
}
