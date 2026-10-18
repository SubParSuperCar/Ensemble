using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const int CreationChannel = 2;
	private const double StreamInterval = 1 / 20d;

	// Below a typical MTU, so ENet never fragments a packet (one lost fragment would lose them all)
	private const int MaxPacketBytes = 1200;
	private const int MaxStatesPerPacket = (MaxPacketBytes * 8 - CreationPackets.HeaderBits) / AssemblyState.Bits;

	// Sleeping assemblies are resent in turn, about 40 a second, so a lost final update can't leave a proxy off
	private const int KeepalivesPerStream = 2;

	private readonly Dictionary<int, Assembly> _assembliesById = [];
	private readonly HashSet<int> _awakeIds = [];
	private readonly List<AssemblyState> _receivedStates = [];
	private readonly List<AssemblyState> _streamedStates = [];
	private readonly BitWriter _writer = new(MaxPacketBytes);

	private int _keepaliveIndex;
	private double _sinceStream;
	private ushort _streamSequence;

	private static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	/// <summary>The simulation epoch and every assembly's latest state, asleep or not, for a joining peer.</summary>
	public (ushort Epoch, byte[] States) CaptureCreation()
	{
		var toLocal = _staticInstances.GlobalTransform.AffineInverse();
		var states = _assemblies
			.Where(static assembly => assembly.Id <= AssemblyState.MaxId)
			.Select(assembly => GetState(assembly, toLocal, !assembly.Sleeping))
			.ToList();

		var header = new CreationPacketHeader(Id, SimulationEpoch, 0);
		return (SimulationEpoch, CreationPackets.Write(new BitWriter(), header, states));
	}

	/// <summary>Adopts a joining snapshot's epoch and assembly states, once the snapshot has spawned the plot.</summary>
	public void RestoreCreation(ushort epoch, byte[] states)
	{
		SimulationEpoch = epoch;

		if (_plot.IsSpawned)
			Receive(states);
	}

	private void UpdateReplication(double delta)
	{
		if (!IsStreaming)
			return;

		if (IsSimulatedLocally)
			Stream(delta);
		else
			foreach (var assembly in _assemblies)
				assembly.UpdateProxy(delta);
	}

	// Awake assemblies are sent every time; one falling asleep is sent once more, at rest, then only as a keepalive
	private void Stream(double delta)
	{
		_sinceStream += delta;
		if (_sinceStream < StreamInterval)
			return;

		_sinceStream = 0;

		var toLocal = _staticInstances.GlobalTransform.AffineInverse();
		_streamedStates.Clear();

		foreach (var assembly in _assemblies)
		{
			if (assembly is not { IsKilled: false, Id: <= AssemblyState.MaxId })
				continue;

			if (!assembly.Sleeping)
			{
				_streamedStates.Add(GetState(assembly, toLocal, true));
				_awakeIds.Add(assembly.Id);
			}
			else if (_awakeIds.Remove(assembly.Id))
				_streamedStates.Add(GetState(assembly, toLocal, false));
		}

		for (var i = 0; i < KeepalivesPerStream && _assemblies.Count > 0; i++)
		{
			var assembly = _assemblies[_keepaliveIndex = (_keepaliveIndex + 1) % _assemblies.Count];

			if (assembly is { IsKilled: false, Sleeping: true, Id: <= AssemblyState.MaxId } &&
				!_awakeIds.Contains(assembly.Id))
				_streamedStates.Add(GetState(assembly, toLocal, false));
		}

		if (_streamedStates.Count is 0)
			return;

		_streamSequence++;

		for (var start = 0; start < _streamedStates.Count; start += MaxStatesPerPacket)
		{
			var count = Math.Min(MaxStatesPerPacket, _streamedStates.Count - start);
			var header = new CreationPacketHeader(Id, SimulationEpoch, _streamSequence);
			var data = CreationPackets.Write(_writer, header, _streamedStates.GetRange(start, count));

			Send(data, GSessionManager.LocalPeerId);
		}
	}

	private static AssemblyState GetState(Assembly assembly, Transform3D toLocal, bool isAwake)
	{
		var transform = toLocal * assembly.GlobalTransform;
		var linearVelocity = isAwake ? toLocal.Basis * assembly.LinearVelocity : Vector3.Zero;
		var angularVelocity = isAwake ? toLocal.Basis * assembly.AngularVelocity : Vector3.Zero;

		return new AssemblyState(
			assembly.Id, transform.Origin, transform.Basis.GetRotationQuaternion(), linearVelocity, angularVelocity);
	}

	// The host relays to every other synced peer; anyone else sends to the host
	private void Send(byte[] data, int sourcePeerId)
	{
		if (!GSessionManager.IsServer)
		{
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReceiveCreation, data);
			return;
		}

		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && peerId != GSessionManager.LocalPeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcReceiveCreation, data);
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = CreationChannel)]
	private void RpcReceiveCreation(byte[] data)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		// TODO: Rate limit relays once owners simulate their creations (0.54.0)
		if (GSessionManager.IsServer)
		{
			if (senderId != SimulatorPeerId)
				return;

			Send(data, senderId);
		}
		else if (senderId != MultiplayerPeer.TargetPeerServer)
			return;

		Receive(data);
	}

	// State from another epoch (an earlier spawn, or one this peer hasn't caught up to yet) is dropped
	private void Receive(byte[] data)
	{
		if (
			!_plot.IsSpawned || IsSimulatedLocally ||
			!CreationPackets.TryRead(data, out var header, _receivedStates) ||
			header.PlotId != Id || header.Epoch != SimulationEpoch)
			return;

		var origin = _staticInstances.GlobalTransform;

		foreach (var state in _receivedStates)
			if (_assembliesById.TryGetValue(state.Id, out var assembly) && !assembly.IsKilled)
				assembly.SetProxyTarget(
					origin * new Transform3D(new Basis(state.Rotation), state.Position),
					origin.Basis * state.LinearVelocity, origin.Basis * state.AngularVelocity);
	}
}
