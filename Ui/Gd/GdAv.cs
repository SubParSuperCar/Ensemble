using Avalonia;
using Estragonia;
using Fonts.Avalonia.JetBrainsMono;
using Godot;
using Root.Ui.Impl;

namespace Root.Ui.Gd;

public partial class GdAv : Node
{
	public override void _Ready()
		=> AppBuilder
			.Configure<App>()
			.UseGodot()
			.WithJetBrainsMonoFont()
			.SetupWithoutStarting();
}
