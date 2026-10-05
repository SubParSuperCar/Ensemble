using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Rendering.Composition;
using EnsembleRoot.Common.Interop;
using EnsembleRoot.Ui.Impl;
using Estragonia;
using Fonts.Avalonia.JetBrainsMono;
using Godot;

namespace EnsembleRoot.Ui;

public partial class AvaloniaLoader : Node
{
	public override void _Ready()
	{
		if (!Main.IsHeadlessServer)
			Load();

		QueueFree();
	}

	private static void Load()
	{
		Console.WriteLine("Configuring Avalonia UI...");
		var stopwatch = Stopwatch.StartNew();

		try
		{
			HarfBuzzIsolation.Apply();

			AppBuilder
				.Configure<App>()
				.UseGodot()
				.WithJetBrainsMonoFont()
				.With(new SkiaOptions
				{
					// Up from Avalonia's 28.125 MiB default; one 4K RGBA8 surface alone is ~31.6 MiB
					MaxGpuResourceSizeBytes = 32 * 1024 * 1024
				})
				.With(new CompositionOptions
				{
					// Enabling this reduces FPS by about 13% in some heavy-UI scenarios
					UseRegionDirtyRectClipping = false
				})
				.With(new GodotPlatformOptions { ProcessInterval = Ui.ProcessInterval })
				.LogToTrace()
				.SetupWithGodot();

			stopwatch.Stop();
			Console.WriteLine(string.Create(
				CultureInfo.InvariantCulture,
				$"Configured Avalonia UI in {stopwatch.Elapsed.TotalMilliseconds:F3} ms"));
		}
		catch (Exception exception)
		{
			if (
				!Main.AskUser(
					"Avalonia UI Config Failed",
					Main.FormatFailureMessage(
						"Avalonia UI failed to configure",
						exception,
						"Ensemble UI may not appear.")))
				Main.FailFast(exception);
		}
	}
}
