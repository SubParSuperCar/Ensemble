using Godot;

namespace EnsembleRoot.Scripts.Adornments;

public abstract partial class HighlightBase : MeshInstance3D
{
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

	protected abstract Shader Shader { get; }

	public override void _Ready()
	{
		Material.Shader = Shader;

		Mesh = _box;
		MaterialOverride = Material;

		UpdateMesh();
		UpdateEdgeThickness();
	}

	private void UpdateMesh()
	{
		if (!IsNodeReady())
			return;

		_box.Size = Aabb.Size;
		Position = Aabb.Position + Aabb.Size / 2;

		Material.SetShaderParameter("box_size", Aabb.Size);
	}

	private void UpdateEdgeThickness() => Material.SetShaderParameter("edge_thickness", EdgeThickness);
}
