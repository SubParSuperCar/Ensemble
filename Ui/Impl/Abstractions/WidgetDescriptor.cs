using Avalonia;

namespace EnsembleRoot.Ui.Impl.Abstractions;

/// <param name="InitialBounds">The bounds when first opened, relative to the widget area, from 0 to 1.</param>
public sealed record WidgetDescriptor(Rect InitialBounds)
{
	public static WidgetDescriptor Default { get; } = new(new Rect(0.25, 0.25, 0.5, 0.5));

	/// <summary>The header text, or <see langword="null" /> to derive it from the view model's type name.</summary>
	public string? Title { get; init; }

	public string? Description { get; init; }

	public Size MinSize { get; init; } = new(192, 128);
	public bool IsResizable { get; init; } = true;
}
