using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Extensions;
using Root.Ui.Impl.Messages;
using Root.Ui.Impl.Services;
using Root.Ui.Impl.ViewModels;
using Serilog;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace Root.Ui;

[GlobalClass]
public partial class Ui : AvaloniaControl
{
	public static readonly StringName ProcessTimeMonitor = "Ensemble/Time/UIProcess";

	private double _processTime;

	public override void _Ready()
	{
		if (Main.IsHeadlessServer)
		{
			QueueFree();
			return;
		}

		Console.WriteLine($"Starting {nameof(Ui)}...");
		var stopwatch = Stopwatch.StartNew();

		try
		{
			GetWindow().SetImeActive(true);

			Control = new TextBlock
			{
				Text = "Loading Autoloads\u2026",
				FontSize = 48,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextOptions.SetTextRenderingMode(Control, TextRenderingMode.Antialias);

			base._Ready();

			stopwatch.Stop();
			Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
				$"Started {nameof(Ui)} in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));

			if (Main.AutoloadsLoaded)
				SwapToRealUi();
			else
				Main.AutoloadsReady += OnAutoloadsReady;
		}
		catch (Exception exception)
		{
			if (!Main.AskUser(
					"UI Load Failed",
					Main.FormatFailureMessage(
						"Ensemble UI failed to load", exception, "Ensemble UI may not appear.")))
				Main.FailFast();

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
		var before = Time.GetTicksUsec();

		WeakReferenceMessenger.Default.Send(new ProcessMessage(delta));
		base._Process(delta);

		var after = Time.GetTicksUsec();
		_processTime = (after - before) / (double)TimeSpan.MicrosecondsPerSecond;
	}

	public override void _Input(InputEvent @event) => WeakReferenceMessenger.Default.Send(new InputMessage(@event));

	private void OnAutoloadsReady()
	{
		Main.AutoloadsReady -= OnAutoloadsReady;
		SwapToRealUi();
	}

	private void SwapToRealUi()
	{
		Log.Debug("Swapping {Class} to real UI...", nameof(Ui));
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var collection = new ServiceCollection();
			collection.AddServices();

			var services = collection.BuildServiceProvider();

			var locator = services.GetRequiredService<ViewLocatorService>();
			Application.Current!.DataTemplates.Add(locator);

			Performance.AddCustomMonitor(
				ProcessTimeMonitor,
				Callable.From(() => _processTime),
				[],
				Performance.MonitorType.Time);

			var viewModel = services.GetRequiredService<MainViewModel>();
			Control = locator.Build(viewModel);

			stopwatch.Stop();
			Log.Debug("Swapped {Class} to real UI in {ElapsedMs:F3} ms.",
				nameof(Ui), stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception exception)
		{
			if (!Main.AskUser(
					"UI Swap Failed",
					Main.FormatFailureMessage(
						"Ensemble UI failed to swap to the real UI",
						exception,
						"Ensemble UI may not appear as the real UI.")))
				Main.FailFast();

			QueueFree();
		}
	}
}
