using Estragonia;

namespace Root.Ui.Gd;

public partial class GdUi : AvaloniaControl
{
	public override void _Ready()
	{
		GetWindow().SetImeActive(true);

		//Control = new HelloWorldView();

		base._Ready();
	}

	/*public override void _Process(double delta)
	{
		base._Process(delta);
	}*/
}
