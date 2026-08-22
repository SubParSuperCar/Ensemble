using CommunityToolkit.Mvvm.Messaging;
using Godot;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Messages;

// ReSharper disable EventNeverSubscribedTo.Global

namespace Root.Ui.Impl.Services;

// Lets resources in the UI react to Godot input and new frames, since normally only Ui.cs could (inherits Node).
public class DispatcherService : DisposableObject, ISingletonObject, IServiceBase
{
	public DispatcherService()
	{
		WeakReferenceMessenger.Default.Register<ProcessMessage>(this,
			(_, message) => Process?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<InputMessage>(this,
			(_, message) => Input?.Invoke(message.Value));
	}

	// Explicitly unregister even though the messenger is weak.
	protected override void OnDispose() => WeakReferenceMessenger.Default.UnregisterAll(this);

	public event Action<double>? Process;
	public event Action<InputEvent>? Input;
}
