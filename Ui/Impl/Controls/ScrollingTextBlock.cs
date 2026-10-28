using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     A line of text, trimmed with an ellipsis where it doesn't fit, that scrolls back and forth while hovered to
///     show the rest.
/// </summary>
public sealed class ScrollingTextBlock : Control
{
	// In pixels per second, and seconds of pause before each pass
	private const double ScrollSpeed = 48d;
	private const double ScrollPause = 1d;

	public static readonly StyledProperty<string?> TextProperty =
		AvaloniaProperty.Register<ScrollingTextBlock, string?>(nameof(Text));

	private readonly TextBlock _text = new() { TextTrimming = TextTrimming.CharacterEllipsis };
	private bool _isFramePending;
	private bool _isScrolling;
	private double _offset;
	private TimeSpan? _scrollStart;

	public ScrollingTextBlock()
	{
		ClipToBounds = true;

		_text.Bind(TextBlock.TextProperty, this.GetObservable(TextProperty));

		LogicalChildren.Add(_text);
		VisualChildren.Add(_text);
	}

	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	// Avalonia trims a line whose glyphs overhang its width, which a text block's desired size leaves out
	private double WholeWidth =>
		Math.Ceiling(
			_text.DesiredSize.Width +
			Math.Max(_text.TextLayout.OverhangLeading, 0) +
			Math.Max(_text.TextLayout.OverhangTrailing, 0));

	private double Overflow => Math.Max(WholeWidth - Bounds.Width, 0);

	protected override Size MeasureOverride(Size availableSize)
	{
		_text.Measure(availableSize.WithWidth(double.PositiveInfinity));
		return new Size(Math.Min(WholeWidth, availableSize.Width), _text.DesiredSize.Height);
	}

	// Whole while it fits or scrolls, and trimmed otherwise
	protected override Size ArrangeOverride(Size finalSize)
	{
		var width = _isScrolling || WholeWidth <= finalSize.Width ? WholeWidth : finalSize.Width;
		_text.Arrange(new Rect(-_offset, 0, width, finalSize.Height));

		return finalSize;
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		base.OnPointerEntered(e);

		if (_isScrolling || Overflow <= 0)
			return;

		_isScrolling = true;
		_scrollStart = null;
		RequestFrame();
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		StopScrolling();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		StopScrolling();
	}

	private void RequestFrame()
	{
		if (_isFramePending || TopLevel.GetTopLevel(this) is not { } topLevel)
			return;

		_isFramePending = true;
		topLevel.RequestAnimationFrame(OnFrame);
	}

	private void OnFrame(TimeSpan time)
	{
		_isFramePending = false;

		if (!_isScrolling)
			return;

		var overflow = Overflow;

		// The text may have come to fit, e.g., in a widened widget
		if (overflow <= 0)
		{
			StopScrolling();
			return;
		}

		_scrollStart ??= time;

		// Passes alternate directions, each pausing before it moves
		var travel = overflow / ScrollSpeed;
		var pass = ScrollPause + travel;
		var elapsed = (time - _scrollStart.Value).TotalSeconds % (2 * pass);
		var progress = Math.Clamp((elapsed % pass - ScrollPause) / travel, 0, 1);

		_offset = overflow * (elapsed < pass ? progress : 1 - progress);
		InvalidateArrange();
		RequestFrame();
	}

	private void StopScrolling()
	{
		if (!_isScrolling)
			return;

		_isScrolling = false;
		_offset = 0;
		InvalidateArrange();
	}
}
