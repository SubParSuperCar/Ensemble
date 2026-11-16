using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class PaintPaletteView : UserControl, IViewFor<PaintPaletteViewModel>
{
	public PaintPaletteView()
	{
		InitializeComponent();

		// Handled ones too, as number boxes handle Enter themselves
		AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, true);
	}

	// Typed values wait for Enter or focus loss; Escape takes back what was typed
	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Enter or Key.Escape) || e.Source is not Control source)
			return;

		if (e.Key is Key.Escape)
			(source.FindAncestorOfType<NumericUpDown>(true) is { } number
				? BindingOperations.GetBindingExpressionBase(number, NumericUpDown.ValueProperty)
				: BindingOperations.GetBindingExpressionBase(source, TextBox.TextProperty))?.UpdateTarget();

		e.Handled = true;
		TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);
	}

	private void OnMaterialFilterGotFocus(object? sender, FocusChangedEventArgs e) =>
		(DataContext as PaintPaletteViewModel)?.MaterialQuery = string.Empty;
}
