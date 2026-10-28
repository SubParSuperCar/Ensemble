using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class ConsoleViewModel : ViewModelBase
{
	public ConsoleViewModel(IServiceProvider services)
	{
		Output = services.Create<LogOutputViewModel>();
		Editor = services.Create<LuaEditorViewModel>();
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial LogOutputViewModel? Output { get; private set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial LuaEditorViewModel? Editor { get; private set; }

	protected override void OnDispose()
	{
		Output = null;
		Editor = null;
	}
}
