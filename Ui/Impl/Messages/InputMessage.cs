using CommunityToolkit.Mvvm.Messaging.Messages;
using Godot;

namespace EnsembleRoot.Ui.Impl.Messages;

public class InputMessage(InputEvent @event) : ValueChangedMessage<InputEvent>(@event);
