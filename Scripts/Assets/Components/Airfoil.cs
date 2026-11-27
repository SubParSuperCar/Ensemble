using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Components;

/// <summary>
///     A surface the air pushes on while live: lift across the airflow, which grows with the angle of attack until the
///     surface stalls and then falls away, and drag along it, both with the square of the airspeed. They act at
///     <see cref="Marker" />, whose Y axis is the surface's normal, scaled by the instance's lift and drag scales.
/// </summary>
[GlobalClass]
public partial class Airfoil : AssetComponent, ISimulationTicked
{
	private const string LiftScaleKey = "liftScale";
	private const string DragScaleKey = "dragScale";

	private const float MaxScale = 16f;

	// Kilograms per cubic meter, at sea level
	private const float AirDensity = 1.225f;

	// Skin friction, and the aspect ratio of the wing a surface is assumed to be part of, for its induced drag
	private const float ParasiteDrag = 1 / 50f;
	private const float AspectRatio = 6f;

	// Meters per second: slower air pushes on nothing, and faster air pushes as hard as this, so a light surface's
	// drag can't overshoot within a tick and fling it back
	private const float MinAirspeed = 1 / 64f;
	private const float MaxAirspeed = 256f;

	// Where attached flow's lift gives way to a flat plate's, and over how many radians past it
	private static readonly float StallAngle = float.DegreesToRadians(15f);
	private static readonly float StallBlend = float.DegreesToRadians(5f);

	private float _area;
	private Assembly _assembly = null!;
	private float _dragScale;
	private float _liftScale;
	private Transform3D _marker;

	/// <summary>
	///     Where lift and drag act, with its Y axis normal to the surface: a child of the handle or of a piece.
	/// </summary>
	[Export]
	public Node3D Marker { get; set; } = null!;

	/// <summary>Square meters, or, if 0, the handle's bounds across <see cref="Marker" />'s normal.</summary>
	[Export(PropertyHint.Range, "0,0,0.001,or_greater,hide_slider,suffix:m²")]
	public float Area { get; set; }

	public void Tick(in SimulationTick tick)
	{
		var state = _assembly.State;
		var position = state.Transform * _marker.Origin;
		var normal = (state.Transform.Basis * _marker.Basis.Y).Normalized();

		// The air's motion past the surface
		var air = -state.GetVelocityAt(position);
		var speed = air.Length();

		if (speed < MinAirspeed)
			return;

		var flow = air / speed;
		var sine = Math.Clamp(flow.Dot(normal), -1, 1);
		var angle = MathF.Asin(sine);
		var pressure = AirDensity / 2 * MathF.Pow(MathF.Min(speed, MaxAirspeed), 2) * _area;

		// Positive angles meet the surface from below its normal, and lift it along the normal's part across the flow
		var lift = normal - sine * flow;
		var force = flow * pressure * GetDragCoefficient(angle) * _dragScale;

		if (lift.LengthSquared() > 0)
			force += lift.Normalized() * pressure * GetLiftCoefficient(angle) * _liftScale;

		_assembly.AddForce(force, position);
	}

	protected override void OnInitialized()
	{
		if (!Handle.IsLive)
			return;

		_assembly = (Marker.GetParent() as AssetPiece)?.Assembly ?? Handle.Assembly!;
		_marker = _assembly.GlobalTransform.AffineInverse() * Marker.GlobalTransform;
		_area = Area > 0 ? Area : MeasureArea();

		_liftScale = Handle.GetNumberProperty(LiftScaleKey, 0, MaxScale);
		_dragScale = Handle.GetNumberProperty(DragScaleKey, 0, MaxScale);
	}

	// Attached flow's, a thin airfoil's, up to the stall, and past it a flat plate's
	private static float GetLiftCoefficient(float angle) =>
		float.Lerp(float.Tau * angle, MathF.Sin(2 * angle), GetSeparation(angle));

	// Skin friction, the drag of lift while the flow is attached, and a flat plate's drag from the air it turns
	private static float GetDragCoefficient(float angle)
	{
		var attachedLift = float.Tau * angle;
		var induced = attachedLift * attachedLift / (float.Pi * AspectRatio);

		return ParasiteDrag + (1 - GetSeparation(angle)) * induced + 2 * MathF.Pow(MathF.Sin(angle), 2);
	}

	// From 0 with the flow attached to 1 with it separated
	private static float GetSeparation(float angle) => Math.Clamp((MathF.Abs(angle) - StallAngle) / StallBlend, 0, 1);

	// The handle's bounds as seen along the marker's normal
	private float MeasureArea()
	{
		var bounds = Handle.BoundaryAabb;
		var toMarker = (Handle.GlobalTransform.AffineInverse() * Marker.GlobalTransform).AffineInverse();
		var projected = Enumerable.Range(0, 8)
			.Select(corner => toMarker * bounds.GetEndpoint(corner))
			.Aggregate(new Aabb(toMarker * bounds.Position, Vector3.Zero), static (aabb, point) => aabb.Expand(point));

		return projected.Size.X * projected.Size.Z;
	}
}
