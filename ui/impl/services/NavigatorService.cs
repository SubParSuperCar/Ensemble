using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;

// ReSharper disable UnusedMember.Global

namespace Root.Ui.Impl.Services;

[INotifyPropertyChanged]
// ReSharper disable once UnusedType.Global
public partial class NavigatorService(IServiceProvider services) : DisposableObject, IScopedObject, IServiceBase
{
	private readonly Stack<Type> _history = [];

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	// ReSharper disable once MemberCanBeMadeStatic.Global
	// ReSharper disable once MemberCanBePrivate.Global
	public partial ViewModelBase? Current { get; set; }

	public bool CanGoBack => _history.Count > 0;

	public void GoTo<T>() where T : ViewModelBase
	{
		if (Current is not null)
			_history.Push(Current.GetType());

		Current = services.GetRequiredService<T>();
		OnPropertyChanged(nameof(CanGoBack));
	}

	public void GoBack()
	{
		if (!CanGoBack)
			return;

		var type = _history.Pop();
		Current = (ViewModelBase)services.GetRequiredService(type);
		OnPropertyChanged(nameof(CanGoBack));
	}
}
