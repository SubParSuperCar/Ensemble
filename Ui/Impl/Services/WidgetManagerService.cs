using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Services;

/// <summary>Opens, closes, and stacks widgets, at most one per view model type, for its scope's lifetime.</summary>
public sealed class WidgetManagerService(IServiceProvider services) : DisposableObject, IScopedObject
{
	private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

	private readonly Dictionary<Type, WidgetEntry> _entriesByType = [];
	private readonly Dictionary<Type, WidgetViewModel> _widgetsByType = [];
	private WidgetViewModel? _active;
	private int _topZIndex;

	public ObservableCollection<WidgetEntry> Entries { get; } = [];
	public ObservableCollection<WidgetViewModel> Widgets { get; } = [];

	public WidgetEntry Register<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget
	{
		if (_entriesByType.TryGetValue(typeof(TViewModel), out var entry))
			return entry;

		entry = new WidgetEntry(this, typeof(TViewModel), TViewModel.Descriptor, services.Create<TViewModel>);
		var index = Entries.Count(other => StringComparer.OrdinalIgnoreCase.Compare(other.Title, entry.Title) < 0);

		_entriesByType.Add(entry.Type, entry);
		Entries.Insert(index, entry);

		return entry;
	}

	public void Open<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget =>
		Open(Register<TViewModel>());

	public void Close<TViewModel>() where TViewModel : ViewModelBase, IWidget => Close(typeof(TViewModel));

	private void Open(WidgetEntry entry)
	{
		if (_widgetsByType.TryGetValue(entry.Type, out var open))
		{
			Activate(open);
			return;
		}

		var widget = new WidgetViewModel(
			this, entry, entry.CreateContent(), entry.LastBounds ?? entry.Descriptor.InitialBounds, entry.WasMaximized);

		_widgetsByType.Add(entry.Type, widget);
		Widgets.Add(widget);
		Activate(widget);

		entry.IsOpen = true;
	}

	internal bool Close(Type type, bool skipAnimations = false)
	{
		if (!_widgetsByType.Remove(type, out var widget))
			return false;

		widget.Entry.LastBounds = widget.RestoredBounds;
		widget.Entry.WasMaximized = widget.IsMaximized;
		widget.Entry.IsOpen = false;
		widget.IsClosing = true;

		if (ReferenceEquals(_active, widget))
		{
			_active = null;

			if (_widgetsByType.Values.MaxBy(static other => other.ZIndex) is { } next)
				Activate(next);
		}

		if (skipAnimations)
			Remove(widget);
		else
			DispatcherTimer.RunOnce(() => Remove(widget), AnimationDuration);

		return true;
	}

	internal void Toggle(WidgetEntry entry)
	{
		if (!Close(entry.Type))
			Open(entry);
	}

	internal void Activate(WidgetViewModel widget)
	{
		if (ReferenceEquals(_active, widget))
			return;

		_active?.IsActive = false;
		_active = widget;

		widget.IsActive = true;
		widget.ZIndex = ++_topZIndex;
	}

	protected override void OnDispose()
	{
		foreach (var (type, _) in _widgetsByType)
			Close(type, true);
	}

	private void Remove(WidgetViewModel widget)
	{
		Widgets.Remove(widget);
		widget.Dispose();
	}
}
