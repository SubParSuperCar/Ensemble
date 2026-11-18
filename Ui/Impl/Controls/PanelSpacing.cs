using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Keeps every grid's, stack panel's, and dock panel's spacing on whole pixels at the UI's scale. Avalonia rounds
///     a panel's children to pixels but not the spacing between them, so at a fractional scale its gaps differ by a
///     pixel, and a grid's last cells overflow it.
/// </summary>
public static class PanelSpacing
{
	private static readonly StyledProperty<double>[] Properties =
	[
		Grid.RowSpacingProperty, Grid.ColumnSpacingProperty, StackPanel.SpacingProperty,
		DockPanel.HorizontalSpacingProperty, DockPanel.VerticalSpacingProperty
	];

	// Each panel's spacing as it was set, since the property itself holds the snapped value
	private static readonly ConditionalWeakTable<Panel, Dictionary<StyledProperty<double>, double>> SetSpacings = new();

	private static bool _isSnapping;

	/// <summary>The UI's render scale, which every spacing is snapped for.</summary>
	public static double Scale
	{
		get;
		set
		{
			field = value;

			foreach (var (panel, spacings) in SetSpacings)
				foreach (var (property, spacing) in spacings)
					Snap(panel, property, spacing);
		}
	} = 1d;

	public static void Register()
	{
		foreach (var property in Properties)
			property.Changed.AddClassHandler<Panel>(OnSpacingChanged);
	}

	private static void OnSpacingChanged(Panel panel, AvaloniaPropertyChangedEventArgs e)
	{
		if (_isSnapping)
			return;

		var property = (StyledProperty<double>)e.Property;
		var spacing = panel.GetValue(property);

		SetSpacings.GetOrCreateValue(panel)[property] = spacing;
		Snap(panel, property, spacing);
	}

	private static void Snap(Panel panel, StyledProperty<double> property, double spacing)
	{
		_isSnapping = true;

		try
		{
			panel.SetCurrentValue(property, LayoutHelper.RoundLayoutValue(spacing, Scale));
		}
		finally
		{
			_isSnapping = false;
		}
	}
}
