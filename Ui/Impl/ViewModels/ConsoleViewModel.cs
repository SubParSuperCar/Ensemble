using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Extensions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class ConsoleViewModel : ViewModelBase
{
	public ConsoleViewModel(IServiceProvider services)
	{
		Output = services.Create<LogOutputViewModel>();
		Editor = services.Create<LuaEditorViewModel>();
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial LogOutputViewModel? Output { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial LuaEditorViewModel? Editor { get; set; }

	protected override void OnDispose()
	{
		Output = null;
		Editor = null;
	}
}
