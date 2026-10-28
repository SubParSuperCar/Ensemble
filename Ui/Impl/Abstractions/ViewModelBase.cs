using CommunityToolkit.Mvvm.ComponentModel;

namespace EnsembleRoot.Ui.Abstractions;

[INotifyPropertyChanged]
public abstract partial class ViewModelBase : DisposableObject, ITransientObject;
