using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.Abstractions;

[INotifyPropertyChanged] // Use the attribute because we can't inherit from both DispObj and ObservObj.
public abstract partial class ViewModelBase : DisposableObject, ITransientObject;
