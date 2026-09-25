#if !ENSEMBLE_DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using Godot;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class StatViewModel : ViewModelBase
{
	private const double RefreshInterval = 1 / 3d;
	private const double SampleWindow = 1d;

#if !ENSEMBLE_DEBUG
	private readonly Process _process = Process.GetCurrentProcess();
#endif

	private readonly DispatcherService _dispatcher;
	private readonly Queue<double> _uiFrameTimes = [];
	private double _sinceLastRefresh = RefreshInterval;

	public StatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.UiProcess += OnUiProcess;
	}

	[ObservableProperty] public partial string Text { get; set; } = "<Default>";

	protected override void OnDispose() => _dispatcher.UiProcess -= OnUiProcess;

#pragma warning disable MA0051
	private void OnUiProcess(UiProcessData data)
#pragma warning restore MA0051
	{
		var now = Time.GetTicksUsec() / (double)TimeSpan.MicrosecondsPerSecond;
		_uiFrameTimes.Enqueue(now);

		while (_uiFrameTimes.Count > 0 && now - _uiFrameTimes.Peek() > SampleWindow)
			_uiFrameTimes.Dequeue();

		_sinceLastRefresh += data.SinceLastUiProcess;
		if (_sinceLastRefresh < RefreshInterval)
			return;

		_sinceLastRefresh %= RefreshInterval;

		var fps = Engine.GetFramesPerSecond();
		var frameTimeMs = fps > 0 ? TimeSpan.MillisecondsPerSecond / fps : double.PositiveInfinity;

		var sampleDuration = _uiFrameTimes.Count > 1 ? now - _uiFrameTimes.Peek() : 0;
		var uiFps = sampleDuration > 0 ? (_uiFrameTimes.Count - 1) / sampleDuration : 0;
		var uiFrameTimeMs = uiFps > 0 ? TimeSpan.MillisecondsPerSecond / uiFps : double.PositiveInfinity;

#if ENSEMBLE_DEBUG
		var dram = Formatter.FormatBytes(OS.GetStaticMemoryUsage());
#else
		_process.Refresh();
		var dram = $"{Formatter.FormatBytes((ulong)_process.PrivateMemorySize64)} (PWS)";
#endif

		var processTimeMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * TimeSpan.MillisecondsPerSecond;
		var physicsTimeMs =
			Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * TimeSpan.MillisecondsPerSecond;

		var uiProcessTimeMs = Performance.HasCustomMonitor(Ui.ProcessTimeMonitor)
			? Performance.GetCustomMonitor(Ui.ProcessTimeMonitor).AsDouble() * TimeSpan.MillisecondsPerSecond
			: 0;

		List<(string Key, object Value)> stats =
		[
			("Frame Rate", string.Create(CultureInfo.InvariantCulture, $"{fps} FPS ({frameTimeMs:F3} mspf)")),
			("Process Time", string.Create(CultureInfo.InvariantCulture, $"{processTimeMs:F3} msec")),
			("Physics Time", string.Create(CultureInfo.InvariantCulture, $"{physicsTimeMs:F3} msec")),
			("UI Frame Rate", string.Create(CultureInfo.InvariantCulture, $"{uiFps:F3} FPS ({uiFrameTimeMs:F3} mspf)")),
			("UI Proc. Time", string.Create(CultureInfo.InvariantCulture, $"{uiProcessTimeMs:F3} msec")),
			("Used DRAM", dram),
			("Used VRAM", Formatter.FormatBytes((ulong)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed))),
			("C# Heap Size", Formatter.FormatBytes((ulong)GC.GetTotalMemory(false))),
			("Objects", Performance.GetMonitor(Performance.Monitor.ObjectCount)),
			("Nodes", Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)),
			("Orphan Nodes", Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)),
			("Draw Calls", Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)),
			("Render Prims.", Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)),
			("Input Sinking", InputSink.IsSunk)
		];

		if (GPlayers.Local is { } local && GPlayerManager.Handles.TryGetValue(local.Id, out var handle))
		{
			var character = handle.Character ?? handle.Controller!;

			stats.Add(("Char. Pos.", character.GlobalPosition.Round()));
			stats.Add(("Char. Speed",
				string.Create(CultureInfo.InvariantCulture, $"{character.GetRealVelocity().Length():0.###} m/s")));
		}

		var width = stats.Max(static stat => stat.Key.Length);
		Text = string.Join('\n', stats.Select(stat => $"{stat.Key.PadRight(width)} = {stat.Value}"));
	}
}
