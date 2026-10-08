using Godot;

namespace EnsembleRoot.Scripts.Adornments;

[GlobalClass]
public partial class SolidHighlight : HighlightBase
{
	private static readonly Shader HighlightShader = GD.Load<Shader>(ShadersDir + "solid_highlight.gdshader");
	private static readonly StringName TintParameter = "tint";

	[Export]
	public Color Tint
	{
		get;
		set
		{
			field = value;
			UpdateTint();
		}
	} = Colors.White;

	protected override Shader Shader => HighlightShader;

	public override void _Ready()
	{
		base._Ready();
		UpdateTint();
	}

	private void UpdateTint() => Material.SetShaderParameter(TintParameter, new Vector3(Tint.R, Tint.G, Tint.B));
}
