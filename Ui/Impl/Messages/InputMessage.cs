using CommunityToolkit.Mvvm.Messaging.Messages;
using Godot;

namespace EnsembleRoot.Ui.Messages;

public sealed class InputMessage(InputEvent @event) : ValueChangedMessage<InputEvent>(@event);
