using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;

namespace EnsembleRoot.Ui.Impl.ViewModels;

/// <remarks><see cref="Bounds" /> is relative to the widget area, from 0 to 1, and always kept within it.</remarks>
public sealed partial class WidgetViewModel : ViewModelBase
{
	private static readonly Rect MaximizedBounds = new(0, 0, 1, 1);

	private readonly WidgetManagerService _manager;
	private Rect? _restoreBounds;

	internal WidgetViewModel(WidgetManagerService manager, WidgetEntry entry, ViewModelBase content, Rect bounds)
	{
		_manager = manager;

		Entry = entry;
		Content = content;
		Bounds = bounds
			.WithX(Math.Clamp(bounds.X, 0, 1 - bounds.Width))
			.WithY(Math.Clamp(bounds.Y, 0, 1 - bounds.Height));
	}

	public WidgetEntry Entry { get; }
	public ViewModelBase Content { get; }

	public string Title => Entry.Title;
	public WidgetDescriptor Descriptor => Entry.Descriptor;

	[ObservableProperty] public partial Rect Bounds { get; set; }
	[ObservableProperty] public partial int ZIndex { get; set; }
	[ObservableProperty] public partial bool IsActive { get; set; }
	[ObservableProperty] public partial bool IsMaximized { get; set; }
	[ObservableProperty] public partial bool IsClosing { get; set; }

	internal Rect RestoredBounds => _restoreBounds ?? Bounds;

	public void Activate()
	{
		if (!IsClosing)
			_manager.Activate(this);
	}

	public void Move(Rect origin, Vector delta, Size area)
	{
		if (IsMaximized || !IsValid(area))
			return;

		Bounds = origin
			.WithX(Math.Clamp(origin.X + delta.X / area.Width, 0, 1 - origin.Width))
			.WithY(Math.Clamp(origin.Y + delta.Y / area.Height, 0, 1 - origin.Height));
	}

	public void Resize(Rect origin, Vector delta, Size area)
	{
		if (IsMaximized || !Descriptor.IsResizable || !IsValid(area))
			return;

		var minWidth = Math.Min(Descriptor.MinSize.Width / area.Width, 1 - origin.X);
		var minHeight = Math.Min(Descriptor.MinSize.Height / area.Height, 1 - origin.Y);

		Bounds = origin
			.WithWidth(Math.Clamp(origin.Width + delta.X / area.Width, minWidth, 1 - origin.X))
			.WithHeight(Math.Clamp(origin.Height + delta.Y / area.Height, minHeight, 1 - origin.Y));
	}

	protected override void OnDispose() => Content.Dispose();

	[RelayCommand]
	private void Close() => _manager.Close(Entry.Type);

	[RelayCommand]
	private void ToggleMaximized()
	{
		if (IsMaximized)
		{
			Bounds = RestoredBounds;
			_restoreBounds = null;
		}
		else
		{
			_restoreBounds = Bounds;
			Bounds = MaximizedBounds;
		}

		IsMaximized = !IsMaximized;
	}

	private static bool IsValid(Size area) => area is { Width: > 0, Height: > 0 };
}
