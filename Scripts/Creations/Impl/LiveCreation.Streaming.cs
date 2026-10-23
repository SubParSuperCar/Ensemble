using System.Diagnostics;
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
	private long _relayRefillTimestamp;
	private double _relayTokens;
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
			.Select(assembly => GetState(assembly, toLocal, !assembly.Sleeping))
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
	internal bool TryReceive(byte[] data, bool isRelaying)
	{
		if (data.Length > MaxPacketBytes || !TryRead(data, out var header) ||
			(isRelaying && !TryTakeRelayTokens(_receivedStates.Count + 1)))
			return false;

		Apply(header.Sequence);
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
				assembly.UpdateProxy(_clock - InterpolationDelay);
	}

	private void ResetStream()
	{
		_awakeIds.Clear();
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

			Plot.SendCreation(CreationPackets.Write(_writer, header, states[start..end]));
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

		// Small creations would otherwise resend their few bodies many times a second
		_keepaliveCredit = Math.Min(_keepaliveCredit + _assemblies.Count / (double)StreamRate, KeepalivesPerStream);

		for (; _keepaliveCredit >= 1 && _assemblies.Count > 0; _keepaliveCredit--)
		{
			_keepaliveIndex = (_keepaliveIndex + 1) % _assemblies.Count;
			var assembly = _assemblies[_keepaliveIndex];

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

	// State from another epoch (an earlier spawn, or one this peer hasn't caught up to yet) is dropped
	private bool TryRead(byte[] data, out CreationPacketHeader header) =>
		CreationPackets.TryRead(data, out header, _receivedStates) &&
		header.PlotId == Plot.Id && header.Epoch == Plot.SimulationEpoch;

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
		var time = _timeline.GetTime(sequence, _clock);

		foreach (var state in _receivedStates)
			if (state.Id < _assemblies.Count && _assemblies[state.Id] is { IsKilled: false } assembly)
				assembly.AddProxyState(
					time, StreamInterval, Origin * new Transform3D(new Basis(state.Rotation), state.Position),
					Origin.Basis * state.LinearVelocity, Origin.Basis * state.AngularVelocity);
	}
}
