using Godot;

namespace EnsembleRoot.Scripts.Adornments;

[GlobalClass]
public partial class SolidHighlight : HighlightBase
{
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

	public override void _Ready()
	{
		base._Ready();
		UpdateTint();
	}

	private void UpdateTint() => SetParameter(TintParameter, new Vector3(Tint.R, Tint.G, Tint.B));
}
