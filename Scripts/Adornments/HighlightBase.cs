using Godot;

namespace EnsembleRoot.Scripts.Adornments;

public abstract partial class HighlightBase : MeshInstance3D
{
	private static readonly StringName BoxSizeParameter = "box_size";
	private static readonly StringName EdgeThicknessParameter = "edge_thickness";
	private static readonly StringName FaceAlphaParameter = "face_alpha";

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
	} = 0.1f;

	protected abstract Shader Shader { get; }

	public override void _Ready()
	{
		Material.Shader = Shader;
		Material.RenderPriority = (int)Godot.Material.RenderPriorityMax;

		Mesh = _box;
		MaterialOverride = Material;

		UpdateMesh();
		UpdateEdgeThickness();
		UpdateFaceAlpha();
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
}
