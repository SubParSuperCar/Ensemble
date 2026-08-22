using CommunityToolkit.Mvvm.Messaging;
using Godot;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Messages;

// ReSharper disable EventNeverSubscribedTo.Global

namespace Root.Ui.Impl.Services;

public class DispatcherService : DisposableObject, ISingletonObject, IServiceBase
{
	public DispatcherService()
	{
		WeakReferenceMessenger.Default.Register<ProcessMessage>(this,
			(_, message) => Process?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<InputMessage>(this,
			(_, message) => Input?.Invoke(message.Value));
	}

	protected override void OnDispose() => WeakReferenceMessenger.Default.UnregisterAll(this);

	public event Action<double>? Process;
	public event Action<InputEvent>? Input;
}
