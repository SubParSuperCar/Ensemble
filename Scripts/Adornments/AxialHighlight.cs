using Godot;

namespace Root.Scripts.Adornments;

[GlobalClass]
public partial class AxialHighlight : HighlightBase
{
	private static readonly Shader HighlightShader = GD.Load<Shader>(ShadersDir + "axial_highlight.gdshader");

	protected override Shader Shader => HighlightShader;
}
