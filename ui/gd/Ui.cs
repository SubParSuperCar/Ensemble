using System.Diagnostics;
using Avalonia;
using CommunityToolkit.Mvvm.Messaging;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Extensions;
using Root.Ui.Impl.Messages;
using Root.Ui.Impl.Services;
using Root.Ui.Impl.ViewModels;
using Serilog;
using TinyDialogsNet;

namespace Root.Ui.Gd;

[GlobalClass]
public partial class Ui : AvaloniaControl
{
	public static readonly StringName ProcessTimeMonitor = "Ensemble/Time/UIProcess";

	private double _processTime;

	public override void _Ready()
	{
		try
		{
			Log.Debug("Initializing {Class} (Avalonia User Interface)...", nameof(Ui));

			var stopwatch = Stopwatch.StartNew();

			GetWindow().SetImeActive(true);

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

			base._Ready();

			stopwatch.Stop();
			Log.Debug(
				"{Class} (Avalonia User Interface) initialized in {Elapsed} ({ElapsedMs:F3} msec)",
				nameof(Ui),
				stopwatch.Elapsed,
				stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception exception)
		{
			TinyDialogs.Beep();

			const string message = $"{nameof(Ui)} (Avalonia User Interface) initialization failed";
			Log.Error(exception, message);

			new Thread(() =>
			{
				TinyDialogs.MessageBox(
					message,
					exception.ToString().Replace('"', '\''),
					MessageBoxDialogType.Ok,
					MessageBoxIconType.Error,
					MessageBoxButton.Ok);
			})
			{
				IsBackground = true
			}.Start();

			throw;
		}
	}

	public override void _ExitTree()
	{
		if (Performance.HasCustomMonitor(ProcessTimeMonitor))
			Performance.RemoveCustomMonitor(ProcessTimeMonitor);

		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		var start = Time.GetTicksUsec();

		WeakReferenceMessenger.Default.Send(new ProcessMessage(delta));
		base._Process(delta);

		_processTime = (Time.GetTicksUsec() - start) / (double)TimeSpan.MicrosecondsPerSecond;
	}

	public override void _Input(InputEvent @event)
	{
		WeakReferenceMessenger.Default.Send(new InputMessage(@event));
		base._Input(@event);
	}
}
