using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

// TODO
public partial class StatViewModel : ViewModelBase
{
	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	public override void Dispose() => GC.SuppressFinalize(this);
}
