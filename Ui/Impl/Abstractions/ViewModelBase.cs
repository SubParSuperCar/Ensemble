using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.Abstractions;

[INotifyPropertyChanged]
public abstract partial class ViewModelBase : DisposableObject, ITransientObject;
