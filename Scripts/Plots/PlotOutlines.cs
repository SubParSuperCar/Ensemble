using Godot;

namespace EnsembleRoot.Scripts.Plots;

/// <summary>
///     Decides which plot outlines show: a hovered plot (white) takes precedence over the local plot's active tool
///     color. Writable without a loaded world; <see cref="PlotManager" /> applies it to its plots.
/// </summary>
public static class PlotOutlines
{
	public static int? HoveredPlotId
	{
		get;
		set
		{
			if (field == value)
				return;

			field = value;
			Changed?.Invoke();
		}
	}

	public static Color? ToolColor
	{
		get;
		set
		{
			if (field == value)
				return;

			field = value;
			Changed?.Invoke();
		}
	}

	public static event Action? Changed;

	public static Color? GetColor(int plotId) =>
		plotId == HoveredPlotId
			? Colors.White
			: plotId == LocalPlot?.Id
				? ToolColor
				: null;
}
