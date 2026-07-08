using Avalonia;
using CommunityToolkit.Mvvm.Messaging;
using Estragonia;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Extensions;
using Root.Ui.Impl.Messages;
using Root.Ui.Impl.Services;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Gd;

public partial class Ui : AvaloniaControl
{
	public override void _Ready()
	{
		GetWindow().SetImeActive(true);

		var collection = new ServiceCollection();
		collection.AddServices();

		var services = collection.BuildServiceProvider();

		var locator = services.GetRequiredService<ViewLocatorService>();
		Application.Current!.DataTemplates.Add(locator);

		var vm = services.GetRequiredService<MainViewModel>();
		Control = locator.Build(vm);

		base._Ready();
	}

	public override void _Process(double delta)
	{
		WeakReferenceMessenger.Default.Send(new ProcessMessage(delta));

		base._Process(delta);
	}

	public override void _Input(InputEvent @event)
	{
		WeakReferenceMessenger.Default.Send(new InputMessage(@event));

		base._Input(@event);
	}
}
