using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Estragonia;
using Fonts.Avalonia.JetBrainsMono;
using Godot;
using Root.Ui.Impl;

namespace Root.Ui;

// We CANNOT use the autoload system. I tried and had terrible bugs.
// We MUST run this first as a native Godot autoload. Nothing else works. Estragonia/Avalonia quirks.
public partial class AvaloniaLoader : Node
{
	public override void _Ready()
	{
		// Only run this when we're not a headless server for obvious reasons.
		if (!Main.IsHeadlessServer)
		{
			Console.WriteLine("Loading Avalonia UI...");
			var stopwatch = Stopwatch.StartNew();

			// Wrap this in a try-catch block in-case UI fails (missing DLL, etc.)
			// It has happened before that missing DLLs in the export release dir causes Avalonia to fail.
			// This at least prevents it from taking everything down. Handle it gracefully. Same for Ui.cs.
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
				// It would be nice to add a better method to Main for displaying these messages.
				if (!Main.AskUser(
						"Avalonia UI Load Failed",
						"Avalonia UI failed to load:" +
						$"\n\n{exception}\n\nContinue anyway?\n" +
						"Ensemble UI may not appear."))
					Main.FailFast();
			}
		}

		// Job's done. No need to stick around. Queue to free up memory.
		QueueFree();
	}
}
