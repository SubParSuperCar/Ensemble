using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Ui.Messages;

public sealed class NotificationMessage(int what) : ValueChangedMessage<int>(what);
