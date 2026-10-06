using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace EnsembleRoot.Ui.Impl.ViewModels;

// TODO: Add a ComboBox to select the minimum log severity level to show in Output
public partial class ConsoleViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private byte _updateLogHistoryFlag;

	public ConsoleViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		Editor = services.Create<LuaEditorViewModel>();

		dispatcher.UiProcess += OnUiProcess;

		OnLogHistoryUpdated();
		VolatileLogHistorySink.Updated += OnLogHistoryUpdated;
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial LuaEditorViewModel? Editor { get; set; }

	[ObservableProperty] public partial string Output { get; set; } = "<Default>";

	protected override void OnDispose()
	{
		VolatileLogHistorySink.Updated -= OnLogHistoryUpdated;
		_dispatcher.UiProcess -= OnUiProcess;

		Editor = null;
	}

	[RelayCommand]
	private static void OpenUserDataDir() => OS.ShellOpen(ProjectSettings.GlobalizePath(UserScheme));

	private void OnLogHistoryUpdated() => Volatile.Write(ref _updateLogHistoryFlag, 1);

	private void OnUiProcess(UiProcessData data)
	{
		if (Interlocked.Exchange(ref _updateLogHistoryFlag, 0) is 1)
			Dispatcher.UIThread.Post(UpdateOutput);
	}

	private void UpdateOutput()
	{
		var history = VolatileLogHistorySink.History;
		Output = history.Count is 0 ? "<Empty>" : string.Join('\n', history.Select(static entry => entry.Text));
	}
}
