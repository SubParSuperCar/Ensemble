using Godot;

namespace EnsembleRoot.BuildAssets.Advanced.Hinges;

/// <summary>
///     Spins its head while driven; a negative speed reverses it. Otherwise it idles: free to coast, unless it brakes.
///     Its torque is constant, or falls off in a straight line to nothing at full speed, like an electric motor's.
/// </summary>
public partial class Motor : DrivenHinge
{
	private const string BrakesWhenIdleKey = "brakesWhenIdle";
	private const string TorqueFalloffKey = "torqueFalloff";

	private bool _brakesWhenIdle;
	private bool _hasTorqueFalloff;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		if (!IsLive)
			return;

		_brakesWhenIdle = Instance.Properties.GetValue(BrakesWhenIdleKey).AsBool();
		_hasTorqueFalloff = Instance.Properties.GetValue(TorqueFalloffKey).AsBool() && Speed is not 0;

		ApplyDirection();
	}

	protected override void Drive(bool isDirectionChanged, double delta)
	{
		if (isDirectionChanged)
			ApplyDirection();

		SetTorque(GetTorque(), delta);
	}

	private void ApplyDirection()
	{
		Hinge.SetFlag(HingeJoint3D.Flag.EnableMotor, Direction is not 0 || _brakesWhenIdle);
		SetVelocity(Direction * Speed);
	}

	private float GetTorque()
	{
		if (!_hasTorqueFalloff || Direction is 0)
			return Torque;

		var spin = (Head.Assembly!.AngularVelocity - Base.Assembly!.AngularVelocity).Dot(Hinge.GlobalBasis.Z);
		return Torque * Math.Clamp(1 - spin / (Direction * Speed), 0, 1);
	}
}
