using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Messages;
using Godot;

namespace EnsembleRoot.Ui.Impl.Services;

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
