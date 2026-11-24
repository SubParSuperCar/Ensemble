using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;

namespace EnsembleRoot.Ui.Controls;

/// <summary>A line of text whose characters flip over and fade as they change, like a flip clock's.</summary>
/// <remarks>Meant for monospace fonts, so a flipping character doesn't shift the rest.</remarks>
public sealed partial class FlipText : Control
{
	private readonly StackPanel _cells = new() { Orientation = Orientation.Horizontal };

	private readonly CompositePageTransition _flip = new()
	{
		PageTransitions =
		{
			new CrossFade(AnimationDuration), new Rotate3DTransition(AnimationDuration, PageSlide.SlideAxis.Vertical)
		}
	};

	public FlipText()
	{
		LogicalChildren.Add(_cells);
		VisualChildren.Add(_cells);
	}

	[GeneratedStyledProperty] public partial string? Text { get; set; }

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == TextProperty)
			Update(Text ?? string.Empty);
	}

	// Aligned at the end, so text that grows (e.g., 9:59 to 10:00) gains cells at the start
	private void Update(string text)
	{
		var cells = _cells.Children;

		while (cells.Count > text.Length)
			cells.RemoveAt(0);

		while (cells.Count < text.Length)
			cells.Insert(0, new TransitioningContentControl { PageTransition = _flip, ClipToBounds = false });

		for (var i = 0; i < text.Length; i++)
		{
			var cell = (TransitioningContentControl)cells[i];

			if (cell.Content is not char current || current != text[i])
				cell.Content = text[i];
		}
	}
}
