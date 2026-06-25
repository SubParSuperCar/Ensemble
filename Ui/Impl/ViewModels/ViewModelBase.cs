using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
	public abstract void Dispose();
}
