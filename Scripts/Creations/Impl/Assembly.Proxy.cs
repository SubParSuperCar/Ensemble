using System.Runtime.InteropServices;
using Godot;

namespace EnsembleRoot.Scripts.Creations;

public partial class Assembly
{
	private const int ProxyBufferSize = 4;
	private const float ProxySnapDistance = 8f;
	private const double MaxProxyExtrapolation = 1 / 10d;

	// A velocity change beyond twice gravity's means the body hit something between the two states, so a curve would
	// overshoot into it
	private const float MaxCurvedAcceleration = 2 * 9.8f;

	private readonly ProxyState[] _proxyStates = new ProxyState[ProxyBufferSize];
	private int _proxyStateCount;

	/// <summary>Where a proxy was last put, known without asking Godot, so a settled proxy costs no interop.</summary>
	internal Transform3D ProxyTransform { get; private set; }

	/// <summary>
	///     Buffers a proxy's streamed state, in global space, stamped with the local time it describes; the first one
	///     is jumped to. A body that rested long before this state holds its pose until <paramref name="interval" />
	///     before it, rather than drifting toward it all along.
	/// </summary>
	internal void AddProxyState(
		double time, double interval, Transform3D transform, Vector3 linearVelocity, Vector3 angularVelocity)
	{
		if (_proxyStateCount is 0)
			JumpProxy(transform);
		else
		{
			var latest = _proxyStates[_proxyStateCount - 1];

			if (time <= latest.Time)
				return;

			// A held proxy's state has no moment of its own, so it takes this one; any other was shown at its moment, so
			// the hold is a copy, as retiming it would drag moments already shown back toward the state before it
			if (time - latest.Time > 2 * interval && latest.IsAtRest)
			{
				if (double.IsNegativeInfinity(latest.Time))
					--_proxyStateCount;

				PushProxyState(latest with { Time = time - interval });
			}
		}

		PushProxyState(new ProxyState(time, transform, linearVelocity, angularVelocity));
	}

	/// <summary>Forgets the buffered states' times (another simulator's clock), keeping the proxy in place.</summary>
	internal void ResetProxyTimeline()
	{
		if (!IsSimulated && _proxyStateCount is not 0)
			HoldProxy();
	}

	/// <summary>
	///     Shows the proxy as it was at <paramref name="time" />, between the buffered states around it, or briefly
	///     extrapolated past the latest one when the next is late.
	/// </summary>
	internal void UpdateProxy(double time)
	{
		if (_proxyStateCount is 0)
			return;

		ref readonly var latest = ref _proxyStates[_proxyStateCount - 1];
		Transform3D transform;

		if (time >= latest.Time)
			transform = Extrapolate(latest, Math.Min(time - latest.Time, MaxProxyExtrapolation));
		else
		{
			var index = _proxyStateCount - 2;

			while (index >= 0 && _proxyStates[index].Time > time)
				index--;

			transform = index < 0
				? _proxyStates[0].Transform
				: Interpolate(_proxyStates[index], _proxyStates[index + 1], time);
		}

		// A kinematic body moves toward its new transform, so one that isn't finite would never recover
		if (transform == ProxyTransform || !transform.IsFinite())
			return;

		if (transform.Origin.DistanceTo(ProxyTransform.Origin) > ProxySnapDistance)
			JumpProxy(transform);
		else
			GlobalTransform = ProxyTransform = transform;
	}

	private static Transform3D Extrapolate(in ProxyState state, double age) =>
		new(
			new Basis(Spin(state.AngularVelocity, (float)age) * state.Transform.Basis.GetRotationQuaternion()),
			state.Transform.Origin + state.LinearVelocity * (float)age);

	// A cubic Hermite curve through both positions and velocities, unless something was hit in between; rotations
	// spin each state toward the moment by its own angular velocity, so fast spins don't take the shortest way round
	private static Transform3D Interpolate(in ProxyState from, in ProxyState to, double time)
	{
		var span = (float)(to.Time - from.Time);
		var t = (float)((time - from.Time) / span);
		var p0 = from.Transform.Origin;
		var p1 = to.Transform.Origin;

		Vector3 origin;

		if ((to.LinearVelocity - from.LinearVelocity).Length() > MaxCurvedAcceleration * span)
			origin = p0.Lerp(p1, t);
		else
		{
			var t2 = t * t;
			var t3 = t2 * t;

			origin =
				(2 * t3 - 3 * t2 + 1) * p0 + (t3 - 2 * t2 + t) * span * from.LinearVelocity +
				(3 * t2 - 2 * t3) * p1 + (t3 - t2) * span * to.LinearVelocity;
		}

		var start = Spin(from.AngularVelocity, t * span) * from.Transform.Basis.GetRotationQuaternion();
		var end = Spin(to.AngularVelocity, (t - 1) * span) * to.Transform.Basis.GetRotationQuaternion();

		return new Transform3D(new Basis(start.Slerp(end, t)), origin);
	}

	private static Quaternion Spin(Vector3 angularVelocity, float seconds)
	{
		var speed = angularVelocity.Length();
		return speed is 0 || seconds is 0
			? Quaternion.Identity
			: new Quaternion(angularVelocity / speed, speed * seconds);
	}

	/// <summary>
	///     Starts simulating the body from its latest received state, brought forward to <paramref name="time" />
	///     (asleep if it was at rest, so a hand-off doesn't wake a whole creation), or freezes it into a proxy where it
	///     is, holding still until the new simulator's states arrive.
	/// </summary>
	internal void HandOff(bool isSimulated, double time)
	{
		if (isSimulated == IsSimulated)
			return;

		if (!isSimulated)
		{
			IsSimulated = false;
			HoldProxy();

			return;
		}

		if (_proxyStateCount is 0)
		{
			IsSimulated = true;
			return;
		}

		var latest = _proxyStates[_proxyStateCount - 1];
		_proxyStateCount = 0;

		JumpProxy(Extrapolate(latest, Math.Clamp(time - latest.Time, 0, MaxProxyExtrapolation)));
		IsSimulated = true;

		LinearVelocity = latest.LinearVelocity;
		AngularVelocity = latest.AngularVelocity;
		Sleeping = latest.IsAtRest;
	}

	private void PushProxyState(in ProxyState state)
	{
		if (_proxyStateCount == ProxyBufferSize)
			Array.Copy(_proxyStates, 1, _proxyStates, 0, --_proxyStateCount);

		_proxyStates[_proxyStateCount++] = state;
	}

	// The proxy holds where it was last placed, and its timeline starts over with the next received state
	private void HoldProxy()
	{
		_proxyStates[0] = new ProxyState(double.NegativeInfinity, ProxyTransform, Vector3.Zero, Vector3.Zero);
		_proxyStateCount = 1;
	}

	// A kinematic body moved by a transform shoves what it touched on the way, so jumps leave the space for the move
	private void JumpProxy(Transform3D transform)
	{
		var rid = GetRid();
		var space = PhysicsServer3D.BodyGetSpace(rid);

		PhysicsServer3D.BodySetSpace(rid, default);
		GlobalTransform = ProxyTransform = transform;
		ForceUpdateTransform();
		PhysicsServer3D.BodySetSpace(rid, space);
		ResetPhysicsInterpolation();
	}

	[StructLayout(LayoutKind.Auto)]
	private readonly record struct ProxyState(
		double Time,
		Transform3D Transform,
		Vector3 LinearVelocity,
		Vector3 AngularVelocity)
	{
		public bool IsAtRest => LinearVelocity == Vector3.Zero && AngularVelocity == Vector3.Zero;
	}
}
