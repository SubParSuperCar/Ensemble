using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Messages;
using Godot;

namespace EnsembleRoot.Ui.Services;

public sealed class DispatcherService : DisposableObject, ISingletonObject, IServiceBase
{
	public DispatcherService()
	{
		WeakReferenceMessenger.Default.Register<DispatcherService, UiProcessMessage>(this,
			static (dispatcher, message) => dispatcher.UiProcess?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<DispatcherService, InputMessage>(this,
			static (dispatcher, message) => dispatcher.Input?.Invoke(message.Value));

		WeakReferenceMessenger.Default.Register<DispatcherService, NotificationMessage>(this,
			static (dispatcher, message) => dispatcher.Notification?.Invoke(message.Value));
	}

	public event Action<UiProcessData>? UiProcess;
	public event Action<InputEvent>? Input;
	public event Action<int>? Notification;

	protected override void OnDispose() => WeakReferenceMessenger.Default.UnregisterAll(this);
}
