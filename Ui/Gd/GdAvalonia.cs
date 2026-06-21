using Avalonia;
using Estragonia;
using Godot;
using Root.Ui.Impl;

namespace Root.Ui.Gd;

public partial class GdAvalonia : Node
{
	public override void _Ready()
		=> AppBuilder
			.Configure<App>()
			.UseGodot()
			.SetupWithoutStarting();
}
