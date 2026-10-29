using Godot;

namespace EnsembleRoot.Scripts.Adornments;

public abstract partial class HighlightBase : MeshInstance3D
{
	private static readonly StringName BoxSizeParameter = "box_size";
	private static readonly StringName EdgeThicknessParameter = "edge_thickness";
	private static readonly StringName FaceAlphaParameter = "face_alpha";
	private static readonly StringName OpacityParameter = "opacity";
	private static readonly StringName MarchingAntsParameter = "marching_ants";
	private static readonly StringName AntSpacingParameter = "ant_spacing";
	private static readonly StringName AntSpeedParameter = "ant_speed";

	private readonly BoxMesh _box = new();

	protected ShaderMaterial Material { get; } = new();

	[Export]
	public Aabb Aabb
	{
		get;
		set
		{
			field = value;
			UpdateMesh();
		}
	}

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float EdgeThickness
	{
		get;
		set
		{
			field = value;
			UpdateEdgeThickness();
		}
	} = 1 / 64f;

	[Export(PropertyHint.Range, "0,1")]
	public float FaceAlpha
	{
		get;
		set
		{
			field = value;
			UpdateFaceAlpha();
		}
	} = 1 / 8f;

	[Export(PropertyHint.Range, "0,1")]
	public float Opacity
	{
		get;
		set
		{
			field = value;
			UpdateOpacity();
		}
	} = 1f;

	/// <summary>Whether the edges are dashed, with the dashes marching toward each axis's positive end.</summary>
	[Export]
	public bool MarchingAnts
	{
		get;
		set
		{
			field = value;
			UpdateMarchingAnts();
		}
	}

	/// <summary>The length of each dash, and of each gap between them.</summary>
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float AntSpacing
	{
		get;
		set
		{
			field = value;
			UpdateAntSpacing();
		}
	} = 0.05f;

	[Export(PropertyHint.Range, "0,0,or_less,or_greater,hide_slider,suffix:m/s")]
	public float AntSpeed
	{
		get;
		set
		{
			field = value;
			UpdateAntSpeed();
		}
	} = 0.1f;

	protected abstract Shader Shader { get; }

	public override void _Ready()
	{
		// Highlights are placed between physics ticks, so interpolating would drag them from where they last were
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

		Material.Shader = Shader;
		Material.RenderPriority = (int)Godot.Material.RenderPriorityMax;

		Mesh = _box;
		MaterialOverride = Material;

		UpdateMesh();
		UpdateEdgeThickness();
		UpdateFaceAlpha();
		UpdateOpacity();
		UpdateMarchingAnts();
		UpdateAntSpacing();
		UpdateAntSpeed();
	}

	private void UpdateMesh()
	{
		if (!IsNodeReady())
			return;

		_box.Size = Aabb.Size;
		Position = Aabb.Position + Aabb.Size / 2;

		Material.SetShaderParameter(BoxSizeParameter, Aabb.Size);
	}

	private void UpdateEdgeThickness() => Material.SetShaderParameter(EdgeThicknessParameter, EdgeThickness);
	private void UpdateFaceAlpha() => Material.SetShaderParameter(FaceAlphaParameter, FaceAlpha);
	private void UpdateOpacity() => Material.SetShaderParameter(OpacityParameter, Opacity);
	private void UpdateMarchingAnts() => Material.SetShaderParameter(MarchingAntsParameter, MarchingAnts);
	private void UpdateAntSpacing() => Material.SetShaderParameter(AntSpacingParameter, AntSpacing);
	private void UpdateAntSpeed() => Material.SetShaderParameter(AntSpeedParameter, AntSpeed);
}
