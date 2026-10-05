using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.ViewModels;

// ReSharper disable UnusedMethodReturnValue.Global

namespace EnsembleRoot.Ui.Impl.Services;

/// <summary>
///     Registers, opens, closes, and stacks widgets, at most one per view model type. Widgets live as long as the
///     manager's service scope, e.g., a session's when owned by <see cref="GameViewModel" />.
/// </summary>
public sealed class WidgetManagerService(IServiceProvider services) : DisposableObject, IScopedObject, IServiceBase
{
	private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

	private readonly Dictionary<Type, WidgetEntry> _entriesByType = [];
	private readonly Dictionary<Type, WidgetViewModel> _widgetsByType = [];
	private int _topZIndex;

	public ObservableCollection<WidgetEntry> Entries { get; } = [];
	public ObservableCollection<WidgetViewModel> Widgets { get; } = [];

	public WidgetViewModel? Active { get; private set; }

	public WidgetEntry Register<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget
	{
		if (_entriesByType.TryGetValue(typeof(TViewModel), out var entry))
			return entry;

		entry = new WidgetEntry(
			this,
			typeof(TViewModel),
			TViewModel.Descriptor,
			services.Create<TViewModel>);

		_entriesByType.Add(entry.Type, entry);
		Entries.Insert(
			Entries.TakeWhile(other => string.Compare(other.Title, entry.Title, StringComparison.OrdinalIgnoreCase) < 0)
				.Count(),
			entry);

		return entry;
	}

	public bool IsOpen<TViewModel>() where TViewModel : ViewModelBase, IWidget =>
		_widgetsByType.ContainsKey(typeof(TViewModel));

	public TViewModel Open<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget =>
		(TViewModel)Open(Register<TViewModel>()).Content;

	public bool Close<TViewModel>() where TViewModel : ViewModelBase, IWidget => Close(typeof(TViewModel));

	public bool Toggle<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget =>
		Toggle(Register<TViewModel>());

	internal WidgetViewModel Open(WidgetEntry entry)
	{
		if (_widgetsByType.TryGetValue(entry.Type, out var open))
		{
			Activate(open);
			return open;
		}

		var widget = new WidgetViewModel(
			this,
			entry,
			entry.CreateContent(),
			entry.LastBounds ?? entry.Descriptor.InitialBounds);

		_widgetsByType.Add(entry.Type, widget);
		Widgets.Add(widget);
		Activate(widget);

		entry.IsOpen = true;
		return widget;
	}

	// Closing widgets stay shown while they fade out, then are removed and disposed
	internal bool Close(Type type, bool skipAnimations = false)
	{
		if (!_widgetsByType.Remove(type, out var widget))
			return false;

		widget.Entry.LastBounds = widget.RestoredBounds;
		widget.Entry.IsOpen = false;
		widget.IsClosing = true;

		if (ReferenceEquals(Active, widget))
		{
			Active = null;

			if (_widgetsByType.Values.MaxBy(static other => other.ZIndex) is { } next)
				Activate(next);
		}

		if (!skipAnimations && Application.Current?.FindResource("TransitionDuration") is TimeSpan duration)
			DispatcherTimer.RunOnce(() => Remove(widget), duration);
		else
			Remove(widget);

		return true;
	}

	internal bool Toggle(WidgetEntry entry)
	{
		if (Close(entry.Type))
			return false;

		Open(entry);
		return true;
	}

	internal void Activate(WidgetViewModel widget)
	{
		if (ReferenceEquals(Active, widget))
			return;

		Active?.IsActive = false;
		Active = widget;

		widget.IsActive = true;
		widget.ZIndex = ++_topZIndex;
	}

	protected override void OnDispose()
	{
		foreach (var type in _widgetsByType.Keys.ToArray())
			Close(type, true);
	}

	private void Remove(WidgetViewModel widget)
	{
		Widgets.Remove(widget);
		widget.Dispose();
	}
}
