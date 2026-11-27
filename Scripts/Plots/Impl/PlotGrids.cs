namespace EnsembleRoot.Scripts.Plots;

/// <summary>
///     The spacing of the local plot's finest grid lines, in meters, which a tool can set (e.g., to its snapping
///     increment), or null for the grid's own. Writable without a loaded world; <see cref="PlotManager" /> applies it.
/// </summary>
public static class PlotGrids
{
	public static float? FineSpacing
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
}
