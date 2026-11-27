using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class PropertyEditorView : UserControl, IViewFor<PropertyEditorViewModel>
{
	private const string NameMinWidthKey = "NameMinWidth";
	private const string NameMaxWidthKey = "NameMaxWidth";
	private const double NameMinWidthShare = 1 / 3d;
	private const double NameMaxWidthShare = 1 / 2d;
	private const double ScrollBarGap = 2d;

	private readonly PressFlash _copyFlash;
	private readonly PressFlash _pasteFlash;

	private PropertyEditorViewModel? _viewModel;

	public PropertyEditorView()
	{
		InitializeComponent();

		_copyFlash = new PressFlash(CopyButton);
		_pasteFlash = new PressFlash(PasteButton);

		ScrollBarInset.ApplyGap(ItemsScroll, Items, ScrollBarGap);

		// Handled ones too, as number boxes handle Enter themselves
		Items.AddHandler(KeyDownEvent, OnItemsKeyDown, RoutingStrategies.Bubble, true);
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_viewModel?.Copied -= _copyFlash.Flash;
		_viewModel?.Pasted -= _pasteFlash.Flash;

		_viewModel = DataContext as PropertyEditorViewModel;

		_viewModel?.Copied += _copyFlash.Flash;
		_viewModel?.Pasted += _pasteFlash.Flash;
	}

	// The editor's bound value, which an editor's own edits update only when committed
	private static BindingExpressionBase? GetValueBinding(Control source) =>
		source.FindAncestorOfType<NumericUpDown>(true) is { } number
			? BindingOperations.GetBindingExpressionBase(number, NumericUpDown.ValueProperty)
			: BindingOperations.GetBindingExpressionBase(source, TextBox.TextProperty);

	// Names take at least a share of the list's width, so the editors start near its middle, and long ones take up to
	// a larger share and scroll while hovered past that
	private void OnItemsSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		Items.Resources[NameMinWidthKey] = e.NewSize.Width * NameMinWidthShare;
		Items.Resources[NameMaxWidthKey] = e.NewSize.Width * NameMaxWidthShare;
	}

	private void OnFilterBoxGotFocus(object? sender, FocusChangedEventArgs e) => _viewModel?.FilterQuery = string.Empty;

	private void OnKeyButtonClick(object? sender, RoutedEventArgs e) =>
		TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);

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
}
