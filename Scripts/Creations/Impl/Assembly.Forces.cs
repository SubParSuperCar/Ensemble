using Godot;

namespace EnsembleRoot.Scripts.Creations;

public partial class Assembly
{
	private Vector3 _force;
	private BodyState? _state;
	private Vector3 _torque;

	/// <summary>
	///     The body's motion this physics tick, read from the engine at most once per tick, so assets that ask for it
	///     cost one read between them. Meant for simulation ticks, which run before the tick's physics step.
	/// </summary>
	public BodyState State => _state ??= new BodyState(GlobalTransform, LinearVelocity, AngularVelocity);

	/// <summary>Pushes the body this physics tick with a force at a point, both in world space.</summary>
	/// <remarks>
	///     Forces are summed in C# and applied once per body per tick, so a thousand wings make a thousand additions,
	///     not a thousand engine calls.
	/// </remarks>
	public void AddForce(Vector3 force, Vector3 position)
	{
		_force += force;
		_torque += (position - State.Transform.Origin).Cross(force);
	}

	/// <summary>Turns the body this physics tick with a torque in world space.</summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public void AddTorque(Vector3 torque) => _torque += torque;

	// After every asset has ticked. Only sums that aren't zero are applied, as applying one wakes the body, and an idle
	// creation should fall asleep.
	internal void EndTick()
	{
		_state = null;

		if (_force != Vector3.Zero)
			ApplyCentralForce(_force);

		if (_torque != Vector3.Zero)
			ApplyTorque(_torque);

		_force = _torque = Vector3.Zero;
	}
}
