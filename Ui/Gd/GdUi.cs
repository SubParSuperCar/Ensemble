using Estragonia;
using Godot;
using Root.Ui.Impl.Views;
using Dispatcher = Root.Ui.Impl.Dispatcher;

namespace Root.Ui.Gd;

public partial class GdUi : AvaloniaControl
{
	public override void _Ready()
	{
		GetWindow().SetImeActive(true);

		Control = new MainView();

		base._Ready();
	}

	public override void _Process(double delta)
	{
		Dispatcher.RaiseProcess(delta);

		base._Process(delta);
	}

	public override void _Input(InputEvent @event) => Dispatcher.RaiseInput(@event);
}
