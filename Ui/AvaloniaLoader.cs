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

			try
			{
				AppBuilder
					.Configure<App>()
					.UseGodot()
					.WithJetBrainsMonoFont()
					.SetupWithoutStarting();

				stopwatch.Stop();
				Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
					$"Loaded Avalonia UI in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));
			}
			catch (Exception exception)
			{
				if (!Main.AskUser(
						"Avalonia UI Load Failed",
						Main.FormatFailureMessage(
							"Avalonia UI failed to load", exception, "Ensemble UI may not appear.")))
					Main.FailFast();
			}
		}

		QueueFree();
	}
}
