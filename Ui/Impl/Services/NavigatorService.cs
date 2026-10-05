using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Extensions;

namespace EnsembleRoot.Ui.Impl.Services;

[INotifyPropertyChanged]
public partial class NavigatorService(IServiceProvider services) : DisposableObject, IScopedObject, IServiceBase
{
	private readonly Stack<Func<ViewModelBase>> _history = [];
	private Func<ViewModelBase>? _createCurrent;
	private bool _shouldExcludeFromHistory;

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? Current { get; set; }

	public bool CanGoBack => _history.Count > 0;

	public void GoTo()
	{
		_createCurrent = null;
		Current = null;
	}

	public void GoTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
		bool shouldExcludeFromHistory = false) where TViewModel : ViewModelBase
	{
		if (Current?.GetType() == typeof(TViewModel))
			return;

		if (_createCurrent is not null && !_shouldExcludeFromHistory)
			_history.Push(_createCurrent);

		_shouldExcludeFromHistory = shouldExcludeFromHistory;
		Show(services.Create<TViewModel>);
	}

	public void GoBack()
	{
		if (!CanGoBack)
			return;

		_shouldExcludeFromHistory = false;
		Show(_history.Pop());
	}

	private void Show(Func<ViewModelBase> create)
	{
		_createCurrent = create;
		Current = create();

		OnPropertyChanged(nameof(CanGoBack));
	}
}
