using Avalonia;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace EnsembleRoot.Ui.Impl.Abstractions;

/// <param name="InitialBounds">The bounds when first opened, relative to the widget area, from 0 to 1.</param>
public sealed record WidgetDescriptor(Rect InitialBounds)
{
	public const int GridSize = 16;

	public static WidgetDescriptor Default { get; } = new(Cells(4, 4, 8, 8));

	/// <summary>The header text, or <see langword="null" /> to derive it from the view model's type name.</summary>
	public string? Title { get; init; }

	public string? Description { get; init; }

	public Size MinSize { get; init; } = new(192, 128);
	public bool IsResizable { get; init; } = true;

	/// <summary>Converts cells of a <see cref="GridSize" />-square grid over the widget area to bounds.</summary>
	public static Rect Cells(int x, int y, int width, int height) =>
		new((double)x / GridSize, (double)y / GridSize, (double)width / GridSize, (double)height / GridSize);
}
