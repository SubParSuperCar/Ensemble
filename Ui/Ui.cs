using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Extensions;
using Root.Ui.Impl.Messages;
using Root.Ui.Impl.Services;
using Root.Ui.Impl.ViewModels;
using Serilog;

namespace Root.Ui;

// This class has a very similar issue with AvaloniaLoader.cs. Must not use AutoloadAttribute.
[GlobalClass]
public partial class Ui : AvaloniaControl
{
	// ReSharper disable once MemberCanBePrivate.Global
	public static readonly StringName ProcessTimeMonitor = "Ensemble/Time/UIProcess"; // Lets us see UI time. Useful.

	private double _processTime;

	public override void _Ready()
	{
		// Only show on clients. Not a client? Queue it free.
		if (Main.IsHeadlessServer)
		{
			QueueFree();
			return;
		}

		Console.WriteLine($"Starting {nameof(Ui)}...");
		var stopwatch = Stopwatch.StartNew();

		try
		{
			// We MUST MUST MUST initialize UI here in _Ready BEFORE Main. This is very annoying but must stay.
			GetWindow().SetImeActive(true);

			// For some reason "Loading" doesn't appear even though Main waits a moment to let a frame render
			// before loading the autoloads.
			Control = new TextBlock { Text = "Loading\u2026" };
			base._Ready(); // Call Estragonia's base _Ready, critical.

			stopwatch.Stop();
			Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
				$"Started {nameof(Ui)} in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));

			// Wait to replace the dummy/loading UI with the actual UI.
			// Hacky but necessary solution.
			if (Main.AutoloadsLoaded)
				SwapToRealUi();
			else
				Main.AutoloadsReady += OnAutoloadsReady;
		}
		catch (Exception exception)
		{
			// Same question as AvaloniaLoader.cs.
			if (!Main.AskUser(
					"UI Load Failed",
					"Ensemble UI failed to load:" +
					$"\n\n{exception}\n\nContinue anyway?\n" +
					"Ensemble UI may not appear."))
				Main.FailFast();

			// No UI. Queue it free. It failed.
			QueueFree();
		}
	}

	public override void _ExitTree()
	{
		if (Performance.HasCustomMonitor(ProcessTimeMonitor))
			Performance.RemoveCustomMonitor(ProcessTimeMonitor);
	}

	public override void _Process(double delta)
	{
		// Send messages and measure how long it takes to render and process UI. Super handy.
		var before = Time.GetTicksUsec();

		WeakReferenceMessenger.Default.Send(new ProcessMessage(delta));
		base._Process(delta);

		var after = Time.GetTicksUsec();
		_processTime = (after - before) / (double)TimeSpan.MicrosecondsPerSecond;
	}

	// No base call required here.
	public override void _Input(InputEvent @event) => WeakReferenceMessenger.Default.Send(new InputMessage(@event));

	private void OnAutoloadsReady()
	{
		Main.AutoloadsReady -= OnAutoloadsReady;
		SwapToRealUi();
	}

	// Could maybe use a better method name.
	private void SwapToRealUi()
	{
		Log.Debug("Swapping {Class} to real UI...", nameof(Ui));
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var collection = new ServiceCollection();
			collection.AddServices(); // Registers all the compile-time services from ServiceScan.

			var services = collection.BuildServiceProvider();

			var locator = services.GetRequiredService<ViewLocatorService>();
			Application.Current!.DataTemplates.Add(locator); // We must manually register the View Locator.

			Performance.AddCustomMonitor(
				ProcessTimeMonitor,
				Callable.From(() => _processTime),
				[],
				Performance.MonitorType.Time);

			var viewModel = services.GetRequiredService<MainViewModel>();
			Control = locator.Build(viewModel);

			stopwatch.Stop();
			Log.Debug("Swapped {Class} to real UI in {ElapsedMs:F3} ms.", nameof(Ui),
				stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception exception)
		{
			// Same as before. These messages could also be improved, including in Main.cs.
			if (!Main.AskUser(
					"UI Swap Failed",
					"Ensemble UI failed to swap to the real UI:" +
					$"\n\n{exception}\n\nContinue anyway?\n" +
					"Ensemble UI may not appear as the real UI."))
				Main.FailFast();

			QueueFree();
		}
	}
}
