using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace EnsembleRoot.Ui.Controls;

/// <param name="Tip">What the swatch's tooltip says.</param>
public sealed record Swatch(Color Color, IBrush Brush, string Tip);

/// <summary>
///     Color swatches in rows that wrap at the control's width, drawn as one visual: a palette has hundreds, and a
///     control apiece made it slow to open and to draw. Left-clicking a swatch runs <see cref="Command" /> with it.
/// </summary>
public sealed class SwatchGrid : Control
{
	private const double CellSize = 20d;
	private const double Gap = 2d;
	private const double Pitch = CellSize + Gap;

	public static readonly StyledProperty<IReadOnlyList<Swatch>?> ItemsProperty =
		AvaloniaProperty.Register<SwatchGrid, IReadOnlyList<Swatch>?>(nameof(Items));

	public static readonly StyledProperty<ICommand?> CommandProperty =
		AvaloniaProperty.Register<SwatchGrid, ICommand?>(nameof(Command));

	public static readonly StyledProperty<IBrush?> BorderBrushProperty =
		AvaloniaProperty.Register<SwatchGrid, IBrush?>(nameof(BorderBrush));

	public static readonly StyledProperty<IBrush?> HoverBrushProperty =
		AvaloniaProperty.Register<SwatchGrid, IBrush?>(nameof(HoverBrush));

	private int _columns = 1;
	private int _hovered = -1;

	static SwatchGrid()
	{
		AffectsRender<SwatchGrid>(BorderBrushProperty, HoverBrushProperty);
	}

	public IReadOnlyList<Swatch>? Items
	{
		get => GetValue(ItemsProperty);
		set => SetValue(ItemsProperty, value);
	}

	public ICommand? Command
	{
		get => GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public IBrush? BorderBrush
	{
		get => GetValue(BorderBrushProperty);
		set => SetValue(BorderBrushProperty, value);
	}

	public IBrush? HoverBrush
	{
		get => GetValue(HoverBrushProperty);
		set => SetValue(HoverBrushProperty, value);
	}

	public override void Render(DrawingContext context)
	{
		if (Items is not { } items)
			return;

		var border = BorderBrush is { } brush ? new Pen(brush) : null;
		var hover = HoverBrush is { } hoverBrush ? new Pen(hoverBrush) : null;

		for (var i = 0; i < items.Count; i++)
		{
			var cell = GetCell(i).Deflate(0.5);

			context.FillRectangle(items[i].Brush, cell);

			if ((i == _hovered ? hover : border) is { } pen)
				context.DrawRectangle(pen, cell);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var count = Items?.Count ?? 0;

		_columns = double.IsFinite(availableSize.Width)
			? Math.Max(1, (int)((availableSize.Width + Gap) / Pitch))
			: Math.Max(1, count);

		var rows = (count + _columns - 1) / _columns;
		return new Size(Math.Min(count, _columns) * Pitch - Gap, Math.Max(0, rows * Pitch - Gap));
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != ItemsProperty)
			return;

		if (change.OldValue is INotifyCollectionChanged oldItems)
			oldItems.CollectionChanged -= OnItemsChanged;

		if (change.NewValue is INotifyCollectionChanged newItems)
			newItems.CollectionChanged += OnItemsChanged;

		OnItemsChanged(null, null);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		SetHovered(HitTest(e.GetPosition(this)));
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);
		SetHovered(-1);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (
			e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Items is { } items &&
			HitTest(e.GetPosition(this)) is var index and >= 0)
			Command?.Execute(items[index]);
	}

	private Rect GetCell(int index)
	{
		var (row, column) = Math.DivRem(index, _columns);
		return new Rect(column * Pitch, row * Pitch, CellSize, CellSize);
	}

	private int HitTest(Point point)
	{
		var column = (int)(point.X / Pitch);
		var index = (int)(point.Y / Pitch) * _columns + column;

		return column < _columns && index < (Items?.Count ?? 0) && GetCell(index).Contains(point) ? index : -1;
	}

	private void SetHovered(int index)
	{
		if (_hovered == index)
			return;

		_hovered = index;
		ToolTip.SetTip(this, index >= 0 ? Items![index].Tip : null);

		InvalidateVisual();
	}

	private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs? e)
	{
		_hovered = -1;

		InvalidateMeasure();
		InvalidateVisual();
	}
}
