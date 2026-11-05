using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Creations;
using EnsembleRoot.Scripts.Players;
using EnsembleRoot.Sessions;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// So a lost update heals, whether it was a press or a release
	private const double InputResendInterval = 1 / 4d;

	// A simulator lets go of the keys after this many missed updates, should a controller hang while still connected
	private const double InputTimeout = 4 * InputResendInterval;

	// Bounds what the host relays for a flooding controller, past any key press a player could make
	private const double InputRelaysPerSecond = 120d;

	private readonly TokenGate _inputRelayGate = new(InputRelaysPerSecond, GTimeProvider);

	private InputState _sentInput;
	private double _sinceInputHeard;
	private double _sinceInputSent;

	/// <summary>
	///     The plot's occupant whose keys drive its creation: the oldest one sitting in one of its seats with controls.
	///     Every peer derives it alike from who occupies the plot and who sits where.
	/// </summary>
	internal PlayerHandle? GetController()
	{
		if (Creation is not { IsControlSeatTaken: true })
			return null;

		foreach (var occupant in _plot.Occupants.GetAll())
			if (
				GPlayerManager.GetHandleOrNull(occupant.Player.Id) is { Seat: { HasControls: true } seat } player &&
				seat.Handle.Plot == this)
				return player;

		return null;
	}

	private void UpdateInput(double delta)
	{
		if (Creation is not { } creation)
			return;

		var controller = GetController();

		if (controller is null)
		{
			creation.Input = default;
			return;
		}

		if (controller != GPlayerManager.LocalHandle)
		{
			_sinceInputHeard += delta;

			if (_sinceInputHeard >= InputTimeout)
				creation.Input = default;

			return;
		}

		_sinceInputSent += delta;

		var input = InputState.Sample(creation.BoundKeys);

		if (input == _sentInput && _sinceInputSent < InputResendInterval)
			return;

		_sentInput = input;
		_sinceInputSent = 0;

		RouteInput(creation, input);
	}

	// Toward the simulator, always through the host, which alone knows whom to trust
	private void RouteInput(LiveCreation creation, InputState input)
	{
		if (IsSimulatedLocally)
		{
			creation.Input = input;
			_sinceInputHeard = 0;
		}
		else if (GSessionManager.IsServer)
			RpcId(SimulatorPeerId, MethodName.RpcReceiveInput, (long)input.Mask);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReceiveInput, (long)input.Mask);
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = NetworkChannels.Input)]
	private void RpcReceiveInput(long mask)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		NetworkConditions.Receive(this, () => ReceiveInput(senderId, new InputState((ulong)mask)));
	}

	// The host takes only the controller's input; a simulator takes only the host's relays
	private void ReceiveInput(int senderId, InputState input)
	{
		if (Creation is not { } creation)
			return;

		if (!GSessionManager.IsServer)
		{
			if (senderId == HostPeerId && IsSimulatedLocally)
				RouteInput(creation, input);

			return;
		}

		if (
			GetController() is not { } controller ||
			GSessionManager.GetPeerByPlayerId(controller.Id)?.Id != senderId ||
			!GSessionManager.IsPeerSynced(senderId) || !_inputRelayGate.TryTake())
			return;

		RouteInput(creation, input);
	}
}
