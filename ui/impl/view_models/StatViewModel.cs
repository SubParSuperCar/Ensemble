#if !DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Root.Common.Input;
using Root.Common.Util;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Environment = System.Environment;

namespace Root.Ui.Impl.ViewModels;

public partial class StatViewModel : ViewModelBase
{
	private const double RefreshInterval = 1 / 3d;
	private const double SampleWindow = 3 / 4d;

	private readonly DispatcherService _dispatcher;
	private readonly Queue<double> _frameTimes = [];

	private double _sinceLastRefresh;

	public StatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.Process += OnProcess;
	}

	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var now = Time.GetTicksUsec() / (double)TimeSpan.MicrosecondsPerSecond;
		_frameTimes.Enqueue(now);

		while (_frameTimes.Count > 0 && now - _frameTimes.Peek() > SampleWindow)
			_frameTimes.Dequeue();

		_sinceLastRefresh += delta;
		if (_sinceLastRefresh < RefreshInterval) return;
		_sinceLastRefresh -= RefreshInterval;

		var sampleDuration = _frameTimes.Count > 1 ? now - _frameTimes.Peek() : 0;
		var fps = sampleDuration > 0 ? (_frameTimes.Count - 1) / sampleDuration : 0;
		var frameTimeMs = fps > 0 ? TimeSpan.MillisecondsPerSecond / fps : double.PositiveInfinity;

#if DEBUG
		var dram = OS.GetStaticMemoryUsage();
#else
		using var process = Process.GetCurrentProcess();
		var dram = (ulong)process.PrivateMemorySize64;
#endif

		var processTimeMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * TimeSpan.MillisecondsPerSecond;
		var physicsTimeMs =
			Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * TimeSpan.MillisecondsPerSecond;

		List<(string Key, object Value)> stats =
		[
			("Frame Rate", string.Create(CultureInfo.InvariantCulture, $"{fps:F2} FPS ({frameTimeMs:F3} mspf)")),
			("Process Time", string.Create(CultureInfo.InvariantCulture, $"{processTimeMs:F3} msec")),
			("Physics Time", string.Create(CultureInfo.InvariantCulture, $"{physicsTimeMs:F3} msec")),
			("Used DRAM", Formatter.FormatBytes(dram)),
			("Used VRAM", Formatter.FormatBytes((ulong)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed))),
			("Objects", Performance.GetMonitor(Performance.Monitor.ObjectCount)),
			("Nodes", Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)),
			("Orphan Nodes", Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)),
			("Draw Calls", Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)),
			("Render Prims.", Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)),
			("Input Sinking", InputExtensions.IsSunk)
		];

		if (GPlayers.Local is { } local && GPlayerManager.Handles.TryGetValue(local.Id, out var handle))
		{
			var character = handle.Character ?? handle.Controller!;

			stats.Add(("Char. Pos.", character.GlobalPosition.Round()));
			stats.Add(("Char. Speed",
				string.Create(CultureInfo.InvariantCulture, $"{character.GetRealVelocity().Length():F3} m/s")));
		}

		var width = stats.Max(stat => stat.Key.Length);
		Text = string.Join(Environment.NewLine, stats.Select(stat => $"{stat.Key.PadRight(width)} = {stat.Value}"));
	}
}
