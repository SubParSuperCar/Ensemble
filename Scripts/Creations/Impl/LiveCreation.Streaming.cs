using System.Runtime.InteropServices;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Creations.Packets;
using EnsembleRoot.Scripts.Plots;
using Godot;

namespace EnsembleRoot.Scripts.Creations;

public partial class LiveCreation
{
	private const int StreamRate = 20;

	// Below a typical MTU, so ENet never fragments a packet (one lost fragment would lose them all)
	private const int MaxPacketBytes = 1200;
	private const int MaxStatesPerPacket = (MaxPacketBytes * 8 - CreationPackets.HeaderBits) / AssemblyState.Bits;

	// Past this, awake assemblies take turns (about 130 KB/s per receiver at most), each sent less often
	private const int MaxStatesPerStream = 256;

	// Sleeping assemblies are resent in turn, each about once a second but at most 40 a second, so a lost final update
	// can't leave a proxy off
	private const int KeepalivesPerStream = 2;

	// A simulator with nothing to send still sends a header this often, so the host knows it's alive
	private const int HeartbeatStreams = StreamRate;

	// The host relays up to this multiple of what a simulator should send, so only floods are cut
	private const double RelayAllowance = 1.5;

	// Proxies show the creation this far in the past, so a late state still arrives before it's needed
	private const double InterpolationDelay = 2d / StreamRate;

	// Of the way to its target, per received stream
	private const double ReplayDelayRate = 1 / 8d;

	// Whole physics ticks, so each sequence number stands for an exact moment on the simulator
	private static readonly int StreamTicks = Math.Max(1, Engine.PhysicsTicksPerSecond / StreamRate);
	private static readonly double StreamInterval = StreamTicks / (double)Engine.PhysicsTicksPerSecond;

	private readonly List<Assembly> _awakeAssemblies = [];
	private readonly HashSet<int> _awakeIds = [];
	private readonly List<AssemblyState> _receivedStates = [];
	private readonly List<Assembly> _restingAssemblies = [];
	private readonly List<AssemblyState> _streamedStates = [];
	private readonly StreamTimeline _timeline = new(StreamInterval);
	private readonly BitWriter _writer = new(MaxPacketBytes);

	private int _awakeCursor;
	private double _clock;
	private double _keepaliveCredit;
	private int _keepaliveIndex;
	private ushort _receivedSequence;
	private TokenGate _relayGate = null!;
	private double _replayDelay = InterpolationDelay;
	private int _sinceStream;
	private ushort _streamSequence;
	private int _streamsSinceSent;

	/// <summary>Every assembly's latest state, asleep or not, as one packet for a joining peer.</summary>
	internal byte[] CaptureStates()
	{
		var toLocal = Origin.AffineInverse();

		// Killed assemblies are included, so the joiner's copies fall out of the world and die too
		var states = _assemblies
			.Where(static assembly => assembly.Id <= AssemblyState.MaxId)
			.Select(assembly => GetState(assembly, toLocal, !assembly.IsAtRest))
			.ToArray();

		var header = new CreationPacketHeader(Plot.Id, Plot.SimulationEpoch, _streamSequence);
		return CreationPackets.Write(new BitWriter(), header, states);
	}

	/// <summary>Puts the proxies where a joining snapshot says the simulator had them.</summary>
	internal void RestoreStates(byte[] states)
	{
		if (!TryRead(states, out var header))
			return;

		Apply(header.Sequence);

		// The sequence may come from a counter that isn't the simulator's, so live states start the timeline over
		_timeline.Reset();
	}

	/// <summary>
	///     Takes a packet from the simulator, unless it's malformed, from another epoch, or (when relaying) beyond the
	///     rate the creation's size allows, so a bad one goes no further.
	/// </summary>
	internal bool TryReceive(byte[] data, bool isRelaying, out ushort sequence)
	{
		sequence = 0;

		if (
			data.Length > MaxPacketBytes || !TryRead(data, out var header) ||
			(isRelaying && !_relayGate.TryTake(_receivedStates.Count + 1)))
			return false;

		sequence = header.Sequence;
		UpdateReplayDelay(sequence);
		Apply(sequence);

		return true;
	}

