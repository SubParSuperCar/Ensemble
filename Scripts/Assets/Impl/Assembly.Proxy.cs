using EnsembleRoot.Common.Utils;
using Godot;

namespace EnsembleRoot.Scripts.Assets;

public partial class Assembly
{
	private const float ProxySmoothingRate = 16f;
	private const float ProxySnapDistance = 8f;
	private const double MaxProxyExtrapolation = 1 / 4d;

	private Vector3 _proxyAngularVelocity;
	private Vector3 _proxyLinearVelocity;
	private Transform3D? _proxyTarget;
	private double _proxyTargetAge;

	// The proxy's own copy of where it was last put, so a settled proxy costs no interop
	private Transform3D _proxyTransform;

	/// <summary>Sets a proxy's latest streamed state, in global space; the first one is jumped to.</summary>
	internal void SetProxyTarget(Transform3D target, Vector3 linearVelocity, Vector3 angularVelocity)
	{
		if (_proxyTarget is null)
			JumpProxy(target);

		_proxyTarget = target;
		_proxyLinearVelocity = linearVelocity;
		_proxyAngularVelocity = angularVelocity;
		_proxyTargetAge = 0;
	}

	// Extrapolates briefly with the streamed velocities, so late packets don't stall it, then eases toward that
	internal void UpdateProxy(double delta)
	{
		if (_proxyTarget is not { } target)
			return;

		var isMoving = _proxyLinearVelocity != Vector3.Zero || _proxyAngularVelocity != Vector3.Zero;

		if (!isMoving && _proxyTransform.IsEqualApprox(target))
			return;

		_proxyTargetAge = Math.Min(_proxyTargetAge + delta, MaxProxyExtrapolation);

		var age = (float)_proxyTargetAge;
		var spin = _proxyAngularVelocity * age;
		var basis = spin.IsZeroApprox() ? target.Basis : new Basis(spin.Normalized(), spin.Length()) * target.Basis;
		var extrapolated = new Transform3D(basis, target.Origin + _proxyLinearVelocity * age);

		if (_proxyTransform.Origin.DistanceTo(extrapolated.Origin) > ProxySnapDistance)
		{
			JumpProxy(extrapolated);
			return;
		}

		var weight = Smoothing.GetWeight(ProxySmoothingRate, delta);
		var rotation = _proxyTransform.Basis.GetRotationQuaternion()
			.Slerp(extrapolated.Basis.GetRotationQuaternion(), weight);
		var origin = _proxyTransform.Origin.Lerp(extrapolated.Origin, weight);

		GlobalTransform = _proxyTransform = new Transform3D(new Basis(rotation), origin);
	}

	// A kinematic body moved by a transform shoves what it touched on the way, so jumps leave the space for the move
	private void JumpProxy(Transform3D transform)
	{
		var rid = GetRid();
		var space = PhysicsServer3D.BodyGetSpace(rid);

		PhysicsServer3D.BodySetSpace(rid, default);
		GlobalTransform = _proxyTransform = transform;
		ForceUpdateTransform();
		PhysicsServer3D.BodySetSpace(rid, space);
		ResetPhysicsInterpolation();
	}
}
