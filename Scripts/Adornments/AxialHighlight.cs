using Godot;

namespace Root.Scripts.Adornments;

[GlobalClass]
public partial class AxialHighlight : MeshInstance3D
{
	private static readonly Shader HighlightShader =
		GD.Load<Shader>("res://shaders/axial_highlight.gdshader");

	private readonly BoxMesh _box = new();
	private readonly ShaderMaterial _material = new();

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

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float EdgeThickness
	{
		get;
		set
		{
			field = value;
			UpdateEdgeThickness();
		}
	} = 1 / 64f;

	public override void _Ready()
	{
		_material.Shader = HighlightShader;

		Mesh = _box;
		MaterialOverride = _material;

		UpdateMesh();
		UpdateEdgeThickness();
	}

	private void UpdateMesh()
	{
		if (!IsNodeReady())
			return;

		_box.Size = Aabb.Size;
		Position = Aabb.Position + Aabb.Size * 0.5f;

		_material.SetShaderParameter("box_size", Aabb.Size);
	}

	private void UpdateEdgeThickness() => _material.SetShaderParameter("edge_thickness", EdgeThickness);
}
