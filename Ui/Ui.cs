using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Messages;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using EnsembleRoot.Ui.Impl.ViewModels;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Dispatcher = Avalonia.Threading.Dispatcher;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using GdControl = Godot.Control;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace EnsembleRoot.Ui;

[GlobalClass]
public partial class Ui : GdControl
{
	public const double ProcessInterval = 1 / 120d;

	public static readonly StringName ProcessTimeMonitor = "Ensemble/Time/UIProcess";

	private AvaloniaControl _host = null!;
	private double _lastUiProcessTime;
	private ulong _processStartTicks;

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

			_host = new AvaloniaControl
			{
				FocusMode = FocusModeEnum.All,
				MouseForcePassScrollEvents = false
			};
			_host.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			_host.Processing += OnHostProcessing;
			_host.Processed += OnHostProcessed;

			_host.Control = CreateLoadingScreen();

			AddChild(_host);

			stopwatch.Stop();
			Console.WriteLine(string.Create(
				CultureInfo.InvariantCulture,
				$"Started {nameof(Ui)} in {stopwatch.Elapsed.TotalMilliseconds:F3} ms"));

			WeakReferenceMessenger.Default.Register<SetUiRenderScaleMessage>(this,
				(_, message) => _host.RenderScaling = message.Value);

			if (Main.AreAutoloadsLoaded)
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
						"Ensemble UI failed to load",
						exception,
						"Ensemble UI may not appear.")))
				Main.FailFast(exception);

			QueueFree();
		}
	}

	public override void _ExitTree()
	{
		Main.AutoloadsReady -= OnAutoloadsReady;

		if (Performance.HasCustomMonitor(ProcessTimeMonitor))
			Performance.RemoveCustomMonitor(ProcessTimeMonitor);

		WeakReferenceMessenger.Default.Unregister<SetUiRenderScaleMessage>(this);
		Dispatcher.UIThread.UnhandledException -= OnAvaloniaUnhandledException;
	}

	private void OnHostProcessing(double delta)
	{
		_processStartTicks = Time.GetTicksUsec();
		WeakReferenceMessenger.Default.Send(new UiProcessMessage(new UiProcessData(delta, GetProcessDeltaTime())));
	}

	private void OnHostProcessed(double delta) =>
		_lastUiProcessTime = (Time.GetTicksUsec() - _processStartTicks) / (double)TimeSpan.MicrosecondsPerSecond;

	public override void _Input(InputEvent @event) => WeakReferenceMessenger.Default.Send(new InputMessage(@event));
	public override void _Notification(int what) => WeakReferenceMessenger.Default.Send(new NotificationMessage(what));

	private static TextBlock CreateLoadingScreen()
	{
		var loadingScreen = new TextBlock
		{
			Text = "Loading Ensemble's Autoloads\u2026\nThis shouldn't take long.",
			FontFamily = new FontFamily("sans-serif"),
			FontWeight = FontWeight.Regular,
			FontSize = 48,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};
		TextOptions.SetTextRenderingMode(loadingScreen, TextRenderingMode.Antialias);

		return loadingScreen;
	}

	private static float GetRenderScale(Vector2I size)
	{
		Log.Debug("Window resolution: {Size}", size);

		var diagonal = Math.Sqrt(size.X * size.X + size.Y * size.Y);
		Log.Debug("Window diagonal: {Diagonal:F2}", diagonal);

		return diagonal switch
		{
			< 1652.18 => 0.75f,
			< 2019.33 => 0.875f,
			< 2570.06 => 1f,
			< 3671.51 => 1.25f,
			_ => 1.5f
		};
	}

	private static void OnAvaloniaUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		args.Handled = true;
		Log.Error(args.Exception, "Ensemble mitigated an unhandled exception in Avalonia");
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
				Callable.From(() => _lastUiProcessTime),
				[],
				Performance.MonitorType.Time);

			var viewModel = services.GetRequiredService<MainViewModel>();
			_host.Control = locator.Build(viewModel);

			stopwatch.Stop();
			Log.Debug("Swapped loading UI to real UI in {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);

			// Set the initial UI scale for better UX; it doesn't update when viewport resolution changes
			_host.RenderScaling = GetRenderScale(GetWindow().Size);
			Log.Debug("Initial {Class} render scale: {Scale}", nameof(Ui), _host.RenderScaling);
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
