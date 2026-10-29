using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Sessions.Api;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using Godot;
#if !DEBUG
using System.Diagnostics;
#endif

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class StatViewModel : ViewModelBase
{
	private const double RefreshInterval = 1 / 3d;
	private const double SampleWindow = 1d;

#if !DEBUG
	private readonly Process _process = Process.GetCurrentProcess();
#endif

	private readonly DispatcherService _dispatcher;
	private readonly Queue<double> _uiFrameTimes = [];
	private double _sinceRefresh;

	public StatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		Refresh(GetNow());
		dispatcher.UiProcess += OnUiProcess;
	}

	[ObservableProperty] public partial string Text { get; private set; } = string.Empty;

	protected override void OnDispose()
	{
		_dispatcher.UiProcess -= OnUiProcess;

#if !DEBUG
		_process.Dispose();
#endif
	}

	private void OnUiProcess(double delta)
	{
		var now = GetNow();
		_uiFrameTimes.Enqueue(now);

		while (_uiFrameTimes.Count > 0 && now - _uiFrameTimes.Peek() > SampleWindow)
			_uiFrameTimes.Dequeue();

		_sinceRefresh += delta;
		if (_sinceRefresh < RefreshInterval)
			return;

		_sinceRefresh %= RefreshInterval;
		Refresh(now);
	}

	private void Refresh(double now)
	{
		var fps = Engine.GetFramesPerSecond();
		var frameTimeMs = fps > 0 ? TimeSpan.MillisecondsPerSecond / fps : double.PositiveInfinity;

		var sampleDuration = _uiFrameTimes.Count > 1 ? now - _uiFrameTimes.Peek() : 0d;
		var uiFps = sampleDuration > 0 ? (_uiFrameTimes.Count - 1) / sampleDuration : 0d;
		var uiFrameTimeMs = uiFps > 0 ? TimeSpan.MillisecondsPerSecond / uiFps : double.PositiveInfinity;

#if DEBUG
		var dram = ByteFormat.Humanize(OS.GetStaticMemoryUsage());
#else
		_process.Refresh();
		var dram = $"{ByteFormat.Humanize((ulong)_process.PrivateMemorySize64)} (PWS)";
#endif

		var processTimeMs = GetMonitorMs(Performance.Monitor.TimeProcess);
		var physicsTimeMs = GetMonitorMs(Performance.Monitor.TimePhysicsProcess);
		var uiProcessTimeMs = Performance.HasCustomMonitor(Ui.ProcessTimeMonitor)
			? Performance.GetCustomMonitor(Ui.ProcessTimeMonitor).AsDouble() * TimeSpan.MillisecondsPerSecond
			: 0d;

		List<(string Key, object Value)> stats =
		[
			("3D Frame Rate", string.Create(CultureInfo.InvariantCulture, $"{fps} FPS ({frameTimeMs:F3} mspf)")),
			("Process Time", string.Create(CultureInfo.InvariantCulture, $"{processTimeMs:F3} msec")),
			("Physics Time", string.Create(CultureInfo.InvariantCulture, $"{physicsTimeMs:F3} msec")),
			("UI Frame Rate", string.Create(CultureInfo.InvariantCulture, $"{uiFps:F3} FPS ({uiFrameTimeMs:F3} mspf)")),
			("UI Proc. Time", string.Create(CultureInfo.InvariantCulture, $"{uiProcessTimeMs:F3} msec")),
			("Used DRAM", dram),
			("Used VRAM", ByteFormat.Humanize((ulong)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed))),
			("C# Heap Size", ByteFormat.Humanize((ulong)GC.GetTotalMemory(false))),
			("Objects", Performance.GetMonitor(Performance.Monitor.ObjectCount)),
			("Nodes", Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)),
			("Orphan Nodes", Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)),
			("Draw Calls", Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)),
			("Render Prims.", Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)),
			("Input Sinking", InputSink.IsSunk)
		];

		AddSessionStats(stats);

		var width = stats.Max(static stat => stat.Key.Length);
		Text = string.Join('\n', stats.Select(stat => $"{stat.Key.PadRight(width)} = {stat.Value}"));
	}

	// Only some sessions have them: traffic in multiplayer, and the character once spawned
	private static void AddSessionStats(List<(string Key, object Value)> stats)
	{
		if (GSessionManager.Mode is SessionMode.MultiPlayer)
			stats.Add(("Net. Traffic",
				$"{ByteFormat.Humanize((ulong)GSessionManager.ReceivedBytesPerSecond)}/s in, " +
				$"{ByteFormat.Humanize((ulong)GSessionManager.SentBytesPerSecond)}/s out"));

		if (GPlayers.Local is null || GPlayerManager.LocalHandle is not { Body: var character })
			return;

		stats.Add(("Char. Pos.", character.GlobalPosition.Round()));
		stats.Add(("Char. Speed",
			string.Create(CultureInfo.InvariantCulture, $"{character.GetRealVelocity().Length():0.###} m/s")));
	}

	private static double GetNow() => Time.GetTicksUsec() / (double)TimeSpan.MicrosecondsPerSecond;

	private static double GetMonitorMs(Performance.Monitor monitor) =>
		Performance.GetMonitor(monitor) * TimeSpan.MillisecondsPerSecond;
}
