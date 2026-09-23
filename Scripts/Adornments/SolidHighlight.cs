using Godot;

namespace Root.Scripts.Adornments;

[GlobalClass]
public partial class SolidHighlight : HighlightBase
{
	private static readonly Shader HighlightShader = GD.Load<Shader>(ShadersDir + "solid_highlight.gdshader");

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

	private void UpdateTint() => Material.SetShaderParameter("tint", new Vector3(Tint.R, Tint.G, Tint.B));
}
