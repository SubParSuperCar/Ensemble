using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace EnsembleRoot.Ui.Controls;

/// <summary>Every number box's text shows when it can't be taken, and Enter or Escape drops its focus.</summary>
public static class NumberBoxes
{
	private const string InvalidClass = "invalid";

	public static void Register()
	{
		NumericUpDown.TextProperty.Changed.AddClassHandler<NumericUpDown>(OnTextChanged);

		// At the top level, after a view's own handling, and even if the box handled the key
		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, RoutingStrategies.Bubble, true);
	}

	private static bool IsValid(NumericUpDown box) =>
		string.IsNullOrEmpty(box.Text) ||
		(decimal.TryParse(box.Text, box.ParsingNumberStyle, box.NumberFormat, out var value) &&
		 value >= box.Minimum && value <= box.Maximum);

	private static void OnTextChanged(NumericUpDown box, AvaloniaPropertyChangedEventArgs e) =>
		box.Classes.Set(InvalidClass, !IsValid(box));

	private static void OnKeyDown(TopLevel topLevel, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Escape && e.Source is Control source &&
			source.FindAncestorOfType<NumericUpDown>(true) is not null)
			topLevel.FocusManager.Focus(null);
	}
}
