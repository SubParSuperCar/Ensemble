using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Controls;

/// <summary>A selectable text block that never loses its last line (see <see cref="WholeTextBlock" />).</summary>
public sealed class WholeSelectableTextBlock : SelectableTextBlock
{
	protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

	protected override Size ArrangeOverride(Size finalSize)
	{
		base.ArrangeOverride(finalSize.WithHeight(double.PositiveInfinity));
		return finalSize;
	}
}