	private void UpdateStream(double delta)
	{
		if (!PlotHandle.IsStreaming)
			return;

		_clock += delta;

		if (Plot.IsSimulatedLocally)
			Stream();
		else
			foreach (var assembly in _assemblies)
				assembly.UpdateProxy(_clock - _replayDelay);
	}

	private void ResetStream()
	{
		_awakeIds.Clear();
		_replayDelay = InterpolationDelay;
		_sinceStream = 0;
		_timeline.Reset();

		foreach (var assembly in _assemblies)
			assembly.ResetProxyTimeline();
	}

	private void Stream()
	{
		if (++_sinceStream < StreamTicks)
			return;

		_sinceStream = 0;
		_streamSequence++;

		SendAssetStates();
		GatherStreamedStates();

		if (_streamedStates.Count is 0 && ++_streamsSinceSent < HeartbeatStreams)
			return;

		_streamsSinceSent = 0;

		var states = CollectionsMarshal.AsSpan(_streamedStates);
		var header = new CreationPacketHeader(Plot.Id, Plot.SimulationEpoch, _streamSequence);
		var start = 0;

		do
		{
			var end = Math.Min(start + MaxStatesPerPacket, states.Length);

			Plot.SendCreation(CreationPackets.Write(_writer, header, states[start..end]), _streamSequence);
			start = end;
		} while (start < states.Length);
	}

	// Awake assemblies are sent every time (in turns past the budget); one falling asleep is sent once more, at
	// rest and first, as proxies keep that state, then only as a keepalive
	private void GatherStreamedStates()
	{
		var toLocal = Origin.AffineInverse();
		_streamedStates.Clear();
		_awakeAssemblies.Clear();
		_restingAssemblies.Clear();

		foreach (var assembly in _assemblies)
		{
			if (assembly is not { IsKilled: false, Id: <= AssemblyState.MaxId })
				continue;

			if (!assembly.IsAtRest)
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

		// Small creations would otherwise resend their few bodies many times a second
		_keepaliveCredit = Math.Min(_keepaliveCredit + _assemblies.Count / (double)StreamRate, KeepalivesPerStream);

		for (; _keepaliveCredit >= 1 && _assemblies.Count > 0; _keepaliveCredit--)
		{
			_keepaliveIndex = (_keepaliveIndex + 1) % _assemblies.Count;
			var assembly = _assemblies[_keepaliveIndex];

			if (
				assembly is { IsKilled: false, IsAtRest: true, Id: <= AssemblyState.MaxId } &&
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

	// State from another epoch (an earlier spawn, or one this peer hasn't caught up to yet) is dropped
	private bool TryRead(byte[] data, out CreationPacketHeader header) =>
		CreationPackets.TryRead(data, out header, _receivedStates) &&
		header.PlotId == Plot.Id && header.Epoch == Plot.SimulationEpoch;

	// What the creation's size lets a simulator send, plus the allowance
	private double GetRelayRate()
	{
		var statesPerStream = Math.Min(_assemblies.Count, MaxStatesPerStream) + KeepalivesPerStream;
		var packetsPerStream = statesPerStream / MaxStatesPerPacket + 1;

		return (statesPerStream + packetsPerStream) * StreamRate * RelayAllowance;
	}

	// The host may relay only some streams, which the replay then needs to stay behind in proportion. Longer gaps are
	// creations at rest, and a lost packet only nudges this
	private void UpdateReplayDelay(ushort sequence)
	{
		var gap = (short)(sequence - _receivedSequence);
		_receivedSequence = sequence;

		if (gap is > 0 and <= RelayBudget.MaxDivisor)
			_replayDelay += (gap * InterpolationDelay - _replayDelay) * ReplayDelayRate;
	}

	private void Apply(ushort sequence)
	{
		var time = _timeline.GetTime(sequence, _clock);

		foreach (var state in _receivedStates)
			if (state.Id < _assemblies.Count && _assemblies[state.Id] is { IsKilled: false } assembly)
				assembly.AddProxyState(
					time, StreamInterval, Origin * new Transform3D(new Basis(state.Rotation), state.Position),
					Origin.Basis * state.LinearVelocity, Origin.Basis * state.AngularVelocity);
	}
}
