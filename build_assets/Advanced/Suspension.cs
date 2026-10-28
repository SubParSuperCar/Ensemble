using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.BuildAssets.Advanced;

/// <summary>
///     A spring between two plates, the top a piece of its own, whose stiffness, damping, rest length, and longest
///     length come from the instance's properties, clamped so the plates stay apart and within reach. Lengths are
///     end to end, in grid units, and a longest length of 0 leaves only the reach. While live, the coil between the
///     plates stretches with them.
/// </summary>
public partial class Suspension : AssetHandle
{
	private const string StiffnessKey = "stiffness";
	private const string DampingKey = "damping";
	private const string TargetLengthKey = "targetLength";
	private const string MaxLengthKey = "maxLength";

	private const float MaxStiffness = 100_000f;
	private const float MaxDamping = 10_000f;

	// The plates' closest approach, so they never strike each other, and the longest the suspension may get, end to end
	private const float MinGap = 0.1f;
	private const float MaxReach = 8 * PlotHandle.GridToWorldScale;

	private static readonly StringName HelixLengthParameter = "helix_length";

	private float _coilLength;

	[Export] public Generic6DofJoint3D Spring { get; set; } = null!;

	/// <summary>The coil, standing on the bottom plate, and drawn by the helix shader.</summary>
	[Export]
	public MeshInstance3D Coil { get; set; } = null!;

	/// <summary>Where the coil meets the top plate.</summary>
	[Export]
	public Node3D CoilEnd { get; set; } = null!;

	// Built and previewed suspensions show the coil as authored
	public override void _Ready() => SetProcess(IsLive);

	public override void _Process(double delta)
	{
		// Interpolated, as the plates are drawn
		var length = MeasureCoil(Coil.GetGlobalTransformInterpolated(), CoilEnd.GetGlobalTransformInterpolated());

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (length == _coilLength)
			return;

		_coilLength = length;
		Coil.SetInstanceShaderParameter(HelixLengthParameter, length);
	}

	// The joint measures the top plate's travel from where it was built, so it takes each length less the built one
	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		// Nothing has moved yet, so this is the gap as built
		var builtGap = MeasureCoil(Coil.GlobalTransform, CoilEnd.GlobalTransform);
		var builtLength = BoundaryAabb.Size.Y;
		var plates = builtLength - builtGap;
		var minLength = plates + MinGap;

		var maxLength = GetNumberProperty(MaxLengthKey, float.MinValue, float.MaxValue) is 0
			? MaxReach
			: GetLengthProperty(MaxLengthKey, minLength, MaxReach);
		var targetLength = GetLengthProperty(TargetLengthKey, minLength, maxLength);

		var stiffness = GetNumberProperty(StiffnessKey, 0, MaxStiffness);
		var damping = GetNumberProperty(DampingKey, 0, MaxDamping);

		Spring.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearLimit, true);
		Spring.SetParamY(Generic6DofJoint3D.Param.LinearLowerLimit, minLength - builtLength);
		Spring.SetParamY(Generic6DofJoint3D.Param.LinearUpperLimit, maxLength - builtLength);

		Spring.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearSpring, true);
		Spring.SetParamY(Generic6DofJoint3D.Param.LinearSpringEquilibriumPoint, targetLength - builtLength);
		Spring.SetParamY(Generic6DofJoint3D.Param.LinearSpringStiffness, stiffness);
		Spring.SetParamY(Generic6DofJoint3D.Param.LinearSpringDamping, damping);
	}

	private static float MeasureCoil(Transform3D coil, Transform3D end) => (coil.AffineInverse() * end.Origin).Y;
}
