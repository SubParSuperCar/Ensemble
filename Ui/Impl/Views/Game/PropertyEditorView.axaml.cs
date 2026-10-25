using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class PropertyEditorView : UserControl, IViewFor<PropertyEditorViewModel>
{
	public PropertyEditorView()
	{
		InitializeComponent();

		// Handled ones too, as number boxes handle Enter themselves
		Items.AddHandler(KeyDownEvent, OnItemsKeyDown, RoutingStrategies.Bubble, true);
	}

	private void OnFilterBoxGotFocus(object? sender, FocusChangedEventArgs e)
	{
		if (DataContext is PropertyEditorViewModel viewModel)
			viewModel.FilterQuery = string.Empty;
	}

	// Enter commits an edit by dropping focus, and Escape drops it after undoing the typing
	private void OnItemsKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Enter or Key.Escape))
			return;

		if (e.Key is Key.Escape && e.Source is TextBox { DataContext: TextPropertyItem item } textBox)
			textBox.Text = item.Value;

		e.Handled = true;
		TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);
	}
}
