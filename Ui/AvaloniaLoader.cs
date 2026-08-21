using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Estragonia;
using Fonts.Avalonia.JetBrainsMono;
using Godot;
using Root.Ui.Impl;

namespace Root.Ui;

public partial class AvaloniaLoader : Node
{
	public override void _Ready()
	{
		if (!Main.IsHeadlessServer)
		{
			Console.WriteLine("Loading Avalonia UI...");
			var stopwatch = Stopwatch.StartNew();

			AppBuilder
				.Configure<App>()
				.UseGodot()
				.WithJetBrainsMonoFont()
				.SetupWithoutStarting();

			stopwatch.Stop();
			Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
				$"Loaded Avalonia UI in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));
		}

		QueueFree();
	}
}
