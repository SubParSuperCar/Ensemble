using CommunityToolkit.Mvvm.ComponentModel;

namespace EnsembleRoot.Ui.Impl.Abstractions;

[INotifyPropertyChanged]
public abstract partial class ViewModelBase : DisposableObject, ITransientObject;
