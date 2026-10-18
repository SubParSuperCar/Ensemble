using System.Diagnostics;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using EnsembleRoot.Sessions;
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

	// Past this, awake assemblies take turns (about 130 KB/s per receiver at most), each sent less often
	private const int MaxStatesPerStream = 256;

	// Sleeping assemblies are resent in turn, about 40 a second, so a lost final update can't leave a proxy off
	private const int KeepalivesPerStream = 2;

	// A simulator with nothing to send still sends a header this often, so the host knows it's alive
	private const int HeartbeatStreams = StreamRate;

	// The host relays up to this much more than a simulator should send, so only floods are cut
	private const double RelayAllowance = 1.5;

	// Proxies show the creation this far in the past, so a late state still arrives before it's needed
	private const double InterpolationDelay = 2d / StreamRate;

	// How fast a timeline adapts to states arriving later than before (they move it earlier at once)
	private const double TimelineDriftRate = 1 / 16d;

	// Whole physics ticks, so each sequence number stands for an exact moment on the simulator
	private static readonly int StreamTicks = Math.Max(1, Engine.PhysicsTicksPerSecond / StreamRate);
	private static readonly double StreamInterval = StreamTicks / (double)Engine.PhysicsTicksPerSecond;

	private readonly Dictionary<int, Assembly> _assembliesById = [];
	private readonly List<Assembly> _awakeAssemblies = [];
	private readonly HashSet<int> _awakeIds = [];
	private readonly List<AssemblyState> _receivedStates = [];
	private readonly List<Assembly> _restingAssemblies = [];
	private readonly List<AssemblyState> _streamedStates = [];
	private readonly BitWriter _writer = new(MaxPacketBytes);

	private int _awakeCursor;
	private double _clock;
	private int _keepaliveIndex;
	private long _receivedSequence;
	private long _relayRefillTimestamp;
	private double _relayTokens;
	private int _sinceStream;
	private ushort _streamSequence;
	private int _streamsSinceSent;

	// The local time of sequence 0 on the simulator's clock, or null until the epoch's first state
	private double? _timelineOrigin;

	private static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	/// <summary>
	///     The simulation epoch, the simulator, and every assembly's latest state, asleep or not, for a joining peer.
	/// </summary>
	public (ushort Epoch, int SimulatorPeerId, byte[] States) CaptureCreation()
	{
		var toLocal = _staticInstances.GlobalTransform.AffineInverse();
		var states = _assemblies
			.Where(static assembly => assembly.Id <= AssemblyState.MaxId)
			.Select(assembly => GetState(assembly, toLocal, !assembly.Sleeping))
			.ToList();

		var header = new CreationPacketHeader(Id, SimulationEpoch, _streamSequence);
		return (SimulationEpoch, SimulatorPeerId, CreationPackets.Write(new BitWriter(), header, states));
	}

	/// <summary>Adopts a joining snapshot's simulation state, once the snapshot has spawned the plot.</summary>
	public void RestoreCreation(ushort epoch, int simulatorPeerId, byte[] states)
	{
		SimulationEpoch = epoch;
		SetSimulator(simulatorPeerId);

		if (_plot.IsSpawned && !IsSimulatedLocally && TryRead(states, out var header))
			Apply(header.Sequence);
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

	private void Stream()
	{
		if (++_sinceStream < StreamTicks)
			return;

		_sinceStream = 0;
		_streamSequence++;

		GatherStreamedStates();

		if (_streamedStates.Count is 0 && ++_streamsSinceSent < HeartbeatStreams)
			return;

		_streamsSinceSent = 0;
		var start = 0;

		do
		{
			var count = Math.Min(MaxStatesPerPacket, _streamedStates.Count - start);
			var header = new CreationPacketHeader(Id, SimulationEpoch, _streamSequence);
			var data = CreationPackets.Write(_writer, header, _streamedStates.GetRange(start, count));

			Send(data, GSessionManager.LocalPeerId);
			start += MaxStatesPerPacket;
		} while (start < _streamedStates.Count);
	}

	// Awake assemblies are sent every time (in turns past the budget); one falling asleep is sent once more, at
	// rest and first, as proxies keep that state, then only as a keepalive
	private void GatherStreamedStates()
	{
		var toLocal = _staticInstances.GlobalTransform.AffineInverse();
		_streamedStates.Clear();
		_awakeAssemblies.Clear();
		_restingAssemblies.Clear();

		foreach (var assembly in _assemblies)
		{
			if (assembly is not { IsKilled: false, Id: <= AssemblyState.MaxId })
				continue;

			if (!assembly.Sleeping)
			{
				_awakeAssemblies.Add(assembly);
				_awakeIds.Add(assembly.Id);
			}
			else if (_awakeIds.Contains(assembly.Id))
				_restingAssemblies.Add(assembly);
		}

		for (var i = 0; i < _restingAssemblies.Count && i < MaxStatesPerStream; i++)
		{
			_streamedStates.Add(GetState(_restingAssemblies[i], toLocal, false));
			_awakeIds.Remove(_restingAssemblies[i].Id);
		}

		var room = Math.Min(MaxStatesPerStream - _streamedStates.Count, _awakeAssemblies.Count);

		for (var i = 0; i < room; i++)
			_streamedStates.Add(GetState(_awakeAssemblies[(_awakeCursor + i) % _awakeAssemblies.Count], toLocal, true));

		if (_awakeAssemblies.Count > 0)
			_awakeCursor = (_awakeCursor + room) % _awakeAssemblies.Count;

		for (var i = 0; i < KeepalivesPerStream && _assemblies.Count > 0; i++)
		{
			var assembly = _assemblies[_keepaliveIndex = (_keepaliveIndex + 1) % _assemblies.Count];

			if (assembly is { IsKilled: false, Sleeping: true, Id: <= AssemblyState.MaxId } &&
				!_awakeIds.Contains(assembly.Id))
				_streamedStates.Add(GetState(assembly, toLocal, false));
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
		NetworkConditions.Receive(this, () => ReceiveCreation(senderId, data));
	}

	// The host checks a packet before relaying it, so a bad one goes no further
	private void ReceiveCreation(int senderId, byte[] data)
	{
		if (
			senderId != (GSessionManager.IsServer ? SimulatorPeerId : HostPeerId) ||
			!_plot.IsSpawned || IsSimulatedLocally ||
			!TryRead(data, out var header) || _receivedStates.Count > MaxStatesPerPacket)
			return;

		if (GSessionManager.IsServer)
		{
			if (!TryTakeRelayTokens(_receivedStates.Count + 1))
				return;

			_sinceSimulatorHeard = 0;
			Send(data, senderId);
		}

		Apply(header.Sequence);
	}

	// State from another epoch (an earlier spawn, or one this peer hasn't caught up to yet) is dropped
	private bool TryRead(byte[] data, out CreationPacketHeader header) =>
		CreationPackets.TryRead(data, out header, _receivedStates) &&
		header.PlotId == Id && header.Epoch == SimulationEpoch;

	// Refilled with time, at what the creation's size allows the simulator to send, plus the allowance
	private bool TryTakeRelayTokens(int cost)
	{
		var statesPerStream = Math.Min(_assemblies.Count, MaxStatesPerStream) + KeepalivesPerStream;
		var packetsPerStream = statesPerStream / MaxStatesPerPacket + 1;
		var perSecond = (statesPerStream + packetsPerStream) * StreamRate * RelayAllowance;

		var now = Stopwatch.GetTimestamp();
		var elapsed = Stopwatch.GetElapsedTime(_relayRefillTimestamp, now).TotalSeconds;

		_relayRefillTimestamp = now;
		_relayTokens = Math.Min(perSecond / 2, _relayTokens + elapsed * perSecond);

		if (_relayTokens < cost)
			return false;

		_relayTokens -= cost;
		return true;
	}

	private void Apply(ushort sequence)
	{
		var time = GetStateTime(sequence);
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
