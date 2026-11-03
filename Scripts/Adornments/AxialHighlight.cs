using Godot;

namespace EnsembleRoot.Scripts.Adornments;

[GlobalClass]
public partial class AxialHighlight : HighlightBase
{
	private static readonly Shader HighlightShader = GD.Load<Shader>(ShadersDir + "axial_highlight.gdshader");

	public AxialHighlight()
	{
		FaceAlpha = 3 / 16f;
	}

	protected override Shader Shader => HighlightShader;
}
