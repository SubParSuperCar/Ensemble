using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class PropertyEditorView : UserControl, IViewFor<PropertyEditorViewModel>
{
	private const string NameMaxWidthKey = "NameMaxWidth";
	private const double NameWidthShare = 3 / 7d;

	public PropertyEditorView()
	{
		InitializeComponent();

		// Handled ones too, as number boxes handle Enter and spins themselves
		Items.AddHandler(KeyDownEvent, OnItemsKeyDown, RoutingStrategies.Bubble, true);
		Items.AddHandler(Spinner.SpinEvent, OnItemsSpin, RoutingStrategies.Bubble, true);
	}

	// The editor's bound value, which an editor's own edits update only when committed
	private static BindingExpressionBase? GetValueBinding(Control source) =>
		source.FindAncestorOfType<NumericUpDown>(true) is { } number
			? BindingOperations.GetBindingExpressionBase(number, NumericUpDown.ValueProperty)
			: BindingOperations.GetBindingExpressionBase(source, TextBox.TextProperty);

	// Long names take up to a share of the list's width, and scroll while hovered past that
	private void OnItemsSizeChanged(object? sender, SizeChangedEventArgs e) =>
		Items.Resources[NameMaxWidthKey] = e.NewSize.Width * NameWidthShare;

	private void OnFilterBoxGotFocus(object? sender, FocusChangedEventArgs e)
	{
		if (DataContext is PropertyEditorViewModel viewModel)
			viewModel.FilterQuery = string.Empty;
	}

	// Enter commits an edit by dropping focus, and Escape drops it after reverting the editor
	private void OnItemsKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Enter or Key.Escape))
			return;

		if (e.Key is Key.Escape && e.Source is Control source)
			GetValueBinding(source)?.UpdateTarget();

		e.Handled = true;
		TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);
	}

	// Typed values wait for Enter or focus loss, but spins are done at once
	private static void OnItemsSpin(object? sender, SpinEventArgs e)
	{
		if (e.Source is Control source)
			GetValueBinding(source)?.UpdateSource();
	}
}
