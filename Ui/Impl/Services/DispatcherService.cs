using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Messages;
using Godot;

namespace EnsembleRoot.Ui.Impl.Services;

public class DispatcherService : DisposableObject, ISingletonObject, IServiceBase
{
	public DispatcherService()
	{
		WeakReferenceMessenger.Default.Register<UiProcessMessage>(this,
			(_, message) => UiProcess?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<InputMessage>(this,
			(_, message) => Input?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<NotificationMessage>(this,
			(_, message) => Notification?.Invoke(message.Value));
	}

	public event Action<UiProcessData>? UiProcess;
	public event Action<InputEvent>? Input;
	public event Action<int>? Notification;

	protected override void OnDispose() => WeakReferenceMessenger.Default.UnregisterAll(this);
}
