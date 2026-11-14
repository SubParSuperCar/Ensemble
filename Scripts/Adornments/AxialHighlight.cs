using Godot;

namespace EnsembleRoot.Scripts.Adornments;

[GlobalClass]
public partial class AxialHighlight : HighlightBase
{
	private static readonly StringName AxialParameter = "axial";

	public AxialHighlight()
	{
		FillOpacity = 1 / 2f;
		SetParameter(AxialParameter, true);
	}
}
