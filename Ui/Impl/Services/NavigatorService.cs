using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;

// ReSharper disable UnusedMember.Global

namespace Root.Ui.Impl.Services;

// To be used later on for navigable/paginated menus, etc.
[INotifyPropertyChanged]
public partial class NavigatorService(IServiceProvider services) : DisposableObject, IScopedObject, IServiceBase
{
	// Hold types, not actual living references to actual View Models. The types should be of VMs, though.
	private readonly Stack<Type> _history = [];
	private bool _excludeFromHistory; // Useful for stuff like loading spinners that we shouldn't be able to go back to.

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	// ReSharper disable once MemberCanBePrivate.Global
	public partial ViewModelBase? Current { get; set; }

	public bool CanGoBack => _history.Count > 0;

	// Go to nothing. We'll naturally not be able to go back to this.
	public void GoTo() => Current = null;

	public void GoTo<TViewModel>(bool excludeFromHistory = false) where TViewModel : ViewModelBase
	{
		var type = Current?.GetType();
		if (type == typeof(TViewModel))
			return;

		if (type is not null && !_excludeFromHistory)
			_history.Push(type);

		_excludeFromHistory = excludeFromHistory;

		Current = services.GetRequiredService<TViewModel>();
		OnPropertyChanged(nameof(CanGoBack));
	}

	public void GoBack()
	{
		if (!CanGoBack)
			return;

		_excludeFromHistory = false;

		var type = _history.Pop();
		Current = (ViewModelBase)services.GetRequiredService(type);
		OnPropertyChanged(nameof(CanGoBack));
	}
}
