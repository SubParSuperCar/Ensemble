using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;

namespace Root.Ui.Impl.Services;

[INotifyPropertyChanged]
public partial class NavigatorService(IServiceProvider services) : DisposableObject, IScopedObject, IServiceBase
{
	private readonly Stack<Type> _history = [];
	private bool _excludeFromHistory;

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? Current { get; set; }

	public bool CanGoBack => _history.Count > 0;

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
