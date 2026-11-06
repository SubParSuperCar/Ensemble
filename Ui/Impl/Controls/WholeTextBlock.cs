using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     A text block whose size includes its glyphs' overhangs. Avalonia leaves them out of a text block's size but
///     counts them when it trims or wraps, so text laid out at exactly its own width would lose its last character or
///     word.
/// </summary>
public sealed class WholeTextBlock : TextBlock
{
	protected override Type StyleKeyOverride => typeof(TextBlock);

	protected override Size MeasureOverride(Size availableSize)
	{
		var size = base.MeasureOverride(availableSize);
		var overhang = Math.Max(TextLayout.OverhangLeading, 0) + Math.Max(TextLayout.OverhangTrailing, 0);

		return size.WithWidth(Math.Min(Math.Ceiling(size.Width + overhang), availableSize.Width));
	}
}
