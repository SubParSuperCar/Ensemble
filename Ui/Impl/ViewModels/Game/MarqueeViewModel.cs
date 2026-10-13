using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Impl.Abstractions;
using Estragonia;
using Godot;
using Color = Godot.Color;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class MarqueeViewModel : ViewModelBase
{
	private const double FillOpacity = 1 / 4d;
	private const double StrokeOpacity = 3 / 4d;

	private Color? _color;

	public MarqueeViewModel()
	{
		OnAreaChanged(GToolManager.Marquee.Area);
		GToolManager.Marquee.AreaChanged += OnAreaChanged;
	}

	[ObservableProperty] public partial bool IsActive { get; set; }

	/// <summary>The area in viewport pixels, kept once inactive so the marquee fades out in place.</summary>
	[ObservableProperty]
	public partial Rect Area { get; set; }

	[ObservableProperty] public partial IBrush? Fill { get; set; }
	[ObservableProperty] public partial IBrush? Stroke { get; set; }

	protected override void OnDispose() => GToolManager.Marquee.AreaChanged -= OnAreaChanged;

	private void OnAreaChanged(Rect2? area)
	{
		IsActive = area is not null;

		if (area is not { Position: var position, Size: var size })
			return;

		Area = new Rect(position.X, position.Y, size.X, size.Y);

		if (GToolManager.Marquee.Color is var color && color == _color)
			return;

		_color = color;

		Fill = new ImmutableSolidColorBrush(color.ToAvaloniaColor(), FillOpacity);
		Stroke = new ImmutableSolidColorBrush(color.ToAvaloniaColor(), StrokeOpacity);
	}
}
