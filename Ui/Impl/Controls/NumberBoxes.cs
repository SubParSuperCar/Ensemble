using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Every number box takes arithmetic (<see cref="NumberExpression" />) and shows at most
///     <see cref="MaxDecimalPlaces" /> decimals, its text shows when it can't be taken, a spin commits its value at
///     once (where typing may wait for focus loss), and Enter or Escape drops its focus.
/// </summary>
public static class NumberBoxes
{
	public const int MaxDecimalPlaces = 6;

	private const string InvalidClass = "invalid";

	private static readonly string DecimalFormat = "0." + new string('#', MaxDecimalPlaces);

	public static void Register()
	{
		TemplatedControl.TemplateAppliedEvent.AddClassHandler<NumericUpDown>(OnTemplateApplied);
		NumericUpDown.TextProperty.Changed.AddClassHandler<NumericUpDown>(OnTextChanged);

		// At the top level, after a view's own handling, and even if the box handled the key or spin
		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, RoutingStrategies.Bubble, true);
		Spinner.SpinEvent.AddClassHandler<TopLevel>(OnSpin, RoutingStrategies.Bubble, true);
	}

	private static bool IsValid(NumericUpDown box) =>
		string.IsNullOrEmpty(box.Text) ||
		(NumberExpression.TryEvaluate(box.Text, CultureInfo.CurrentCulture.NumberFormat, out var value) &&
		 value >= box.Minimum && value <= box.Maximum);

	private static bool IsInteger(NumericUpDown box) => !box.ParsingNumberStyle.HasFlag(NumberStyles.AllowDecimalPoint);

	private static void OnTemplateApplied(NumericUpDown box, TemplateAppliedEventArgs e) =>
		box.TextConverter ??= new ExpressionConverter(box);

	private static void OnTextChanged(NumericUpDown box, AvaloniaPropertyChangedEventArgs e) =>
		box.Classes.Set(InvalidClass, !IsValid(box));

	private static void OnKeyDown(TopLevel topLevel, KeyEventArgs e)
	{
		if (
			e.Key is not (Key.Enter or Key.Escape) || e.Source is not Control source ||
			source.FindAncestorOfType<NumericUpDown>(true) is null)
			return;

		// Handled, or dropping focus would let the same key through to the game, where Escape can restart the session
		e.Handled = true;
		topLevel.FocusManager.Focus(null);
	}

	private static void OnSpin(TopLevel topLevel, SpinEventArgs e)
	{
		if (e.Source is Control source && source.FindAncestorOfType<NumericUpDown>(true) is { } box)
			BindingOperations.GetBindingExpressionBase(box, NumericUpDown.ValueProperty)?.UpdateSource();
	}

	// Integer boxes round what an expression gives, as their format would show it
	private sealed class ExpressionConverter(NumericUpDown box) : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var result = NumberExpression.Evaluate((string)value!, culture.NumberFormat);
			return IsInteger(box) ? decimal.Round(result, MidpointRounding.AwayFromZero) : result;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			(value as decimal?)?.ToString(
				string.IsNullOrEmpty(box.FormatString) ? DecimalFormat : box.FormatString, culture);
	}
}
