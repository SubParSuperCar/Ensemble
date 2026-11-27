using Godot;

namespace EnsembleRoot.Common.Utils;

/// <summary>Where the latest frame's time went, in milliseconds.</summary>
/// <remarks>
///     Godot measures the rendering times once first asked to, which adds a few GPU timestamps per frame, too little to
///     be worth turning back off. The GPU's time trails by a frame or two.
/// </remarks>
public static class FrameTimes
{
	private static Rid? _viewport;

	public static double ProcessMs => GetMonitorMs(Performance.Monitor.TimeProcess);
	public static double PhysicsMs => GetMonitorMs(Performance.Monitor.TimePhysicsProcess);

	/// <summary>Frame setup plus the main viewport's draw calls.</summary>
	public static double RenderCpuMs =>
		RenderingServer.ViewportGetMeasuredRenderTimeCpu(Viewport) + RenderingServer.GetFrameSetupTimeCpu();

	public static double RenderGpuMs => RenderingServer.ViewportGetMeasuredRenderTimeGpu(Viewport);

	private static Rid Viewport
	{
		get
		{
			if (_viewport is { } viewport)
				return viewport;

			viewport = ((SceneTree)Engine.GetMainLoop()).Root.GetViewportRid();
			RenderingServer.ViewportSetMeasureRenderTime(viewport, true);
			_viewport = viewport;

			return viewport;
		}
	}

	private static double GetMonitorMs(Performance.Monitor monitor) =>
		Performance.GetMonitor(monitor) * TimeSpan.MillisecondsPerSecond;
}
