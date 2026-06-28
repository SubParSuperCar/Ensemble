using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
	public void Dispose()
	{
		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected abstract void OnDispose();
}
