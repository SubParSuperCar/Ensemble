using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     A selectable text block that never loses its last line (see <see cref="WholeTextBlock" />), and has the
///     <c>:selection</c> pseudo-class while any of its text is selected.
/// </summary>
public sealed class WholeSelectableTextBlock : SelectableTextBlock
{
	protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == SelectionStartProperty || change.Property == SelectionEndProperty)
			PseudoClasses.Set(":selection", SelectionStart != SelectionEnd);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		base.ArrangeOverride(finalSize.WithHeight(double.PositiveInfinity));
		return finalSize;
	}
}
