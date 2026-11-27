using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.ViewModels;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace EnsembleRoot.Ui.Services;

/// <summary>
///     Opens, closes, and stacks widgets, at most one per view model type, for its scope's lifetime. A press outside
///     the UI leaves none active, as clicking a desktop does.
/// </summary>
public sealed class WidgetManagerService : DisposableObject, IScopedObject
{
	private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

	private readonly DispatcherService _dispatcher;
	private readonly Dictionary<Type, WidgetEntry> _entriesByType = [];
	private readonly IServiceProvider _services;
	private readonly Dictionary<Type, WidgetViewModel> _widgetsByType = [];
	private int _topZIndex;

	public WidgetManagerService(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

		dispatcher.PressedOutside += Deactivate;
	}

	public ObservableCollection<WidgetEntry> Entries { get; } = [];
	public ObservableCollection<WidgetViewModel> Widgets { get; } = [];

	public WidgetViewModel? Active { get; private set; }

	public WidgetEntry Register<[DynamicallyAccessedMembers(Constructors)] TViewModel>()
		where TViewModel : ViewModelBase, IWidget
	{
		if (_entriesByType.TryGetValue(typeof(TViewModel), out var entry))
			return entry;

		entry = new WidgetEntry(this, typeof(TViewModel), TViewModel.Descriptor, _services.Create<TViewModel>);
		var index = Entries.Count(other => StringComparer.OrdinalIgnoreCase.Compare(other.Title, entry.Title) < 0);

		_entriesByType.Add(entry.Type, entry);
		Entries.Insert(index, entry);

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

	private WidgetViewModel Open(WidgetEntry entry)
	{
		if (_widgetsByType.TryGetValue(entry.Type, out var open))
		{
			Activate(open);
			return open;
		}

		var widget = new WidgetViewModel(
			this, entry, entry.CreateContent(), entry.LastBounds ?? entry.Descriptor.InitialBounds, entry.WasMaximized);

		_widgetsByType.Add(entry.Type, widget);
		Widgets.Add(widget);
		Activate(widget);

		entry.IsOpen = true;
		Log.Debug("Opened widget {Widget}", entry.Type.Name);

		return widget;
	}

	internal bool Close(Type type, bool skipAnimations = false)
	{
		if (!_widgetsByType.Remove(type, out var widget))
			return false;

		Log.Debug("Closing widget {Widget}...", type.Name);

		widget.Entry.LastBounds = widget.RestoredBounds;
		widget.Entry.WasMaximized = widget.IsMaximized;
		widget.Entry.IsOpen = false;
		widget.IsClosing = true;

		if (ReferenceEquals(Active, widget))
		{
			Active = null;

			if (_widgetsByType.Values.MaxBy(static other => other.ZIndex) is { } next)
				Activate(next);
		}

		if (skipAnimations)
			Remove(widget);
		else
			DispatcherTimer.RunOnce(() => Remove(widget), AnimationDuration);

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

	internal void Deactivate()
	{
		Active?.IsActive = false;
		Active = null;
	}

	protected override void OnDispose()
	{
		_dispatcher.PressedOutside -= Deactivate;

		foreach (var (type, _) in _widgetsByType)
			Close(type, true);
	}

	private void Remove(WidgetViewModel widget)
	{
		Widgets.Remove(widget);
		widget.Dispose();
	}
}
