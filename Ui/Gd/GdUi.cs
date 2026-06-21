using Estragonia;
using Root.Ui.Impl.Views;

namespace Root.Ui.Gd;

public partial class GdUi : AvaloniaControl
{
	public override void _Ready()
	{
		GetWindow().SetImeActive(true);

		Control = new MainView();

		base._Ready();
	}
}
