using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Extensions;
using Root.Ui.Impl.Messages;
using Root.Ui.Impl.Services;
using Root.Ui.Impl.ViewModels;
using Serilog;
using Dispatcher = Avalonia.Threading.Dispatcher;
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

		Dispatcher.UIThread.UnhandledException += OnAvaloniaUnhandledException;

		Console.WriteLine($"Starting {nameof(Ui)} (loading screen)...");
		var stopwatch = Stopwatch.StartNew();

		try
		{
			GetWindow().SetImeActive(true);

			Control = new TextBlock
			{
				Text = "Loading Autoloads\u2026",
				FontFamily = new FontFamily("sans-serif"),
				FontWeight = FontWeight.Regular,
				FontSize = 48,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextOptions.SetTextRenderingMode(Control, TextRenderingMode.Antialias);

			base._Ready();

			stopwatch.Stop();
			Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
				$"Started {nameof(Ui)} in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));

			WeakReferenceMessenger.Default.Register<SetUiRenderScaleMessage>(this,
				(_, message) => RenderScaling = message.Value);

			if (Main.AutoloadsLoaded)
				SwapToRealUi();
			else
				Main.AutoloadsReady += OnAutoloadsReady;
		}
		catch (Exception exception)
		{
			if (
				!Main.AskUser(
					"UI Load Failed",
					Main.FormatFailureMessage(
						"Ensemble UI failed to load", exception, "Ensemble UI may not appear.")))
				Main.FailFast(exception);

			QueueFree();
		}
	}

	public override void _ExitTree()
	{
		if (Performance.HasCustomMonitor(ProcessTimeMonitor))
			Performance.RemoveCustomMonitor(ProcessTimeMonitor);

		WeakReferenceMessenger.Default.Unregister<SetUiRenderScaleMessage>(this);
		Dispatcher.UIThread.UnhandledException -= OnAvaloniaUnhandledException;
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

	private static float GetRenderScale(Vector2I size)
	{
		Log.Debug("Window resolution: {Size}", size);

		var diagonal = Math.Sqrt(size.X * size.X + size.Y * size.Y);
		Log.Debug("Window diagonal: {Diagonal:F2}", diagonal);

		return diagonal switch
		{
			< 2570.06d => 1,
			< 3671.51d => 1.25f,
			_ => 1.5f
		};
	}

	private static void OnAvaloniaUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		args.Handled = true;
		Log.Error(args.Exception, "Ensemble mitigated an unhandled exception in Avalonia.");
	}

	private void OnAutoloadsReady()
	{
		Main.AutoloadsReady -= OnAutoloadsReady;
		SwapToRealUi();
	}

	private void SwapToRealUi()
	{
		Log.Debug("Swapping loading UI to real UI...");
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
			Log.Debug("Swapped loading UI to real UI in {ElapsedMs:F3} ms.", stopwatch.Elapsed.TotalMilliseconds);

			RenderScaling = GetRenderScale(GetWindow().Size);
			Log.Debug("Initial {Class} render scale: {Scale}", nameof(Ui), RenderScaling);
		}
		catch (Exception exception)
		{
			if (
				!Main.AskUser(
					"UI Swap Failed",
					Main.FormatFailureMessage(
						"Ensemble UI failed to swap to the real UI",
						exception,
						"Ensemble UI may not appear as the real UI.")))
				Main.FailFast(exception);

			QueueFree();
		}
	}
}
