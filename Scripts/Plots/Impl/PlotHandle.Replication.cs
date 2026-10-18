using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const int CreationChannel = 2;
	private const int StreamRate = 20;

	// Below a typical MTU, so ENet never fragments a packet (one lost fragment would lose them all)
	private const int MaxPacketBytes = 1200;
	private const int MaxStatesPerPacket = (MaxPacketBytes * 8 - CreationPackets.HeaderBits) / AssemblyState.Bits;

	// Sleeping assemblies are resent in turn, about 40 a second, so a lost final update can't leave a proxy off
	private const int KeepalivesPerStream = 2;

	// Proxies show the creation this far in the past, so a late state still arrives before it's needed
	private const double InterpolationDelay = 2d / StreamRate;

	// How fast a timeline adapts to states arriving later than before (they move it earlier at once)
	private const double TimelineDriftRate = 1 / 16d;

	// Whole physics ticks, so each sequence number stands for an exact moment on the simulator
	private static readonly int StreamTicks =
		Math.Max(1, (int)Math.Round(Engine.PhysicsTicksPerSecond / (double)StreamRate));
	private static readonly double StreamInterval = StreamTicks / (double)Engine.PhysicsTicksPerSecond;

	private readonly Dictionary<int, Assembly> _assembliesById = [];
	private readonly HashSet<int> _awakeIds = [];
	private readonly List<AssemblyState> _receivedStates = [];
	private readonly List<AssemblyState> _streamedStates = [];
	private readonly BitWriter _writer = new(MaxPacketBytes);

	private double _clock;
	private int _keepaliveIndex;
	private long _receivedSequence;
	private int _sinceStream;
	private ushort _streamSequence;

	// The local time of sequence 0 on the simulator's clock, or null until the epoch's first state
	private double? _timelineOrigin;

	private static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	/// <summary>The simulation epoch and every assembly's latest state, asleep or not, for a joining peer.</summary>
	public (ushort Epoch, byte[] States) CaptureCreation()
	{
		var toLocal = _staticInstances.GlobalTransform.AffineInverse();
		var states = _assemblies
			.Where(static assembly => assembly.Id <= AssemblyState.MaxId)
			.Select(assembly => GetState(assembly, toLocal, !assembly.Sleeping))
			.ToList();

		var header = new CreationPacketHeader(Id, SimulationEpoch, _streamSequence);
		return (SimulationEpoch, CreationPackets.Write(new BitWriter(), header, states));
	}

	/// <summary>Adopts a joining snapshot's epoch and assembly states, once the snapshot has spawned the plot.</summary>
	public void RestoreCreation(ushort epoch, byte[] states)
	{
		SimulationEpoch = epoch;
		ResetTimeline();

		if (_plot.IsSpawned)
			Receive(states);
	}

	private void UpdateReplication(double delta)
	{
		if (!IsStreaming)
			return;

		_clock += delta;

		if (IsSimulatedLocally)
			Stream();
		else
			foreach (var assembly in _assemblies)
				assembly.UpdateProxy(_clock - InterpolationDelay);
	}

	// States of a new epoch may come from another simulator, whose sequence numbers mean other moments
	private void ResetTimeline()
	{
		_timelineOrigin = null;

		foreach (var assembly in _assemblies)
			assembly.ResetProxyTimeline();
	}

	// Awake assemblies are sent every time; one falling asleep is sent once more, at rest, then only as a keepalive
	private void Stream()
	{
		if (++_sinceStream < StreamTicks)
			return;

		_sinceStream = 0;
		_streamSequence++;

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

		var time = GetStateTime(header.Sequence);
		var origin = _staticInstances.GlobalTransform;

		foreach (var state in _receivedStates)
			if (_assembliesById.TryGetValue(state.Id, out var assembly) && !assembly.IsKilled)
				assembly.AddProxyState(
					time, StreamInterval, origin * new Transform3D(new Basis(state.Rotation), state.Position),
					origin.Basis * state.LinearVelocity, origin.Basis * state.AngularVelocity);
	}

	// Maps a sequence number to local time, following the earliest arrivals, as later ones were only delayed
	private double GetStateTime(ushort sequence)
	{
		_receivedSequence = _timelineOrigin is null
			? sequence
			: _receivedSequence + (short)(sequence - (ushort)_receivedSequence);

		var simulatorTime = _receivedSequence * StreamInterval;
		var origin = _clock - simulatorTime;

		_timelineOrigin = _timelineOrigin is { } current && origin > current
			? current + (origin - current) * TimelineDriftRate
			: origin;

		return simulatorTime + _timelineOrigin.Value;
	}
}
