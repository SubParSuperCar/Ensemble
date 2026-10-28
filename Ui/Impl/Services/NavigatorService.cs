using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;

namespace EnsembleRoot.Ui.Services;

[INotifyPropertyChanged]
public sealed partial class NavigatorService(IServiceProvider services) : DisposableObject, IScopedObject
{
	private readonly Stack<Func<ViewModelBase>> _history = [];
	private Func<ViewModelBase>? _createCurrent;
	private bool _shouldExcludeFromHistory;

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? Current { get; private set; }

	public bool CanGoBack => _history.Count > 0;

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

	protected override void OnDispose() => Current = null;

	private void Show(Func<ViewModelBase> create)
	{
		_createCurrent = create;
		Current = create();

		OnPropertyChanged(nameof(CanGoBack));
	}
}
