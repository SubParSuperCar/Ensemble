using CommunityToolkit.Mvvm.ComponentModel;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;

// ReSharper disable UnusedMember.Global

namespace Root.Ui.Impl.Services;

[INotifyPropertyChanged]
// ReSharper disable once UnusedType.Global
public partial class NavigatorService : DisposableObject, IScopedObject, IServiceBase
{
	private readonly Stack<ViewModelBase> _history = [];

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	// ReSharper disable once MemberCanBeMadeStatic.Global
	// ReSharper disable once MemberCanBePrivate.Global
	public partial ViewModelBase? Current { get; set; }

	public bool CanGoBack => _history.Count > 0;

	public void GoTo(ViewModelBase viewModel)
	{
		if (Current is not null)
			_history.Push(Current);

		Current = viewModel;
		OnPropertyChanged(nameof(CanGoBack));
	}

	public void GoBack()
	{
		if (!CanGoBack)
			return;

		Current = _history.Pop();
		OnPropertyChanged(nameof(CanGoBack));
	}
}
